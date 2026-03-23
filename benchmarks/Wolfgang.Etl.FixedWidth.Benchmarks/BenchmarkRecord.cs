using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

public class BenchmarkRecord
{
    [FixedWidthField(0, 20)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 20)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 10)]
    public string City { get; set; } = string.Empty;



    [FixedWidthField(3, 5, Alignment = FieldAlignment.Right, Pad = '0')]
    public int ZipCode { get; set; }



    [FixedWidthField(4, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
