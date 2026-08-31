namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Reports <see cref="InterlocutorDiagnostics.ValidationShapeMismatch"/> for any <c>FluentValidation.AbstractValidator&lt;T&gt;</c>
/// whose validated type <c>T</c> is a command/query with no validation-shaped case in its declared response arity.
/// </summary>
/// <remarks>
/// <para>
/// Purely symbol-based - no syntax walking is needed, since everything the check depends on (the validator's
/// base type argument, the validated type's <c>ICommand&lt;TResponse&gt;</c>/<c>IQuery&lt;TResponse&gt;</c>
/// implementation, and the response type's own type arguments) is visible on the compilation's symbol graph
/// directly. No DI/runtime registration information is needed either - a validator and a request are linked
/// purely by <c>AbstractValidator&lt;T&gt;</c>'s type argument, the same link
/// <c>Interlocutor.FluentValidation.ValidationBehavior</c> relies on at runtime via constructor injection.
/// </para>
/// <para>
/// Resolves <c>FluentValidation.AbstractValidator&lt;T&gt;</c> by metadata name only - this project takes no
/// compile-time dependency on FluentValidation itself, so the analyzer simply reports nothing in a compilation
/// that doesn't reference it.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidationShapeMismatchAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [InterlocutorDiagnostics.ValidationShapeMismatch];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            var compilation = compilationStartContext.Compilation;
            var abstractValidator = compilation.GetTypeByMetadataName(WellKnownTypeNames.AbstractValidator);
            var commandMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Command);
            var queryMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Query);

            if (abstractValidator is null || (commandMarker is null && queryMarker is null))
            {
                // Either FluentValidation or Interlocutor.Abstractions isn't referenced - nothing to check.
                return;
            }

            var wellKnown = new WellKnownSymbols(
                abstractValidator,
                commandMarker,
                queryMarker,
                compilation.GetTypeByMetadataName(WellKnownTypeNames.ValidationFailureCase),
                compilation.GetTypeByMetadataName(WellKnownTypeNames.ValidationError),
                compilation.GetTypeByMetadataName(WellKnownTypeNames.ValidationErrorOfErrorCode),
                compilation.GetTypeByMetadataName(WellKnownTypeNames.ConcreteNonGenericOutcome)
            );

            compilationStartContext.RegisterSymbolAction(
                symbolContext => Analyze(symbolContext, wellKnown),
                SymbolKind.NamedType
            );
        });
    }

    private static void Analyze(SymbolAnalysisContext context, WellKnownSymbols wellKnown)
    {
        var validatorType = (INamedTypeSymbol)context.Symbol;
        var validatedType = FindValidatedType(validatorType, wellKnown.AbstractValidator);
        if (validatedType is null)
        {
            // Not a FluentValidation validator (or doesn't derive from AbstractValidator<T> directly/transitively).
            return;
        }

        var responseType = FindDeclaredResponseType(validatedType, wellKnown.CommandMarker, wellKnown.QueryMarker);
        if (responseType is null)
        {
            // The validated type isn't an ICommand<TResponse>/IQuery<TResponse> - out of scope for this rule.
            return;
        }

        if (!TryGetCaseTypes(responseType, wellKnown, out var caseTypes))
        {
            // The response's case set can't be determined statically (a fully custom, non-generic IOutcome
            // implementer) - skip rather than risk a false positive.
            return;
        }

        if (caseTypes.Any(caseType => IsValidationShaped(caseType, wellKnown)))
        {
            return;
        }

        var caseTypeNames =
            caseTypes.Length == 0 ? "none" : string.Join(", ", caseTypes.Select(static c => c.ToDisplayString()));

        context.ReportDiagnostic(
            Diagnostic.Create(
                InterlocutorDiagnostics.ValidationShapeMismatch,
                validatorType.Locations.FirstOrDefault() ?? Location.None,
                validatedType.ToDisplayString(),
                validatorType.ToDisplayString(),
                responseType.ToDisplayString(),
                caseTypeNames
            )
        );
    }

    /// <summary>
    /// Walks <paramref name="validatorType"/>'s base type chain looking for a closed
    /// <c>AbstractValidator&lt;T&gt;</c>, returning the validated type <c>T</c> if found.
    /// </summary>
    /// <param name="validatorType">The candidate validator type.</param>
    /// <param name="abstractValidator">The resolved <c>AbstractValidator&lt;T&gt;</c> open generic symbol.</param>
    /// <returns>The validated type, or <see langword="null"/> if <paramref name="validatorType"/> doesn't derive from it.</returns>
    private static ITypeSymbol? FindValidatedType(INamedTypeSymbol validatorType, INamedTypeSymbol abstractValidator)
    {
        for (var candidate = validatorType.BaseType; candidate is not null; candidate = candidate.BaseType)
        {
            if (
                candidate.IsGenericType
                && SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, abstractValidator)
            )
            {
                return candidate.TypeArguments[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the response type <paramref name="validatedType"/> declares via
    /// <c>ICommand&lt;TResponse&gt;</c>/<c>IQuery&lt;TResponse&gt;</c>, if it implements either.
    /// </summary>
    /// <param name="validatedType">The type a validator was found for.</param>
    /// <param name="commandMarker">The resolved <c>ICommand&lt;TResponse&gt;</c> open generic symbol, if referenced.</param>
    /// <param name="queryMarker">The resolved <c>IQuery&lt;TResponse&gt;</c> open generic symbol, if referenced.</param>
    /// <returns>The declared response type, or <see langword="null"/> if <paramref name="validatedType"/> is neither a command nor a query.</returns>
    private static ITypeSymbol? FindDeclaredResponseType(
        ITypeSymbol validatedType,
        INamedTypeSymbol? commandMarker,
        INamedTypeSymbol? queryMarker
    )
    {
        foreach (var candidateInterface in validatedType.AllInterfaces)
        {
            if (!candidateInterface.IsGenericType)
            {
                continue;
            }

            var openDefinition = candidateInterface.OriginalDefinition;
            if (
                SymbolEqualityComparer.Default.Equals(openDefinition, commandMarker)
                || SymbolEqualityComparer.Default.Equals(openDefinition, queryMarker)
            )
            {
                return candidateInterface.TypeArguments[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Determines the response type's case types - the type arguments of a closed generic <c>IOutcome</c>
    /// union, or the fixed <c>Success | Failure</c> pair for the shipped non-generic <c>Outcome</c>.
    /// </summary>
    /// <param name="responseType">The request's declared response type.</param>
    /// <param name="wellKnown">The resolved well-known symbols.</param>
    /// <param name="caseTypes">The resolved case types, empty for the non-generic <c>Outcome</c>.</param>
    /// <returns><see langword="true"/> if the case set could be determined; <see langword="false"/> for an unrecognized non-generic union.</returns>
    private static bool TryGetCaseTypes(
        ITypeSymbol responseType,
        WellKnownSymbols wellKnown,
        out ImmutableArray<ITypeSymbol> caseTypes
    )
    {
        if (responseType is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } namedResponse)
        {
            caseTypes = namedResponse.TypeArguments;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(responseType.OriginalDefinition, wellKnown.ConcreteNonGenericOutcome))
        {
            // Fixed Success | Failure shape - neither case is ever validation-shaped.
            caseTypes = ImmutableArray<ITypeSymbol>.Empty;
            return true;
        }

        caseTypes = ImmutableArray<ITypeSymbol>.Empty;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="caseType"/> is a validation-shaped failure case: it implements
    /// <c>IValidationFailureCase</c>, or it is the concrete <c>ValidationError</c>/<c>ValidationError&lt;TErrorCode&gt;</c>
    /// type, which does not implement that marker (see <c>documentation/messages-and-result-types.md</c>'s
    /// naming convention callout) but is still a validation failure by construction.
    /// </summary>
    /// <param name="caseType">A response type's case type.</param>
    /// <param name="wellKnown">The resolved well-known symbols.</param>
    /// <returns><see langword="true"/> if this case type is validation-shaped.</returns>
    private static bool IsValidationShaped(ITypeSymbol caseType, WellKnownSymbols wellKnown)
    {
        if (
            wellKnown.ValidationFailureCase is not null
            && caseType.AllInterfaces.Contains(wellKnown.ValidationFailureCase, SymbolEqualityComparer.Default)
        )
        {
            return true;
        }

        if (
            SymbolEqualityComparer.Default.Equals(caseType.OriginalDefinition, wellKnown.ValidationError)
            || SymbolEqualityComparer.Default.Equals(caseType.OriginalDefinition, wellKnown.ValidationErrorOfErrorCode)
        )
        {
            return true;
        }

        return false;
    }

    /// <summary>Every well-known symbol this analyzer resolves once per compilation.</summary>
    /// <param name="AbstractValidator">FluentValidation's <c>AbstractValidator&lt;T&gt;</c>.</param>
    /// <param name="CommandMarker"><c>ICommand&lt;TResponse&gt;</c>, if <c>Interlocutor.Abstractions</c> is referenced.</param>
    /// <param name="QueryMarker"><c>IQuery&lt;TResponse&gt;</c>, if <c>Interlocutor.Abstractions</c> is referenced.</param>
    /// <param name="ValidationFailureCase"><c>IValidationFailureCase</c>, if <c>Interlocutor.CommonKernel</c> is referenced.</param>
    /// <param name="ValidationError">The reason-only <c>ValidationError</c> type.</param>
    /// <param name="ValidationErrorOfErrorCode">The error-code-carrying <c>ValidationError&lt;TErrorCode&gt;</c> type.</param>
    /// <param name="ConcreteNonGenericOutcome">The shipped non-generic <c>Outcome</c> type.</param>
    private sealed record WellKnownSymbols(
        INamedTypeSymbol AbstractValidator,
        INamedTypeSymbol? CommandMarker,
        INamedTypeSymbol? QueryMarker,
        INamedTypeSymbol? ValidationFailureCase,
        INamedTypeSymbol? ValidationError,
        INamedTypeSymbol? ValidationErrorOfErrorCode,
        INamedTypeSymbol? ConcreteNonGenericOutcome
    );
}
