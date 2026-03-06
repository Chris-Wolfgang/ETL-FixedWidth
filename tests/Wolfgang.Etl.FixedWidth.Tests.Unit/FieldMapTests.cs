using System;
using System.Diagnostics.CodeAnalysis;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Parsing;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit
{
    public class FieldMapTests
    {
        // ------------------------------------------------------------------
        // Test POCOs
        // ------------------------------------------------------------------

        [ExcludeFromCodeCoverage]
        private class IndexedRecord
        {
            // Declared last in source, but Index = 0 means it's the first column.
            [FixedWidthField(2, 3)]
            public int Age { get; set; }



            [FixedWidthField(0, 10)]
            public string First { get; set; }



            [FixedWidthField(1, 10)]
            public string Last { get; set; }
        }



        [ExcludeFromCodeCoverage]
        private class DuplicateIndexRecord
        {
            [FixedWidthField(0, 10)]
            public string First { get; set; }



            [FixedWidthField(0, 10)] // duplicate!
            public string Last { get; set; }
        }



        [ExcludeFromCodeCoverage]
        private class NoSetterRecord
        {
            [FixedWidthField(0, 10)]
            public string ReadOnly { get; } // no setter!
        }



        [ExcludeFromCodeCoverage]
        private class UnannotatedRecord
        {
            public string Name { get; set; }
        }



        [ExcludeFromCodeCoverage]
        private class SkipMiddleRecord
        {
            [FixedWidthField(0, 10)]
            public string FirstName { get; set; }



            [FixedWidthSkip(1, 8, Message = "DOB")]
            [FixedWidthSkip(2, 8, Message = "HireDate")]
            [FixedWidthField(3, 5)]
            public string EmployeeNumber { get; set; }



            [FixedWidthField(4, 10)]
            public string LastName { get; set; }
        }



        [ExcludeFromCodeCoverage]
        private class SkipLeadingRecord
        {
            [FixedWidthSkip(0, 5, Message = "RecordType")]
            [FixedWidthField(1, 10)]
            public string FirstName { get; set; }



            [FixedWidthField(2, 10)]
            public string LastName { get; set; }
        }



        [ExcludeFromCodeCoverage]
        private class SkipTrailingRecord
        {
            [FixedWidthField(0, 10)]
            public string FirstName { get; set; }



            [FixedWidthSkip(1, 3, Message = "Filler")]
            public string Unused { get; set; }
        }



        [ExcludeFromCodeCoverage]
        private class SkipDuplicateIndexRecord
        {
            [FixedWidthField(0, 10)]
            public string FirstName { get; set; }



            [FixedWidthSkip(0, 5)] // duplicate of above
            [FixedWidthField(1, 5)]
            public string LastName { get; set; }
        }



        // ------------------------------------------------------------------
        // Index ordering
        // ------------------------------------------------------------------

        [Fact]
        public void GetResult_when_fields_have_Index_attributes_orders_descriptors_by_Index()
        {
            var fieldMap = FieldMap.GetResult<IndexedRecord>();

            Assert.Equal
            (
                3,
                fieldMap.Descriptors.Count
            );
            Assert.Equal
            (
                nameof(IndexedRecord.First),
                fieldMap.Descriptors[0].Property.Name
            );
            Assert.Equal
            (
                nameof(IndexedRecord.Last),
                fieldMap.Descriptors[1].Property.Name
            );
            Assert.Equal
            (
                nameof(IndexedRecord.Age),
                fieldMap.Descriptors[2].Property.Name
            );
        }



        [Fact]
        public void GetResult_when_fields_have_Index_attributes_calculates_Start_positions_after_reorder()
        {
            var fieldMap = FieldMap.GetResult<IndexedRecord>();

            Assert.Equal
            (
                0,
                fieldMap.Descriptors[0].Start
            ); // First (Index=0): 0
            Assert.Equal
            (
                10,
                fieldMap.Descriptors[1].Start
            ); // Last  (Index=1): 0 + 10
            Assert.Equal
            (
                20,
                fieldMap.Descriptors[2].Start
            ); // Age   (Index=2): 0 + 10 + 10
        }



        // ------------------------------------------------------------------
        // Error — duplicate indexes
        // ------------------------------------------------------------------

        [Fact]
        public void GetResult_when_duplicate_Index_values_exist_throws_InvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>( () => FieldMap.GetResult<DuplicateIndexRecord>());

            Assert.Contains
            (
                "duplicate",
                ex.Message,
                StringComparison.OrdinalIgnoreCase
            );
        }



        // ------------------------------------------------------------------
        // Error — property missing setter
        // ------------------------------------------------------------------

        [Fact]
        public void GetResult_when_FixedWidthField_property_has_no_setter_throws_InvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>( () => FieldMap.GetResult<NoSetterRecord>());

            Assert.Contains
            (
                "no public setter",
                ex.Message
            );
        }



        // ------------------------------------------------------------------
        // Edge — no annotated properties
        // ------------------------------------------------------------------

        [Fact]
        public void GetResult_when_type_has_no_FixedWidthField_annotations_returns_empty_descriptors()
        {
            var fieldMap = FieldMap.GetResult<UnannotatedRecord>();

            Assert.Empty(fieldMap.Descriptors);
        }



        // ------------------------------------------------------------------
        // FixedWidthSkipAttribute
        // ------------------------------------------------------------------

        [Fact]
        public void GetResult_when_FixedWidthSkip_attributes_are_in_the_middle_calculates_Start_positions_correctly()
        {
            var fieldMap = FieldMap.GetResult<SkipMiddleRecord>();

            Assert.Equal
            (
                3,
                fieldMap.Descriptors.Count
            );
            Assert.Equal
            (
                5,
                fieldMap.TotalColumnCount
            ); // 3 fields + 2 skips

            Assert.Equal
            (
                "FirstName",
                fieldMap.Descriptors[0].Property.Name
            );
            Assert.Equal
            (
                0,
                fieldMap.Descriptors[0].Start
            );
            Assert.Equal
            (
                0,
                fieldMap.Descriptors[0].AbsoluteColumnIndex
            );

            Assert.Equal
            (
                "EmployeeNumber",
                fieldMap.Descriptors[1].Property.Name
            );
            Assert.Equal
            (
                26,
                fieldMap.Descriptors[1].Start
            ); // 10 + 8 + 8
            Assert.Equal
            (
                3,
                fieldMap.Descriptors[1].AbsoluteColumnIndex
            );

            Assert.Equal
            (
                "LastName",
                fieldMap.Descriptors[2].Property.Name
            );
            Assert.Equal
            (
                31,
                fieldMap.Descriptors[2].Start
            ); // 10 + 8 + 8 + 5
            Assert.Equal
            (
                4,
                fieldMap.Descriptors[2].AbsoluteColumnIndex
            );
        }



        [Fact]
        public void GetResult_when_FixedWidthSkip_attributes_are_in_the_middle_ExpectedLineWidth_includes_skip_widths()
        {
            var fieldMap = FieldMap.GetResult<SkipMiddleRecord>();

            Assert.Equal
            (
                41,
                fieldMap.ExpectedLineWidth
            ); // 10 + 8 + 8 + 5 + 10
        }



        [Fact]
        public void GetResult_when_FixedWidthSkip_attribute_is_leading_offsets_Start_position_by_skip_width()
        {
            var fieldMap = FieldMap.GetResult<SkipLeadingRecord>();

            Assert.Equal
            (
                3,
                fieldMap.TotalColumnCount
            ); // 1 skip + 2 fields
            Assert.Equal
            (
                2,
                fieldMap.Descriptors.Count
            );

            Assert.Equal
            (
                "FirstName",
                fieldMap.Descriptors[0].Property.Name
            );
            Assert.Equal
            (
                5,
                fieldMap.Descriptors[0].Start
            ); // skip is 5 wide
            Assert.Equal
            (
                1,
                fieldMap.Descriptors[0].AbsoluteColumnIndex
            );

            Assert.Equal
            (
                "LastName",
                fieldMap.Descriptors[1].Property.Name
            );
            Assert.Equal
            (
                15,
                fieldMap.Descriptors[1].Start
            ); // 5 + 10
            Assert.Equal
            (
                2,
                fieldMap.Descriptors[1].AbsoluteColumnIndex
            );
        }



        [Fact]
        public void GetResult_when_FixedWidthSkip_attribute_is_trailing_ExpectedLineWidth_includes_skip_width()
        {
            var fieldMap = FieldMap.GetResult<SkipTrailingRecord>();

            Assert.Equal
            (
                1,
                fieldMap.Descriptors.Count
            );
            Assert.Equal
            (
                2,
                fieldMap.TotalColumnCount
            );
            Assert.Equal
            (
                13,
                fieldMap.ExpectedLineWidth
            ); // 10 + 3
        }



        [Fact]
        public void GetResult_when_FixedWidthSkip_has_duplicate_Index_throws_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>( () => FieldMap.GetResult<SkipDuplicateIndexRecord>());
        }
    }
}
