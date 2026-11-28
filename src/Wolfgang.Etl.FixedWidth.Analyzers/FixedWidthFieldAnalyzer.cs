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
            context.Compilation.GetTypeByMetadataName("System.TimeSpan"),
            context.Compilation.GetTypeByMetadataName("System.DateOnly"),
            context.Compilation.GetTypeByMetadataName("System.TimeOnly")
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

        AnalyzeTemporalFormat(context, known, property, attribute, location);
    }



    // FW004 / FW005 — date/time fields need a field-sized Format. DateOnly / TimeOnly without one
    // use the converter's compact default, so they skip FW004 and FW005 checks that default.
    private static void AnalyzeTemporalFormat
    (
        SymbolAnalysisContext context,
        KnownSymbols known,
        IPropertySymbol property,
        AttributeData attribute,
        Location location
    )
    {
        if (!IsTemporal(property.Type, known))
        {
            return;
        }

        var format = GetFormat(attribute);
        if (string.IsNullOrEmpty(format))
        {
            format = DefaultFormat(property.Type, known);
        }

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

        // Compare the minimum WRITTEN width, not the pattern length: quoted literals, escapes and
        // optional/variable-width specifiers make the two differ. Anything not provably too wide
        // (standard single-character specifiers, variable-width tokens) is not reported.
        if (TryGetLength(attribute, out var length))
        {
            var minimumWidth = MinimumFormattedWidth(format!);
            if (minimumWidth > length)
            {
                context.ReportDiagnostic(Diagnostic.Create
                (
                    FixedWidthDiagnostics.FormatWiderThanField,
                    location,
                    format,
                    minimumWidth,
                    property.Name,
                    length
                ));
            }
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
            INamedTypeSymbol? timeSpan,
            INamedTypeSymbol? dateOnly,
            INamedTypeSymbol? timeOnly
        )
        {
            FieldAttribute = fieldAttribute;
            SkipAttribute = skipAttribute;
            DateTime = dateTime;
            DateTimeOffset = dateTimeOffset;
            TimeSpan = timeSpan;
            DateOnly = dateOnly;
            TimeOnly = timeOnly;
        }

        public INamedTypeSymbol FieldAttribute { get; }
        public INamedTypeSymbol? SkipAttribute { get; }
        public INamedTypeSymbol DateTime { get; }
        public INamedTypeSymbol? DateTimeOffset { get; }
        public INamedTypeSymbol? TimeSpan { get; }

        // Null when the compilation targets a framework without them (before .NET 6).
        public INamedTypeSymbol? DateOnly { get; }
        public INamedTypeSymbol? TimeOnly { get; }
    }



    private static bool Matches(AttributeData attribute, INamedTypeSymbol attributeType)
        => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType);



    private static bool IsTemporal(ITypeSymbol type, KnownSymbols known)
    {
        type = UnwrapNullable(type);

        return SymbolEqualityComparer.Default.Equals(type, known.DateTime)
            || (known.DateTimeOffset is not null && SymbolEqualityComparer.Default.Equals(type, known.DateTimeOffset))
            || (known.TimeSpan is not null && SymbolEqualityComparer.Default.Equals(type, known.TimeSpan))
            || IsDateOnlyOrTimeOnly(type, known);
    }



    // The Format FixedWidthConverter uses for a field that sets none: "yyyyMMdd" for DateOnly and
    // "HHmmss" for TimeOnly (keep in step with FixedWidthConverter.DefaultDateOnlyFormat /
    // DefaultTimeOnlyFormat); null for DateTime / DateTimeOffset / TimeSpan, which have no default.
    private static string? DefaultFormat(ITypeSymbol type, KnownSymbols known)
    {
        type = UnwrapNullable(type);

        if (known.DateOnly is not null && SymbolEqualityComparer.Default.Equals(type, known.DateOnly))
        {
            return "yyyyMMdd";
        }

        if (known.TimeOnly is not null && SymbolEqualityComparer.Default.Equals(type, known.TimeOnly))
        {
            return "HHmmss";
        }

        return null;
    }



    private static bool IsDateOnlyOrTimeOnly(ITypeSymbol type, KnownSymbols known)
        => DefaultFormat(type, known) is not null;



    // Unwrap Nullable<T> so DateTime? / TimeSpan? / DateOnly? are treated the same as their
    // underlying value type.
    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }



    /// <summary>
    /// The smallest number of characters a custom date/time or time-span <paramref name="format"/>
    /// can write. Quoted literals and backslash escapes count as their literal text; a
    /// single-character pattern is a standard specifier (width unknown) and counts as 0; optional
    /// (<c>F</c>), zone (<c>z</c>, <c>K</c>) and era (<c>g</c>) tokens count as 0 and name-based
    /// tokens (<c>MMM</c>, <c>ddd</c>) as 3, so the result never exceeds the real width.
    /// </summary>
    internal static int MinimumFormattedWidth(string format)
    {
        if (format.Length == 1)
        {
            return 0;
        }

        var width = 0;
        var i = 0;
        while (i < format.Length)
        {
            var c = format[i];
            if (c == '\'' || c == '"')
            {
                var close = format.IndexOf(c, i + 1);
                var end = close < 0 ? format.Length : close;
                width += end - i - 1;
                i = end + 1;
            }
            else if (c == '\\')
            {
                width += i + 1 < format.Length ? 1 : 0;
                i += 2;
            }
            else if (c == '%')
            {
                i++;
            }
            else
            {
                var run = 1;
                while (i + run < format.Length && format[i + run] == c)
                {
                    run++;
                }

                width += TokenMinimumWidth(c, run);
                i += run;
            }
        }

        return width;
    }



    private static int TokenMinimumWidth(char token, int run)
    {
        switch (token)
        {
            case 'F':
            case 'z':
            case 'K':
            case 'g':
                return 0;
            case 'M':
            case 'd':
                return run >= 3 ? 3 : run;
            default:
                return run;
        }
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
