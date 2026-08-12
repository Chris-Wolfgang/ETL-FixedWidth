using System;
using System.Collections.Generic;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// The resolved binary layout for a record type: the ordered field descriptors, the total record
/// length in bytes, and a factory for the record instance.
/// </summary>
internal sealed class BinaryRecordMap
{
    internal BinaryRecordMap(IReadOnlyList<BinaryFieldDescriptor> descriptors, int recordByteLength, Func<object> factory)
    {
        Descriptors = descriptors;
        RecordByteLength = recordByteLength;
        Factory = factory;
    }

    internal IReadOnlyList<BinaryFieldDescriptor> Descriptors { get; }

    internal int RecordByteLength { get; }

    internal Func<object> Factory { get; }
}
