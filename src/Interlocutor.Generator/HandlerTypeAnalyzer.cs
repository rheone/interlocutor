namespace Interlocutor.Generator;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// Semantic-model-based analysis of one candidate type declaration: does it implement any of the four
/// mediator handler contracts, and/or one of the three request marker interfaces the missing-handler
/// diagnostic checks.
/// </summary>
/// <remarks>
/// Every match here is decided by resolving <see cref="WellKnownTypeNames"/> to real symbols via
/// <see cref="Compilation.GetTypeByMetadataName(string)"/> and comparing <see cref="INamedTypeSymbol.OriginalDefinition"/>
/// with <see cref="SymbolEqualityComparer.Default"/> — never by matching interface names as text. This is what
/// makes discovery correct for aliased usings, nested types, and types outside the generator's own syntax tree.
/// </remarks>
internal static class HandlerTypeAnalyzer
{
    private static readonly SymbolDisplayFormat QualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Analyzes <paramref name="candidate"/> against the four handler contracts and three request markers,
    /// resolved from <paramref name="compilation"/>.
    /// </summary>
    /// <param name="candidate">The concrete, non-abstract named type to analyze.</param>
    /// <param name="declarationLocation">The type declaration's identifier location, used for diagnostics.</param>
    /// <param name="compilation">The compilation to resolve well-known contract types from.</param>
    /// <returns>
    /// The handler contracts <paramref name="candidate"/> implements (possibly more than one, possibly none)
    /// and the request marker it implements (at most one), or <see cref="TypeAnalysisResult.Empty"/> if neither.
    /// </returns>
    public static TypeAnalysisResult Analyze(INamedTypeSymbol candidate, Location declarationLocation, Compilation compilation)
    {
        if (candidate.IsAbstract || candidate.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return TypeAnalysisResult.Empty;
        }

        var queryHandlerContract = compilation.GetTypeByMetadataName(WellKnownTypeNames.QueryHandler);
        var commandHandlerContract = compilation.GetTypeByMetadataName(WellKnownTypeNames.CommandHandler);
        var voidCommandHandlerContract = compilation.GetTypeByMetadataName(WellKnownTypeNames.VoidCommandHandler);
        var notificationHandlerContract = compilation.GetTypeByMetadataName(WellKnownTypeNames.NotificationHandler);
        var queryMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Query);
        var commandMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Command);
        var voidCommandMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.VoidCommand);

        var handlers = ImmutableArray.CreateBuilder<HandlerModel>();
        RequestModel? request = null;

        foreach (var candidateInterface in candidate.AllInterfaces)
        {
            if (!candidateInterface.IsGenericType)
            {
                // The only non-generic contract either side of this analysis cares about is the bare
                // ICommand marker itself.
                if (SymbolEqualityComparer.Default.Equals(candidateInterface, voidCommandMarker))
                {
                    request = new RequestModel(RequestKind.VoidCommand, Format(candidate), null, declarationLocation);
                }

                continue;
            }

            var openDefinition = candidateInterface.OriginalDefinition;

            if (SymbolEqualityComparer.Default.Equals(openDefinition, queryHandlerContract))
            {
                var args = candidateInterface.TypeArguments;
                handlers.Add(
                    new HandlerModel(
                        HandlerKind.Query,
                        Format(candidate),
                        Format(args[0]),
                        Format(args[1]),
                        Format(candidateInterface)
                    )
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, commandHandlerContract))
            {
                var args = candidateInterface.TypeArguments;
                handlers.Add(
                    new HandlerModel(
                        HandlerKind.Command,
                        Format(candidate),
                        Format(args[0]),
                        Format(args[1]),
                        Format(candidateInterface)
                    )
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, voidCommandHandlerContract))
            {
                var args = candidateInterface.TypeArguments;
                handlers.Add(
                    new HandlerModel(
                        HandlerKind.VoidCommand,
                        Format(candidate),
                        Format(args[0]),
                        Format(args[1]),
                        Format(candidateInterface)
                    )
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, notificationHandlerContract))
            {
                var args = candidateInterface.TypeArguments;
                handlers.Add(
                    new HandlerModel(
                        HandlerKind.Notification,
                        Format(candidate),
                        Format(args[0]),
                        null,
                        Format(candidateInterface)
                    )
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, queryMarker))
            {
                request = new RequestModel(
                    RequestKind.Query,
                    Format(candidate),
                    Format(candidateInterface.TypeArguments[0]),
                    declarationLocation
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, commandMarker))
            {
                request = new RequestModel(
                    RequestKind.Command,
                    Format(candidate),
                    Format(candidateInterface.TypeArguments[0]),
                    declarationLocation
                );
            }
        }

        return handlers.Count == 0 && request is null
            ? TypeAnalysisResult.Empty
            : new TypeAnalysisResult(handlers.ToImmutable(), request);
    }

    /// <summary>Formats a symbol as a fully-qualified, <c>global::</c>-prefixed source-safe type name.</summary>
    private static string Format(ITypeSymbol symbol) => symbol.ToDisplayString(QualifiedFormat);
}
