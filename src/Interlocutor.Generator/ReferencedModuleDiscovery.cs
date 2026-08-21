namespace Interlocutor.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

/// <summary>
/// Discovers every referenced assembly's compile-time generated registration module (ticket 12), by reading
/// <c>InterlocutorGeneratedModuleAttribute</c> and <c>InterlocutorGeneratedHandlerAttribute</c> straight out
/// of each reference's assembly-level metadata - never by loading or reflecting over the referenced assembly
/// at run time, and never by re-walking its member types the way <see cref="HandlerTypeAnalyzer"/> walks the
/// current compilation's own syntax.
/// </summary>
/// <remarks>
/// <see cref="Compilation.References"/> already gives every assembly the compiling project transitively
/// depends on as a direct entry - a project reference from the application to library A, which itself
/// references library B, resolves at the MSBuild level to direct assembly references to both A.dll and
/// B.dll for the application's own compilation. This is what makes three-level referencing (application to
/// A to B) work without the application listing B anywhere: this method never has to walk from A's metadata
/// to B's, it only ever inspects the reference list Roslyn already flattened for it.
/// </remarks>
internal static class ReferencedModuleDiscovery
{
    private static readonly SymbolDisplayFormat QualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Finds every distinct referenced assembly carrying <c>InterlocutorGeneratedModuleAttribute</c>.
    /// </summary>
    /// <param name="compilation">The compiling project's compilation.</param>
    /// <param name="ct">A token that may be used to cancel the operation.</param>
    /// <returns>
    /// One <see cref="ExternalModuleInfo"/> per distinct referenced assembly identity carrying the module
    /// attribute - an assembly reachable through more than one reference path (a diamond dependency)
    /// contributes exactly one entry, since <see cref="Compilation.References"/> already de-duplicates by
    /// assembly identity before this method ever sees it.
    /// </returns>
    public static ImmutableArray<ExternalModuleInfo> Discover(Compilation compilation, CancellationToken ct)
    {
        var moduleAttributeType = compilation.GetTypeByMetadataName(WellKnownTypeNames.GeneratedModuleAttribute);
        if (moduleAttributeType is null)
        {
            // Interlocutor.DependencyInjection.Abstractions isn't referenced at all - nothing to discover.
            return [];
        }

        var handlerAttributeType = compilation.GetTypeByMetadataName(WellKnownTypeNames.GeneratedHandlerAttribute);

        var seenAssemblyIdentities = new HashSet<string>(StringComparer.Ordinal);
        var modules = ImmutableArray.CreateBuilder<ExternalModuleInfo>();

        foreach (var reference in compilation.References)
        {
            ct.ThrowIfCancellationRequested();

            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            var assemblyIdentity = assemblySymbol.Identity.ToString();
            if (!seenAssemblyIdentities.Add(assemblyIdentity))
            {
                continue;
            }

            var module = TryReadModule(assemblySymbol, moduleAttributeType, handlerAttributeType, assemblyIdentity);
            if (module is not null)
            {
                modules.Add(module);
            }
        }

        return modules.ToImmutable();
    }

    /// <summary>
    /// Reads one assembly's module attribute (if present) and every handler attribute alongside it.
    /// </summary>
    private static ExternalModuleInfo? TryReadModule(
        IAssemblySymbol assemblySymbol,
        INamedTypeSymbol moduleAttributeType,
        INamedTypeSymbol? handlerAttributeType,
        string assemblyIdentity
    )
    {
        INamedTypeSymbol? moduleType = null;
        var handlers = ImmutableArray.CreateBuilder<ExternalHandlerInfo>();

        foreach (var attribute in assemblySymbol.GetAttributes())
        {
            if (
                moduleType is null
                && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, moduleAttributeType)
                && attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is INamedTypeSymbol declaredModuleType
            )
            {
                moduleType = declaredModuleType;
                continue;
            }

            if (
                handlerAttributeType is not null
                && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, handlerAttributeType)
                && TryReadHandler(attribute, out var handler)
            )
            {
                handlers.Add(handler);
            }
        }

        return moduleType is null ? null : new ExternalModuleInfo(assemblyIdentity, Format(moduleType), handlers.ToImmutable());
    }

    /// <summary>Reads one <c>InterlocutorGeneratedHandlerAttribute</c> instance's constructor arguments.</summary>
    private static bool TryReadHandler(AttributeData attribute, out ExternalHandlerInfo handler)
    {
        handler = null!;

        if (
            attribute.ConstructorArguments.Length < 3
            || attribute.ConstructorArguments[0].Value is not ITypeSymbol requestType
            || attribute.ConstructorArguments[2].Value is not int kindValue
            || kindValue < 0
            || kindValue > (int)HandlerKind.Notification
        )
        {
            return false;
        }

        var responseType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
        handler = new ExternalHandlerInfo(
            (HandlerKind)kindValue,
            Format(requestType),
            responseType is null ? null : Format(responseType)
        );
        return true;
    }

    /// <summary>Formats a symbol as a fully-qualified, <c>global::</c>-prefixed source-safe type name.</summary>
    private static string Format(ITypeSymbol symbol) => symbol.ToDisplayString(QualifiedFormat);
}
