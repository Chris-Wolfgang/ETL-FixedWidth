using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Wolfgang.Etl.FixedWidth.Analyzers;

/// <summary>
/// Reports compile-time diagnostics for <c>[FixedWidthField]</c> / <c>[FixedWidthSkip]</c>
/// layout mistakes (#27) — duplicate indexes (FW003), date/time fields without a Format
/// (FW004), a Format wider than its field (FW005), and mapped properties with no public
/// setter (FW007) or getter (FW008). See <see cref="FixedWidthDiagnostics"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FixedWidthFieldAnalyzer : DiagnosticAnalyzer
{
    private const string FieldAttributeFullName = "Wolfgang.Etl.FixedWidth.Attributes.FixedWidthFieldAttribute";
    private const string SkipAttributeFullName = "Wolfgang.Etl.FixedWidth.Attributes.FixedWidthSkipAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            FixedWidthDiagnostics.DuplicateIndex,
            FixedWidthDiagnostics.MissingTemporalFormat,
            FixedWidthDiagnostics.FormatWiderThanField,
            FixedWidthDiagnostics.NoPublicSetter,
            FixedWidthDiagnostics.NoPublicGetter
        );



    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new System.ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }



    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var fieldAttribute = context.Compilation.GetTypeByMetadataName(FieldAttributeFullName);
        if (fieldAttribute is null)
        {
            // The compilation does not reference Wolfgang.Etl.FixedWidth — nothing to check.
            return;
        }

        var known = new KnownSymbols
        (
            fieldAttribute,
            context.Compilation.GetTypeByMetadataName(SkipAttributeFullName),
            context.Compilation.GetSpecialType(SpecialType.System_DateTime),
            context.Compilation.GetTypeByMetadataName("System.DateTimeOffset"),
            context.Compilation.GetTypeByMetadataName("System.TimeSpan")
        );

        context.RegisterSymbolAction(c => AnalyzeType(c, known), SymbolKind.NamedType);
    }



    private static void AnalyzeType(SymbolAnalysisContext context, KnownSymbols known)
    {
        if (context.Symbol is not INamedTypeSymbol type
            || type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return;
        }

        var indexed = new List<(int Index, Location Location)>();

        foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.IsIndexer)
            {
                continue;
            }

            foreach (var attribute in property.GetAttributes())
            {
                if (Matches(attribute, known.FieldAttribute))
                {
                    AnalyzeField(context, known, property, attribute, indexed);
                }
                else if (known.SkipAttribute is not null
                         && Matches(attribute, known.SkipAttribute)
                         && TryGetIndex(attribute, out var skipIndex))
                {
                    indexed.Add((skipIndex, GetLocation(attribute, property, context.CancellationToken)));
                }
            }
        }

        ReportDuplicateIndexes(context, type, indexed);
    }



    private static void AnalyzeField
    (
        SymbolAnalysisContext context,
        KnownSymbols known,
        IPropertySymbol property,
        AttributeData attribute,
        List<(int Index, Location Location)> indexed
    )
    {
        var location = GetLocation(attribute, property, context.CancellationToken);

        if (TryGetIndex(attribute, out var index))
        {
            indexed.Add((index, location));
        }

        // FW007 / FW008 — a mapped property must be publicly settable (extraction) and,
        // for loading, publicly readable.
        if (property.SetMethod is null || property.SetMethod.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(FixedWidthDiagnostics.NoPublicSetter, location, property.Name));
        }

        if (property.GetMethod is null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(FixedWidthDiagnostics.NoPublicGetter, location, property.Name));
        }

        // FW004 / FW005 — date/time fields need an explicit, field-sized Format.
        if (!IsTemporal(property.Type, known))
        {
            return;
        }

        var format = GetFormat(attribute);
        if (string.IsNullOrEmpty(format))
        {
            context.ReportDiagnostic(Diagnostic.Create
            (
                FixedWidthDiagnostics.MissingTemporalFormat,
                location,
                property.Name,
                property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            ));
            return;
        }

        if (TryGetLength(attribute, out var length) && format!.Length > length)
        {
            context.ReportDiagnostic(Diagnostic.Create
            (
                FixedWidthDiagnostics.FormatWiderThanField,
                location,
                format,
                format.Length,
                property.Name,
                length
            ));
        }
    }



    private static void ReportDuplicateIndexes
    (
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        List<(int Index, Location Location)> indexed
    )
    {
        foreach (var group in indexed.GroupBy(e => e.Index).Where(g => g.Count() > 1))
        {
            foreach (var entry in group)
            {
                context.ReportDiagnostic(Diagnostic.Create
                (
                    FixedWidthDiagnostics.DuplicateIndex,
                    entry.Location,
                    group.Key,
                    type.Name
                ));
            }
        }
    }



    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private sealed class KnownSymbols
    {
        public KnownSymbols
        (
            INamedTypeSymbol fieldAttribute,
            INamedTypeSymbol? skipAttribute,
            INamedTypeSymbol dateTime,
            INamedTypeSymbol? dateTimeOffset,
            INamedTypeSymbol? timeSpan
        )
        {
            FieldAttribute = fieldAttribute;
            SkipAttribute = skipAttribute;
            DateTime = dateTime;
            DateTimeOffset = dateTimeOffset;
            TimeSpan = timeSpan;
        }

        public INamedTypeSymbol FieldAttribute { get; }
        public INamedTypeSymbol? SkipAttribute { get; }
        public INamedTypeSymbol DateTime { get; }
        public INamedTypeSymbol? DateTimeOffset { get; }
        public INamedTypeSymbol? TimeSpan { get; }
    }



    private static bool Matches(AttributeData attribute, INamedTypeSymbol attributeType)
        => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType);



    private static bool IsTemporal(ITypeSymbol type, KnownSymbols known)
    {
        // Unwrap Nullable<T> so DateTime? / TimeSpan? are treated the same as their
        // underlying value type.
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            type = named.TypeArguments[0];
        }

        return SymbolEqualityComparer.Default.Equals(type, known.DateTime)
            || (known.DateTimeOffset is not null && SymbolEqualityComparer.Default.Equals(type, known.DateTimeOffset))
            || (known.TimeSpan is not null && SymbolEqualityComparer.Default.Equals(type, known.TimeSpan));
    }



    private static bool TryGetIndex(AttributeData attribute, out int index)
        => TryGetPrimitiveInt(attribute, 0, out index);



    private static bool TryGetLength(AttributeData attribute, out int length)
        => TryGetPrimitiveInt(attribute, 1, out length);



    private static bool TryGetPrimitiveInt(AttributeData attribute, int position, out int value)
    {
        value = 0;
        if (attribute.ConstructorArguments.Length <= position)
        {
            return false;
        }

        var argument = attribute.ConstructorArguments[position];
        if (argument.Kind != TypedConstantKind.Primitive || argument.Value is not int intValue)
        {
            return false;
        }

        value = intValue;
        return true;
    }



    private static string? GetFormat(AttributeData attribute)
    {
        // foreach kept over LINQ Where — attribute.NamedArguments is a small
        // ImmutableArray (typically 0-2 entries) and the analyzer runs on every
        // symbol per compilation, so avoiding the LINQ allocation matters more
        // than the syntactic sugar the S3267 hint would suggest.
#pragma warning disable S3267
        foreach (var named in attribute.NamedArguments)
#pragma warning restore S3267
        {
            if (string.Equals(named.Key, "Format", System.StringComparison.Ordinal))
            {
                return named.Value.Value as string;
            }
        }

        return null;
    }



    private static Location GetLocation(AttributeData attribute, ISymbol fallback, CancellationToken cancellationToken)
        => attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
            ?? fallback.Locations.FirstOrDefault()
            ?? Location.None;
}
