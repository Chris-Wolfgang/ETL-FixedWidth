using System.Collections.Generic;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.TestKit.Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Runs the <see cref="TransformerBase{TSource, TDestination, TProgress}"/> contract
/// suite (TransformAsync, counters, progress reporting, cancellation) against
/// <see cref="FixedWidthTransformer{TSource, TDestination}"/> using an identity
/// projection, so source and expected items coincide.
/// </summary>
public class FixedWidthTransformerContractTests
    : TransformerBaseContractTests
    <
        FixedWidthTransformer<PersonRecord, PersonRecord>,
        PersonRecord,
        FixedWidthReport
    >
{
    /// <inheritdoc/>
    protected override FixedWidthTransformer<PersonRecord, PersonRecord> CreateSut(int itemCount, int maximumItemCount, int skipItemCount, int reportingInterval) =>
        new
        (
            record => record,
            new FixedWidthTransformerOptions
            {
                MaximumItemCount = maximumItemCount,
                SkipItemCount = skipItemCount,
                ReportingInterval = reportingInterval,
            }
        );



    /// <inheritdoc/>
    protected override IReadOnlyList<PersonRecord> CreateExpectedItems() => new[]
    {
        new PersonRecord { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new PersonRecord { FirstName = "Bob", LastName = "Brown", Age = 30 },
        new PersonRecord { FirstName = "Carol", LastName = "Clark", Age = 35 },
        new PersonRecord { FirstName = "Dan", LastName = "Davis", Age = 40 },
        new PersonRecord { FirstName = "Eve", LastName = "Evans", Age = 45 },
    };



}
