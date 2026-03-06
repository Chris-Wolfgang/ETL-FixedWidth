using System;
using System.ComponentModel;
using System.Globalization;
using Wolfgang.Etl.FixedWidth.Exceptions;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Provides built-in value converter, header converter, and value parser functions
/// for use with <see cref="FixedWidthLoader{TRecord,TProgress}"/> and
/// <see cref="FixedWidthExtractor{TRecord,TProgress}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Value converters</b> (<see cref="Func{T1,T2,TResult}"/> of <c>object, FieldContext, string</c>)
/// convert a typed property value to its string representation. Rules:
/// <list type="bullet">
///   <item><see cref="DateTime"/>, <see cref="DateTimeOffset"/>, and <see cref="TimeSpan"/>
///   require an explicit <see cref="FieldContext.Format"/>. An <see cref="InvalidOperationException"/>
///   is thrown if none is provided.</item>
///   <item>All other <see cref="IFormattable"/> types use <see cref="CultureInfo.InvariantCulture"/>
///   with the optional <see cref="FieldContext.Format"/> if supplied.</item>
///   <item>Null values are converted to <see cref="string.Empty"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Header converters</b> (<see cref="Func{T1,T2,TResult}"/> of <c>string, FieldContext, string</c>)
/// convert a header label to its padded string representation.
/// </para>
/// <para>
/// <b>Value parsers</b> (<see cref="Func{T1,T2,TResult}"/> of <c>string, FieldContext, object</c>)
/// convert a raw string read from the file back to the target property type.
/// <see cref="DefaultParser"/> is the built-in implementation used by
/// <see cref="FixedWidthExtractor{TRecord,TProgress}"/> by default.
/// </para>
/// </remarks>
public static class FixedWidthConverter
{
    // ------------------------------------------------------------------
    // Value converters
    // ------------------------------------------------------------------

    /// <summary>
    /// Converts the value to a string and throws a <see cref="FieldOverflowException"/>
    /// if the result exceeds <see cref="FieldContext.FieldLength"/>. This is the default
    /// value converter.
    /// </summary>
    public static readonly Func<object, FieldContext, string> Strict =
        (value, context) =>
        {
            var text = ConvertToString
            (
                value,
                context
            );

            if (text.Length > context.FieldLength)
            {
                throw new FieldOverflowException
                (
                    $"Value for property '{context.PropertyName}' has length {text.Length} which " + $"exceeds the defined field width of {context.FieldLength}.",
                    context.PropertyName,
                    context.FieldLength,
                    text.Length
                );
            }

            return text;
        };



    /// <summary>
    /// Converts the value to a string and silently truncates it to
    /// <see cref="FieldContext.FieldLength"/> if it is too long.
    /// </summary>
    public static readonly Func<object, FieldContext, string> Truncate =
        (value, context) =>
        {
            var text = ConvertToString
            (
                value,
                context
            );
            return text.Length > context.FieldLength
                ? text.Substring(0, context.FieldLength)
                : text;
        };



    // ------------------------------------------------------------------
    // Header converters
    // ------------------------------------------------------------------

    /// <summary>
    /// Validates that the header label fits within <see cref="FieldContext.FieldLength"/>,
    /// throwing a <see cref="FieldOverflowException"/> if it does not, then returns the
    /// label unchanged. Space-padding is applied by the framework after the converter returns.
    /// This is the default header converter.
    /// </summary>
    /// <remarks>
    /// The <see cref="FieldContext"/> is passed as-is from the attribute. If you want
    /// different alignment or padding for header cells, supply a custom
    /// <see cref="FixedWidthLoader{TRecord,TProgress}.HeaderConverter"/>.
    /// </remarks>
    public static readonly Func<string, FieldContext, string> StrictHeader =
        (header, context) =>
        {
            if (header.Length > context.FieldLength)
            {
                throw new FieldOverflowException
                (
                    $"Header label '{header}' for property '{context.PropertyName}' has length " + $"{header.Length} which exceeds the defined field width of {context.FieldLength}.",
                    context.PropertyName,
                    context.FieldLength,
                    header.Length
                );
            }

            return header;
        };



    /// <summary>
    /// Silently truncates the header label to <see cref="FieldContext.FieldLength"/> if it
    /// is too long, then returns it unchanged otherwise. Space-padding is applied by the
    /// framework after the converter returns.
    /// </summary>
    public static readonly Func<string, FieldContext, string> TruncateHeader =
        (header, context) =>
            header.Length > context.FieldLength
                ? header.Substring(0, context.FieldLength)
                : header;



    // ------------------------------------------------------------------
    // Value parser
    // ------------------------------------------------------------------

    /// <summary>
    /// The default value parser. Converts a raw string read from the file to the
    /// target property type indicated by <see cref="FieldContext.PropertyType"/>.
    /// This is the default <see cref="FixedWidthExtractor{TRecord,TProgress}.ValueParser"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Nullable types: empty string returns <see langword="null"/>, non-empty
    ///   recurses on the underlying type.</item>
    ///   <item><see cref="string"/>: returned as-is.</item>
    ///   <item>Empty string for non-nullable value types: returns the default value
    ///   (e.g. <c>0</c> for <c>int</c>).</item>
    ///   <item><see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/>:
    ///   parsed with <see cref="FieldContext.Format"/> using
    ///   <see cref="CultureInfo.InvariantCulture"/>.</item>
    ///   <item>All other types: parsed via <see cref="TypeDescriptor"/> with
    ///   <see cref="CultureInfo.InvariantCulture"/>.</item>
    /// </list>
    /// </remarks>
    public static readonly Func<string, FieldContext, object> DefaultParser =
        (text, context) => ParseValue
        (
            text,
            context.PropertyType,
            context.Format
        );



    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------

    internal static string ConvertToString
    (
        object? value,
        FieldContext context
    )
    {
        if (value == null)
        {
            return string.Empty;
        }

        // Types that have no safe culture-neutral default — require explicit Format.
        if (value is DateTime or DateTimeOffset or TimeSpan)
        {
            if (string.IsNullOrEmpty(context.Format))
            {
                throw new InvalidOperationException
                    (
                        $"Property '{context.PropertyName}' is of type '{value.GetType().Name}' " +
                        "but no Format was specified on the [FixedWidthField] attribute. " +
                        "Fixed-width date and time fields require an explicit format string " +
                        "(e.g. \"yyyyMMdd\", \"HHmmss\").");
            }

            return ((IFormattable)value).ToString
            (
                context.Format,
                CultureInfo.InvariantCulture
            );
        }



        // All other IFormattable types — use InvariantCulture, apply Format if supplied.
        if (value is IFormattable formattable)
        {
            return formattable.ToString
            (
                context.Format,
                CultureInfo.InvariantCulture
            );
        }

        return value.ToString() ?? string.Empty;
    }



    internal static object ParseValue
    (
        string text,
        Type targetType,
        string format
    )
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            return ParseValue
            (
                text,
                underlying,
                format
            );
        }

        if (targetType == typeof(string))
        {
            return text;
        }

        if (string.IsNullOrEmpty(text))
        {
            return targetType.IsValueType
                ? Activator.CreateInstance(targetType)
                : null;
        }

        if (targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset) || targetType == typeof(TimeSpan))
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new InvalidOperationException
                (
                    $"Cannot parse a value of type '{targetType.Name}' without " +
                    "a format string. Specify a Format on the [FixedWidthField] " +
                    "attribute (e.g. \"yyyyMMdd\", \"HHmmss\")."
                );
            }

            if (targetType == typeof(DateTime))
            {
                return DateTime.ParseExact
                (
                    text,
                    format,
                    CultureInfo.InvariantCulture
                );
            }
            if (targetType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.ParseExact
                (
                    text,
                    format,
                    CultureInfo.InvariantCulture
                );
            }
            return TimeSpan.ParseExact
            (
                text,
                format,
                CultureInfo.InvariantCulture
            );
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromInvariantString(text);
        }

        return Convert.ChangeType
        (
            text,
            targetType,
            CultureInfo.InvariantCulture
        );
    }
}
