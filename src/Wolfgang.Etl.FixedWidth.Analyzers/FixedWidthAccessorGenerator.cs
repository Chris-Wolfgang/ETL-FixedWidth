using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wolfgang.Etl.FixedWidth.Analyzers;

/// <summary>
/// Incremental source generator that emits, for every type carrying
/// <c>[FixedWidthField]</c> properties, a factory delegate plus per-property getter and
/// setter delegates, and registers them with
/// <c>Wolfgang.Etl.FixedWidth.Generated.GeneratedAccessorRegistry</c> from a
/// <c>[ModuleInitializer]</c>. The runtime field-mapping path prefers these direct-access
/// delegates over reflection-compiled expression trees, removing the last
/// <c>RequiresDynamicCode</c> path and making extraction and loading Native AOT
/// compatible (see #12, #13).
/// </summary>
/// <remarks>
/// The emitted code is wrapped in <c>#if NET5_0_OR_GREATER</c>: module initializers only
/// exist on net5.0+, and those are also the only targets where Native AOT / trimming
/// apply. On older targets (net462/net481/netstandard2.0) the generated file compiles to
/// nothing and the runtime transparently falls back to the reflection-compiled delegates.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class FixedWidthAccessorGenerator : IIncrementalGenerator
{
    private const string FieldAttributeFullName = "Wolfgang.Etl.FixedWidth.Attributes.FixedWidthFieldAttribute";
    private const string RegistryFullName = "global::Wolfgang.Etl.FixedWidth.Generated.GeneratedAccessorRegistry";

    // Control characters used as internal delimiters when flattening the per-type model
    // into value-equatable strings. They can never appear in a C# identifier or a
    // fully-qualified type name, so encode/decode is unambiguous.
    private const char GroupSeparator = (char)29;
    private const char FieldSeparator = (char)31;

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName
            (
                FieldAttributeFullName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax,
                transform: static (ctx, _) => BuildModel(ctx)
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m!)
            .Collect()
            .SelectMany(static (all, _) => Distinct(all));

        context.RegisterSourceOutput(models, static (spc, model) => Emit(spc, model));
    }



    // ------------------------------------------------------------------
    // Model — value-equatable, strings only (no ISymbol retained, so the
    // incremental cache works correctly across compilations).
    // ------------------------------------------------------------------

    private sealed record AccessorModel
    (
        string TypeFullyQualified,
        string MangledName,
        bool CanConstruct,
        string MembersEncoded
    );



    private static AccessorModel? BuildModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol.ContainingType is not INamedTypeSymbol type)
        {
            return null;
        }

        // Only concrete, accessible, non-generic classes/structs can host generated
        // accessors emitted into the consumer assembly. Anything else falls back to the
        // reflection path at runtime.
        if (type.IsGenericType
            || type.IsStatic
            || !IsAccessible(type)
            || type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return null;
        }

        var typeFullyQualified = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var members = new StringBuilder();

        foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.IsIndexer || !HasFieldAttribute(property))
            {
                continue;
            }

            var canGet = IsPublicAccessor(property.GetMethod);
            // Exclude init-only setters: a generated "obj.Prop = value" is invalid C# (CS8852).
            var canSet = IsPublicAccessor(property.SetMethod) && !property.SetMethod!.IsInitOnly;
            if (!canGet && !canSet)
            {
                continue;
            }

            if (members.Length > 0)
            {
                members.Append(GroupSeparator);
            }

            var castType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            members.Append(property.Name)
                .Append(FieldSeparator).Append(castType)
                .Append(FieldSeparator).Append(canGet ? '1' : '0')
                .Append(FieldSeparator).Append(canSet ? '1' : '0');
        }

        var canConstruct = CanConstruct(type);
        if (members.Length == 0 && !canConstruct)
        {
            return null;
        }

        return new AccessorModel
        (
            typeFullyQualified,
            Mangle(typeFullyQualified),
            canConstruct,
            members.ToString()
        );
    }



    // ------------------------------------------------------------------
    // Emit
    // ------------------------------------------------------------------

    private static void Emit(SourceProductionContext context, AccessorModel model)
    {
        var members = DecodeMembers(model.MembersEncoded);
        var type = model.TypeFullyQualified;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#if NET5_0_OR_GREATER");
        builder.AppendLine("namespace Wolfgang.Etl.FixedWidth.Generated");
        builder.AppendLine("{");
        builder.AppendLine($"    internal static class FixedWidthAccessors_{model.MangledName}");
        builder.AppendLine("    {");

        if (model.CanConstruct)
        {
            builder.AppendLine($"        internal static object Create() => new {type}();");
        }

        foreach (var member in members)
        {
            if (member.CanGet)
            {
                builder.AppendLine($"        internal static object? Get_{member.Name}(object instance) => (({type})instance).{member.Name};");
            }

            if (member.CanSet)
            {
                builder.AppendLine($"        internal static void Set_{member.Name}(object instance, object? value) => (({type})instance).{member.Name} = ({member.CastType})value!;");
            }
        }

        builder.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("        internal static void Initialize()");
        builder.AppendLine("        {");

        if (model.CanConstruct)
        {
            builder.AppendLine($"            {RegistryFullName}.RegisterFactory(typeof({type}), Create);");
        }

        foreach (var member in members)
        {
            if (member.CanGet)
            {
                builder.AppendLine($"            {RegistryFullName}.RegisterGetter(typeof({type}), \"{member.Name}\", Get_{member.Name});");
            }

            if (member.CanSet)
            {
                builder.AppendLine($"            {RegistryFullName}.RegisterSetter(typeof({type}), \"{member.Name}\", Set_{member.Name});");
            }
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine("#endif");

        context.AddSource($"FixedWidthAccessors_{model.MangledName}.g.cs", builder.ToString());
    }



    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private sealed class Member
    {
        public Member(string name, string castType, bool canGet, bool canSet)
        {
            Name = name;
            CastType = castType;
            CanGet = canGet;
            CanSet = canSet;
        }

        public string Name { get; }
        public string CastType { get; }
        public bool CanGet { get; }
        public bool CanSet { get; }
    }



    private static IReadOnlyList<Member> DecodeMembers(string encoded)
    {
        if (encoded.Length == 0)
        {
            return System.Array.Empty<Member>();
        }

        var result = new List<Member>();
        foreach (var chunk in encoded.Split(GroupSeparator))
        {
            var parts = chunk.Split(FieldSeparator);
            result.Add(new Member(
                parts[0],
                parts[1],
                string.Equals(parts[2], "1", System.StringComparison.Ordinal),
                string.Equals(parts[3], "1", System.StringComparison.Ordinal)));
        }

        return result;
    }



    private static IEnumerable<AccessorModel> Distinct(ImmutableArray<AccessorModel> models)
    {
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var model in models)
        {
            if (seen.Add(model.TypeFullyQualified))
            {
                yield return model;
            }
        }
    }



    private static bool HasFieldAttribute(IPropertySymbol property)
        => property.GetAttributes().Any(a => string.Equals(a.AttributeClass?.ToDisplayString(), FieldAttributeFullName, System.StringComparison.Ordinal));



    private static bool IsPublicAccessor(IMethodSymbol? accessor)
        => accessor is not null && accessor.DeclaredAccessibility == Accessibility.Public;



    private static bool IsAccessible(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }



    private static bool CanConstruct(INamedTypeSymbol type)
    {
        if (type.IsAbstract || type.IsStatic)
        {
            return false;
        }

        // Structs are always constructible via new S(). A class needs an accessible
        // parameterless constructor (the generated code lives in the consumer assembly,
        // so an internal constructor is reachable too).
        return type.TypeKind == TypeKind.Struct
            || type.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0
                && c.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal);
    }



    /// <summary>
    /// Maps a fully-qualified type name to a unique, deterministic identifier suffix for
    /// the generated class name and hint name. A naive "replace non-alphanumeric with '_'"
    /// is not injective (<c>Ns.A_B</c> and <c>Ns_A.B</c> collapse to the same string), so
    /// an FNV-1a 32-bit hash of the original name is appended to guarantee uniqueness.
    /// The hash is hand-rolled rather than <see cref="string.GetHashCode()"/> because the
    /// latter is randomized per process on .NET Core, which would make generator output
    /// non-deterministic build-to-build.
    /// </summary>
    private static string Mangle(string fullyQualifiedName)
    {
        var builder = new StringBuilder(fullyQualifiedName.Length + 9);
        foreach (var c in fullyQualifiedName)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        builder.Append('_');
        builder.Append(StableHash(fullyQualifiedName).ToString("x8", CultureInfo.InvariantCulture));
        return builder.ToString();
    }



    private static uint StableHash(string value)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;
            var hash = offsetBasis;
            foreach (var c in value)
            {
                hash ^= c;
                hash *= prime;
            }

            return hash;
        }
    }
}
