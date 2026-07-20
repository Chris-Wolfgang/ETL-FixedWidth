using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Transforms a stream of <typeparamref name="TSource"/> records into
/// <typeparamref name="TDestination"/> records in a single streaming pass (#14) —
/// the middle stage of an extract → transform → load pipeline. Compose it between
/// a <see cref="FixedWidthExtractor{TRecord}"/> reading one fixed-width layout and
/// a <see cref="FixedWidthLoader{TRecord}"/> writing another to reformat a file
/// (reorder, add/remove, or reformat fields).
/// </summary>
/// <typeparam name="TSource">The source record type.</typeparam>
/// <typeparam name="TDestination">The destination record type.</typeparam>
/// <example>
/// <code>
/// using var extractor = new FixedWidthExtractor&lt;LegacyRecord&gt;(sourceReader);
/// using var transformer = new FixedWidthTransformer&lt;LegacyRecord, ModernRecord&gt;(
///     legacy =&gt; new ModernRecord { Id = legacy.OldId, Name = legacy.FullName });
/// using var loader = new FixedWidthLoader&lt;ModernRecord&gt;(destinationWriter);
///
/// var modern = transformer.TransformAsync(extractor.ExtractAsync(token), token);
/// await loader.LoadAsync(modern, token);
/// </code>
/// </example>
public sealed class FixedWidthTransformer<TSource, TDestination> : TransformerBase<TSource, TDestination, FixedWidthReport>
    where TSource : notnull
    where TDestination : notnull
{
    private readonly Func<TSource, TDestination> _transform;

    private readonly IProgressTimer? _progressTimer;

    private bool _progressTimerWired;

    private static Func<TSource, TDestination>? _propertyMapper;



    /// <summary>
    /// Creates a transformer that projects each source record with <paramref name="transform"/>.
    /// The projection handles every reformatting case — reordered, added, removed, or
    /// format-converted fields.
    /// </summary>
    /// <param name="transform">The per-record projection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    public FixedWidthTransformer(Func<TSource, TDestination> transform)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }



    // Test-only constructor that injects a deterministic progress timer.
    internal FixedWidthTransformer(Func<TSource, TDestination> transform, IProgressTimer timer)
        : this(transform)
    {
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
    }



    /// <summary>
    /// Creates a transformer that copies every source property to the destination
    /// property of the same name and an assignable type. <typeparamref name="TDestination"/>
    /// must have a public parameterless constructor. Use the projection constructor for
    /// anything beyond a straight same-name copy (reordering, format conversion, combining
    /// fields).
    /// </summary>
#pragma warning disable CA1000 // static factory on a generic type is the natural call site — both type args are required regardless
    public static FixedWidthTransformer<TSource, TDestination> ByMatchingProperties()
        => new(_propertyMapper ??= BuildPropertyMapper());
#pragma warning restore CA1000



    /// <inheritdoc/>
    protected override async IAsyncEnumerable<TDestination> TransformWorkerAsync
    (
        IAsyncEnumerable<TSource> source,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (EqualityComparer<TSource>.Default.Equals(item, default!))
            {
                throw new InvalidOperationException("Cannot transform a null source record.");
            }

            if (CurrentSkippedItemCount < SkipItemCount)
            {
                IncrementCurrentSkippedItemCount();
                continue;
            }

            if (CurrentItemCount >= MaximumItemCount)
            {
                break;
            }

            var result = _transform(item);
            IncrementCurrentItemCount();
            yield return result;
        }
    }



    /// <inheritdoc/>
    protected override FixedWidthReport CreateProgressReport()
    {
        return new FixedWidthReport
        (
            CurrentItemCount,
            CurrentSkippedItemCount,
            currentRejectedItemCount: 0,
            currentFilteredLineCount: 0,
            currentLineNumber: CurrentItemCount + CurrentSkippedItemCount
        );
    }



    /// <inheritdoc/>
    protected override IProgressTimer CreateProgressTimer(IProgress<FixedWidthReport> progress)
    {
        if (_progressTimer != null)
        {
            if (!_progressTimerWired)
            {
                _progressTimerWired = true;
                _progressTimer.Elapsed += () => progress.Report(CreateProgressReport());
            }

            return _progressTimer;
        }

        return base.CreateProgressTimer(progress);
    }



    private static Func<TSource, TDestination> BuildPropertyMapper()
    {
        var constructor = typeof(TDestination).GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException
            (
                $"Type '{typeof(TDestination).FullName}' needs a public parameterless constructor " +
                "for FixedWidthTransformer.ByMatchingProperties()."
            );

        var destinationProperties = typeof(TDestination).GetProperties()
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .ToDictionary(p => p.Name, StringComparer.Ordinal);

        var source = Expression.Parameter(typeof(TSource), "source");
        var destination = Expression.Variable(typeof(TDestination), "destination");

        var body = new List<Expression> { Expression.Assign(destination, Expression.New(constructor)) };

        foreach (var sourceProperty in typeof(TSource).GetProperties()
                     .Where(p => p.GetMethod != null && p.GetMethod.IsPublic))
        {
            if (destinationProperties.TryGetValue(sourceProperty.Name, out var destinationProperty)
                && destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
            {
                body.Add
                (
                    Expression.Assign
                    (
                        Expression.Property(destination, destinationProperty),
                        Expression.Property(source, sourceProperty)
                    )
                );
            }
        }

        body.Add(destination);

        var block = Expression.Block(new[] { destination }, body);
        return Expression.Lambda<Func<TSource, TDestination>>(block, source).Compile();
    }
}
