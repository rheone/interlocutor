namespace Interlocutor.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// Semantic-model-based analysis of one candidate type declaration: does it implement any of the four
/// mediator handler contracts (closed or, for query/command, open generic), and/or one of the three request
/// marker interfaces the missing-handler diagnostic checks.
/// </summary>
/// <remarks>
/// Every match here is decided by resolving <see cref="WellKnownTypeNames"/> to real symbols via
/// <see cref="Compilation.GetTypeByMetadataName(string)"/> and comparing <see cref="INamedTypeSymbol.OriginalDefinition"/>
/// with <see cref="SymbolEqualityComparer.Default"/> - never by matching interface names as text. This is what
/// makes discovery correct for aliased usings, nested types, and types outside the generator's own syntax tree.
/// </remarks>
internal static class HandlerTypeAnalyzer
{
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
        if (
            candidate.IsAbstract
            || candidate.TypeKind is not (TypeKind.Class or TypeKind.Struct)
            || !IsAccessibleFromGeneratedCode(candidate)
        )
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
        var runAfterAttribute = compilation.GetTypeByMetadataName(WellKnownTypeNames.NotificationHandlerRunAfter);
        var runBeforeAttribute = compilation.GetTypeByMetadataName(WellKnownTypeNames.NotificationHandlerRunBefore);
        var handlerOverrideAttribute = compilation.GetTypeByMetadataName(WellKnownTypeNames.HandlerOverride);

        var handlers = ImmutableArray.CreateBuilder<HandlerModel>();
        var openGenericHandlers = ImmutableArray.CreateBuilder<OpenGenericHandlerModel>();
        RequestModel? request = null;

        foreach (var candidateInterface in candidate.AllInterfaces)
        {
            if (!candidateInterface.IsGenericType)
            {
                // The only non-generic contract either side of this analysis cares about is the bare
                // ICommand marker itself.
                if (SymbolEqualityComparer.Default.Equals(candidateInterface, voidCommandMarker))
                {
                    request = new RequestModel(
                        RequestKind.VoidCommand,
                        SymbolFormatting.Format(candidate),
                        null,
                        declarationLocation
                    );
                }

                continue;
            }

            var openDefinition = candidateInterface.OriginalDefinition;

            if (SymbolEqualityComparer.Default.Equals(openDefinition, queryHandlerContract))
            {
                AddQueryOrCommandHandler(
                    HandlerKind.Query,
                    candidate,
                    candidateInterface,
                    declarationLocation,
                    handlers,
                    openGenericHandlers,
                    handlerOverrideAttribute
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, commandHandlerContract))
            {
                AddQueryOrCommandHandler(
                    HandlerKind.Command,
                    candidate,
                    candidateInterface,
                    declarationLocation,
                    handlers,
                    openGenericHandlers,
                    handlerOverrideAttribute
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, voidCommandHandlerContract))
            {
                var args = candidateInterface.TypeArguments;
                handlers.Add(
                    new HandlerModel(
                        HandlerKind.VoidCommand,
                        SymbolFormatting.Format(candidate),
                        SymbolFormatting.Format(args[0]),
                        SymbolFormatting.Format(args[1]),
                        SymbolFormatting.Format(candidateInterface),
                        declarationLocation
                    )
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, notificationHandlerContract))
            {
                var args = candidateInterface.TypeArguments;
                handlers.Add(
                    new HandlerModel(
                        HandlerKind.Notification,
                        SymbolFormatting.Format(candidate),
                        SymbolFormatting.Format(args[0]),
                        null,
                        SymbolFormatting.Format(candidateInterface),
                        declarationLocation,
                        RunAfter: ResolveOrderingAttribute(candidate, runAfterAttribute),
                        RunBefore: ResolveOrderingAttribute(candidate, runBeforeAttribute)
                    )
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, queryMarker) && !candidate.IsGenericType)
            {
                // An open generic request declaration itself (e.g. GenericQuery<T> : IQuery<...>) is not a
                // concrete request the missing-handler diagnostic can check - only its closed usages, found
                // separately by GenericUsageAnalyzer, ever need a handler (see ticket 11).
                request = new RequestModel(
                    RequestKind.Query,
                    SymbolFormatting.Format(candidate),
                    SymbolFormatting.Format(candidateInterface.TypeArguments[0]),
                    declarationLocation
                );
            }
            else if (SymbolEqualityComparer.Default.Equals(openDefinition, commandMarker) && !candidate.IsGenericType)
            {
                request = new RequestModel(
                    RequestKind.Command,
                    SymbolFormatting.Format(candidate),
                    SymbolFormatting.Format(candidateInterface.TypeArguments[0]),
                    declarationLocation
                );
            }
        }

        return handlers.Count == 0 && openGenericHandlers.Count == 0 && request is null
            ? TypeAnalysisResult.Empty
            : new TypeAnalysisResult(handlers.ToImmutable(), openGenericHandlers.ToImmutable(), request);
    }

    /// <summary>
    /// Handles one <c>IQueryHandler&lt;,&gt;</c>/<c>ICommandHandler&lt;,&gt;</c> match: routes it to
    /// <paramref name="handlers"/> as a normal closed handler, unless <paramref name="candidate"/> is itself an
    /// open generic type closing over an open generic request type used positionally as its own type
    /// parameters, in which case it is routed to <paramref name="openGenericHandlers"/> instead (ticket 11).
    /// </summary>
    private static void AddQueryOrCommandHandler(
        HandlerKind kind,
        INamedTypeSymbol candidate,
        INamedTypeSymbol candidateInterface,
        Location declarationLocation,
        ImmutableArray<HandlerModel>.Builder handlers,
        ImmutableArray<OpenGenericHandlerModel>.Builder openGenericHandlers,
        INamedTypeSymbol? handlerOverrideAttribute
    )
    {
        var args = candidateInterface.TypeArguments;
        var requestTypeArgument = args[0];

        if (
            candidate.IsGenericType
            && candidate.TypeParameters.Length > 0
            && requestTypeArgument is INamedTypeSymbol { IsGenericType: true } requestType
            && requestType.TypeArguments.Length > 0
            && requestType.TypeArguments.All(a => a.TypeKind == TypeKind.TypeParameter)
        )
        {
            var requestArgumentParameterNames = requestType
                .TypeArguments.Cast<ITypeParameterSymbol>()
                .Select(tp => tp.Name)
                .ToImmutableArray();

            // Every type argument position on the request must be one of this handler's own type parameters,
            // and every one of the handler's own type parameters must appear there exactly once - a handler
            // mixing in some other fixed/unrelated type parameter at a request position, or declaring a type
            // parameter the request never uses, is not a supported "closes a family of generic requests" shape
            // (a leftover, un-substituted name would make the closed handler/response/service-interface
            // templates invalid source), so it falls through to being excluded below.
            var requestParameterSymbols = requestType.TypeArguments.Cast<ITypeParameterSymbol>().ToImmutableArray();
            var handlerOwnsEveryRequestParameter =
                requestParameterSymbols.Length == candidate.TypeParameters.Length
                && requestParameterSymbols.All(tp => SymbolEqualityComparer.Default.Equals(tp.ContainingSymbol, candidate))
                && candidate.TypeParameters.All(tp => requestParameterSymbols.Contains(tp, SymbolEqualityComparer.Default));

            if (handlerOwnsEveryRequestParameter)
            {
                openGenericHandlers.Add(
                    new OpenGenericHandlerModel(
                        kind,
                        SymbolFormatting.FormatDefinitionKey(candidate),
                        SymbolFormatting.Format(candidate),
                        SymbolFormatting.FormatDefinitionKey(requestType),
                        SymbolFormatting.Format(args[1]),
                        SymbolFormatting.Format(candidateInterface),
                        requestArgumentParameterNames,
                        BuildConstraints(candidate),
                        ResolveHandlerOverrideAttribute(candidate, handlerOverrideAttribute, out var overrideDisplayNames),
                        overrideDisplayNames,
                        declarationLocation
                    )
                );
                return;
            }
        }

        if (candidate.IsGenericType)
        {
            // A generic handler type that does not close over a generic request family in the supported shape
            // is excluded entirely rather than emitted as a closed handler: formatting an unbound generic type
            // name would not be valid source at a typeof(...) site.
            return;
        }

        handlers.Add(
            new HandlerModel(
                kind,
                SymbolFormatting.Format(candidate),
                SymbolFormatting.Format(args[0]),
                SymbolFormatting.Format(args[1]),
                SymbolFormatting.Format(candidateInterface),
                declarationLocation
            )
        );
    }

    /// <summary>Builds one <see cref="GenericParameterConstraintModel"/> per type parameter <paramref name="candidate"/> declares, in order.</summary>
    private static ImmutableArray<GenericParameterConstraintModel> BuildConstraints(INamedTypeSymbol candidate) =>
        candidate
            .TypeParameters.Select(tp => new GenericParameterConstraintModel(
                tp.Name,
                tp.HasReferenceTypeConstraint,
                tp.HasValueTypeConstraint,
                tp.HasConstructorConstraint,
                tp.HasNotNullConstraint,
                tp.ConstraintTypes.Select(SymbolFormatting.TryGetMetadataName).ToImmutableArray()
            ))
            .ToImmutableArray();

    /// <summary>
    /// Resolves every <c>HandlerOverrideAttribute</c> application on <paramref name="candidate"/> into a
    /// definition-key string comparable against <see cref="OpenGenericHandlerModel.HandlerDefinitionKey"/> or
    /// <see cref="HandlerModel.HandlerTypeName"/>, plus a parallel display-name array for diagnostics.
    /// </summary>
    private static ImmutableArray<string> ResolveHandlerOverrideAttribute(
        INamedTypeSymbol candidate,
        INamedTypeSymbol? handlerOverrideAttribute,
        out ImmutableArray<string> displayNames
    )
    {
        if (handlerOverrideAttribute is null)
        {
            displayNames = [];
            return [];
        }

        var keys = ImmutableArray.CreateBuilder<string>();
        var names = ImmutableArray.CreateBuilder<string>();
        foreach (var referencedType in CollectTypeArgumentAttributes(candidate, handlerOverrideAttribute))
        {
            // An unbound generic type token (typeof(SomeHandler<>)) names an open generic handler by its
            // definition key; any other type name is compared against a closed handler's own display name.
            keys.Add(
                referencedType.IsUnboundGenericType
                    ? SymbolFormatting.FormatDefinitionKey(referencedType)
                    : SymbolFormatting.Format(referencedType)
            );
            names.Add(SymbolFormatting.Format(referencedType));
        }

        displayNames = names.ToImmutable();
        return keys.ToImmutable();
    }

    /// <summary>
    /// Collects the fully-qualified type name argument from every application of
    /// <paramref name="orderingAttribute"/> (<c>NotificationHandlerRunAfterAttribute</c> or
    /// <c>NotificationHandlerRunBeforeAttribute</c>) on <paramref name="candidate"/>.
    /// </summary>
    /// <param name="candidate">The notification handler type to inspect.</param>
    /// <param name="orderingAttribute">The resolved attribute symbol, or <see langword="null"/> if the attribute type could not be resolved.</param>
    /// <returns>One entry per attribute application, in declaration order; empty if none or the argument was not a type.</returns>
    private static ImmutableArray<string> ResolveOrderingAttribute(
        INamedTypeSymbol candidate,
        INamedTypeSymbol? orderingAttribute
    )
    {
        if (orderingAttribute is null)
        {
            return [];
        }

        return CollectTypeArgumentAttributes(candidate, orderingAttribute).Select(SymbolFormatting.Format).ToImmutableArray();
    }

    /// <summary>
    /// Collects the single <see cref="INamedTypeSymbol"/> constructor argument from every application of
    /// <paramref name="attributeType"/> on <paramref name="candidate"/> - the shape shared by
    /// <see cref="ResolveHandlerOverrideAttribute"/> and <see cref="ResolveOrderingAttribute"/>, which differ only
    /// in how the collected types are formatted.
    /// </summary>
    /// <param name="candidate">The type whose attributes to inspect.</param>
    /// <param name="attributeType">The single-<see cref="INamedTypeSymbol"/>-argument attribute to match.</param>
    /// <returns>One entry per matching attribute application, in declaration order.</returns>
    private static IEnumerable<INamedTypeSymbol> CollectTypeArgumentAttributes(
        INamedTypeSymbol candidate,
        INamedTypeSymbol attributeType
    )
    {
        foreach (var attribute in candidate.GetAttributes())
        {
            if (
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is INamedTypeSymbol referencedType
            )
            {
                yield return referencedType;
            }
        }
    }

    /// <summary>
    /// Whether <paramref name="symbol"/> - and every type it is nested inside - is visible from generated
    /// code living in an unrelated type in the same assembly (the registration module and dispatch wrappers
    /// this generator emits are never nested inside the handler's own containing type). A <see langword="private"/>,
    /// <see langword="protected"/>, or <see langword="private protected"/> handler or request type at any
    /// nesting level would compile in its own file but produce inaccessible-member errors in the generated
    /// <c>typeof(...)</c> references, so such types are silently excluded from discovery rather than treated
    /// as handlers or requests.
    /// </summary>
    private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            if (
                current.DeclaredAccessibility
                is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal)
            )
            {
                return false;
            }
        }

        return true;
    }
}
