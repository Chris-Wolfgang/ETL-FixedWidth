using System;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

public class BenchmarkRecordWithDate
{
    [FixedWidthField(0, 20)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 20)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 8, Format = "yyyyMMdd")]
    public DateTime BirthDate { get; set; }



    [FixedWidthField(3, 5, Alignment = FieldAlignment.Right, Pad = '0')]
    public int ZipCode { get; set; }
}
