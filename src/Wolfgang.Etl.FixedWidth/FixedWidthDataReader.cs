using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// A forward-only, read-only <see cref="IDataReader"/> over a fixed-width text source, with the
/// field layout taken from the <c>[FixedWidthField]</c> / <c>[FixedWidthSkip]</c> attributes on
/// <typeparamref name="TRecord"/> (#26). Serves each field's value directly from the parsed line —
/// <b>no <typeparamref name="TRecord"/> instance is created per row</b> — which is the optimal shape
/// for <c>SqlBulkCopy</c>, <see cref="DataTable.Load(IDataReader)"/>, and other ADO.NET consumers
/// that would otherwise discard a POCO per row.
/// </summary>
/// <typeparam name="TRecord">
/// The type whose <see cref="Attributes.FixedWidthFieldAttribute"/>-decorated properties define the
/// column layout. It is used only for its layout metadata; instances are never constructed.
/// </typeparam>
/// <example>
/// <code>
/// using var reader = new FixedWidthDataReader&lt;CustomerRecord&gt;(fileStream);
/// using var bulkCopy = new SqlBulkCopy(connection) { DestinationTableName = "Customers" };
/// await bulkCopy.WriteToServerAsync(reader);
/// </code>
/// </example>
public sealed class FixedWidthDataReader<TRecord> : IDataReader
    where TRecord : notnull
{
    private const int DefaultBufferSize = 65536;

    private readonly TextReader _reader;
    private readonly bool _ownsReader;
    private readonly FieldMapResult _fieldMap;
    private readonly string[] _names;
    private readonly object?[] _current;

    private bool _hasRow;
    private bool _closed;
    private long _recordsRead;
    private long _dataLinesSkipped;
    private int _headerLinesConsumed;
    private long _currentLineNumber;



    /// <summary>
    /// Initializes a new <see cref="FixedWidthDataReader{TRecord}"/> reading from a
    /// <see cref="TextReader"/>. The caller owns the reader's lifetime — it is not disposed.
    /// </summary>
    /// <param name="reader">The fixed-width text source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <see langword="null"/>.</exception>
    public FixedWidthDataReader(TextReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _fieldMap = FieldMap.GetResult<TRecord>();
        _names = BuildNames(_fieldMap);
        _current = new object?[_fieldMap.Descriptors.Count];
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthDataReader{TRecord}"/> reading from a
    /// <see cref="Stream"/> via an internal 64 KB-buffered <see cref="StreamReader"/>. The caller
    /// retains ownership of the stream (it is not closed), but <see cref="Dispose"/> must be called
    /// to release the internal reader.
    /// </summary>
    /// <param name="stream">The readable fixed-width stream.</param>
    /// <param name="encoding">The encoding to decode with, or <see langword="null"/> for <see cref="Encoding.UTF8"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public FixedWidthDataReader(Stream stream, Encoding? encoding = null)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        _reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, DefaultBufferSize, leaveOpen: true);
        _ownsReader = true;
        _fieldMap = FieldMap.GetResult<TRecord>();
        _names = BuildNames(_fieldMap);
        _current = new object?[_fieldMap.Descriptors.Count];
    }



    // ------------------------------------------------------------------
    // Configuration (mirrors FixedWidthExtractor<TRecord>)
    // ------------------------------------------------------------------

    /// <summary>The number of leading lines to treat as a header and skip. Default 0.</summary>
    public int HeaderLineCount { get; init; }

    /// <summary>The number of data rows to skip before the first row is served. Default 0.</summary>
    public long SkipItemCount { get; init; }

    /// <summary>The maximum number of data rows to serve. Default <see cref="long.MaxValue"/>.</summary>
    public long MaximumItemCount { get; init; } = long.MaxValue;

    /// <summary>How blank lines are handled. Default <see cref="BlankLineHandling.ThrowException"/>.</summary>
    public BlankLineHandling BlankLineHandling { get; init; } = BlankLineHandling.ThrowException;

    /// <summary>How malformed lines are handled. Default <see cref="MalformedLineHandling.ThrowException"/>.</summary>
    public MalformedLineHandling MalformedLineHandling { get; init; } = MalformedLineHandling.ThrowException;

    /// <summary>The inter-field delimiter used when the file was written, or <see langword="null"/> for none.</summary>
    public string? FieldDelimiter { get; init; }



    // ------------------------------------------------------------------
    // IDataReader — forward-only iteration
    // ------------------------------------------------------------------

    /// <inheritdoc/>
#pragma warning disable MA0051 // the line-reading loop reads best as one method — same call as FixedWidthExtractor.ExtractWorkerAsync
    public bool Read()
    {
        if (_closed)
        {
            throw new InvalidOperationException("The data reader is closed.");
        }

        while (true)
        {
            if (_recordsRead >= MaximumItemCount)
            {
                _hasRow = false;
                return false;
            }

            var line = _reader.ReadLine();
            if (line == null)
            {
                _hasRow = false;
                return false;
            }

            _currentLineNumber++;

            // Leading header lines are consumed first and never served.
            if (_headerLinesConsumed < HeaderLineCount)
            {
                _headerLinesConsumed++;
                continue;
            }

            if (IsBlank(line))
            {
                if (BlankLineHandling == BlankLineHandling.Skip)
                {
                    continue;
                }

                if (BlankLineHandling == BlankLineHandling.ThrowException)
                {
                    var delimiterWidth = string.IsNullOrEmpty(FieldDelimiter) ? 0 : FieldDelimiter!.Length;
                    var expectedWidth = _fieldMap.ExpectedLineWidth + (delimiterWidth * Math.Max(0, _fieldMap.TotalColumnCount - 1));
                    throw new LineTooShortException($"Blank line encountered at line {_currentLineNumber}.", _currentLineNumber, string.Empty, expectedWidth, 0);
                }

                // ReturnDefault — participates in the skip/max budgets like a data row.
                if (_dataLinesSkipped < SkipItemCount)
                {
                    _dataLinesSkipped++;
                    continue;
                }

                FillDefaultRow();
                _recordsRead++;
                _hasRow = true;
                return true;
            }

            if (_dataLinesSkipped < SkipItemCount)
            {
                _dataLinesSkipped++;
                continue;
            }

            try
            {
                FillRow(line);
            }
            catch (Exception ex) when (ex is LineTooShortException or MalformedLineException or FieldConversionException)
            {
                if (MalformedLineHandling == MalformedLineHandling.Skip)
                {
                    continue;
                }

                if (MalformedLineHandling == MalformedLineHandling.ReturnDefault)
                {
                    FillDefaultRow();
                    _recordsRead++;
                    _hasRow = true;
                    return true;
                }

                throw;
            }

            _recordsRead++;
            _hasRow = true;
            return true;
        }
    }
#pragma warning restore MA0051



    /// <inheritdoc/>
    public bool NextResult() => false;

    /// <inheritdoc/>
    public int Depth => 0;

    /// <inheritdoc/>
    public bool IsClosed => _closed;

    /// <inheritdoc/>
    public int RecordsAffected => -1;

    /// <inheritdoc/>
    public void Close()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _hasRow = false;

        // Many ADO.NET consumers call Close() rather than Dispose(); release the internal
        // StreamReader here too. It was created with leaveOpen:true, so the caller's stream stays open.
        if (_ownsReader)
        {
            _reader.Dispose();
        }
    }



    // ------------------------------------------------------------------
    // IDataRecord — field metadata
    // ------------------------------------------------------------------

    /// <inheritdoc/>
    public int FieldCount => _fieldMap.Descriptors.Count;

    /// <inheritdoc/>
    public string GetName(int i) => _names[i];

    /// <inheritdoc/>
    public int GetOrdinal(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        for (var i = 0; i < _names.Length; i++)
        {
            if (string.Equals(_names[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        for (var i = 0; i < _names.Length; i++)
        {
            if (string.Equals(_names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"No field named '{name}'.");
    }

    /// <inheritdoc/>
    public Type GetFieldType(int i)
    {
        // ADO.NET convention: report the underlying value type for a nullable column (nullness is
        // signalled through IsDBNull), and DataColumn.DataType rejects Nullable&lt;T&gt; outright.
        var type = _fieldMap.Descriptors[i].Context.PropertyType;
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <inheritdoc/>
    public string GetDataTypeName(int i) => GetFieldType(i).Name;



    // ------------------------------------------------------------------
    // IDataRecord — value access
    // ------------------------------------------------------------------

    /// <inheritdoc/>
    public object GetValue(int i)
    {
        EnsureRow();
        return _current[i] ?? DBNull.Value;
    }

    /// <inheritdoc/>
    public int GetValues(object[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        EnsureRow();
        var count = Math.Min(values.Length, _current.Length);
        for (var i = 0; i < count; i++)
        {
            values[i] = _current[i] ?? DBNull.Value;
        }

        return count;
    }

    /// <inheritdoc/>
    public bool IsDBNull(int i)
    {
        EnsureRow();
        return _current[i] == null;
    }

    /// <inheritdoc/>
    public object this[int i] => GetValue(i);

    /// <inheritdoc/>
    public object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc/>
    public bool GetBoolean(int i) => (bool)GetValue(i);

    /// <inheritdoc/>
    public byte GetByte(int i) => (byte)GetValue(i);

    /// <inheritdoc/>
    public char GetChar(int i) => (char)GetValue(i);

    /// <inheritdoc/>
    public DateTime GetDateTime(int i) => (DateTime)GetValue(i);

    /// <inheritdoc/>
    public decimal GetDecimal(int i) => (decimal)GetValue(i);

    /// <inheritdoc/>
    public double GetDouble(int i) => (double)GetValue(i);

    /// <inheritdoc/>
    public float GetFloat(int i) => (float)GetValue(i);

    /// <inheritdoc/>
    public Guid GetGuid(int i) => (Guid)GetValue(i);

    /// <inheritdoc/>
    public short GetInt16(int i) => (short)GetValue(i);

    /// <inheritdoc/>
    public int GetInt32(int i) => (int)GetValue(i);

    /// <inheritdoc/>
    public long GetInt64(int i) => (long)GetValue(i);

    /// <inheritdoc/>
    public string GetString(int i) => (string)GetValue(i);

    /// <inheritdoc/>
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        => throw new NotSupportedException("Binary field access is not supported by the fixed-width data reader.");

    /// <inheritdoc/>
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
    {
        var text = GetString(i);
        if (fieldoffset < 0 || fieldoffset > text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldoffset));
        }

        if (buffer == null)
        {
            return text.Length;
        }

        if (bufferoffset < 0 || bufferoffset > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferoffset));
        }

        var available = text.Length - (int)fieldoffset;
        var count = Math.Max(0, Math.Min(length, available));
        text.CopyTo((int)fieldoffset, buffer, bufferoffset, count);
        return count;
    }

    /// <inheritdoc/>
    public IDataReader GetData(int i)
        => throw new NotSupportedException("Nested data readers are not supported by the fixed-width data reader.");



    // ------------------------------------------------------------------
    // Schema
    // ------------------------------------------------------------------

    /// <inheritdoc/>
    public DataTable GetSchemaTable()
    {
        var table = new DataTable("SchemaTable") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("ColumnName", typeof(string));
        table.Columns.Add("ColumnOrdinal", typeof(int));
        table.Columns.Add("ColumnSize", typeof(int));
        table.Columns.Add("DataType", typeof(Type));
        table.Columns.Add("AllowDBNull", typeof(bool));

        for (var i = 0; i < _fieldMap.Descriptors.Count; i++)
        {
            var descriptor = _fieldMap.Descriptors[i];
            var type = descriptor.Context.PropertyType;
            var nullable = !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
            table.Rows.Add(_names[i], i, descriptor.Context.FieldLength, Nullable.GetUnderlyingType(type) ?? type, nullable);
        }

        return table;
    }



    // ------------------------------------------------------------------
    // IDisposable
    // ------------------------------------------------------------------

    /// <inheritdoc/>
    public void Dispose() => Close();



    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------

    private void EnsureRow()
    {
        if (_closed)
        {
            throw new InvalidOperationException("Invalid attempt to read from a closed data reader.");
        }

        if (!_hasRow)
        {
            throw new InvalidOperationException("No current row. Call Read() first, and check that it returned true.");
        }
    }

    // A "blank" line is zero-length only, matching FixedWidthExtractor. A whitespace-only line is a
    // valid data line and flows through normal parsing (so a short one raises LineTooShortException
    // with its actual content/length, not the blank-line path).
    private static bool IsBlank(string line) => line.Length == 0;

    private static string[] BuildNames(FieldMapResult fieldMap)
    {
        var names = new string[fieldMap.Descriptors.Count];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = fieldMap.Descriptors[i].Context.PropertyName;
        }

        return names;
    }

    private void FillRow(string line)
    {
        var delimiterWidth = string.IsNullOrEmpty(FieldDelimiter) ? 0 : FieldDelimiter!.Length;
        var delimiterCount = Math.Max(0, _fieldMap.TotalColumnCount - 1);
        var fullExpectedWidth = _fieldMap.ExpectedLineWidth + (delimiterWidth * delimiterCount);

        if (line.Length < fullExpectedWidth)
        {
            throw new LineTooShortException
            (
                $"Line {_currentLineNumber} is too short. Expected {fullExpectedWidth} characters but found {line.Length}.",
                _currentLineNumber,
                line,
                fullExpectedWidth,
                line.Length
            );
        }

        for (var i = 0; i < _fieldMap.Descriptors.Count; i++)
        {
            var descriptor = _fieldMap.Descriptors[i];
            var start = descriptor.Start + (delimiterWidth * descriptor.AbsoluteColumnIndex);
            var raw = line.AsMemory().Slice(start, descriptor.Attribute.Length);
            var value = descriptor.Attribute.TrimValue ? raw.TrimMemory() : raw;

            try
            {
                _current[i] = FixedWidthConverter.ParseValue
                (
                    value,
                    descriptor.Context.PropertyType,
                    descriptor.Context.Format,
                    descriptor.TypeConverter,
                    descriptor.Context.NumberStyles
                );
            }
            catch (Exception ex) when (!(ex is MalformedLineException))
            {
                throw new FieldConversionException
                (
                    $"Line {_currentLineNumber}: could not convert value '{value}' to type " +
                    $"'{descriptor.Context.PropertyType.Name}' for field '{descriptor.Context.PropertyName}'.",
                    _currentLineNumber,
                    line,
                    descriptor.Context.PropertyName,
                    descriptor.Context.PropertyType,
                    value.ToString(),
                    ex
                );
            }
        }
    }

    private void FillDefaultRow()
    {
        for (var i = 0; i < _fieldMap.Descriptors.Count; i++)
        {
            var type = _fieldMap.Descriptors[i].Context.PropertyType;
            _current[i] = type.IsValueType && Nullable.GetUnderlyingType(type) == null
                ? Activator.CreateInstance(type)
                : null;
        }
    }
}
