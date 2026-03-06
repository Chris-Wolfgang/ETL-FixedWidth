using System;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit
{
    public class FieldContextTests
    {
        [Fact]
        public void Constructor_sets_all_properties()
        {
            var ctx = new FieldContext
            (
                propertyName: "Amount",
                propertyType: typeof(decimal),
                fieldLength: 10,
                pad: '0',
                alignment: FieldAlignment.Right,
                format: "F2",
                headerLabel: "AMOUNT"
            );

            Assert.Equal
            (
                "Amount",
                ctx.PropertyName
            );
            Assert.Equal
            (
                typeof(decimal),
                ctx.PropertyType
            );
            Assert.Equal
            (
                10,
                ctx.FieldLength
            );
            Assert.Equal
            (
                '0',
                ctx.Pad
            );
            Assert.Equal
            (
                FieldAlignment.Right,
                ctx.Alignment
            );
            Assert.Equal
            (
                "F2",
                ctx.Format
            );
            Assert.Equal
            (
                "AMOUNT",
                ctx.HeaderLabel
            );
        }
    }
}
