namespace Interlocutor.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Provides startup validation that all handler types are properly registered in the DI container.</summary>
/// <remarks>
/// <para>
/// Call <see cref="ValidateHandlerRegistrations"/> in your Program.cs (after DI registration) to catch missing
/// handler registrations at startup instead of at runtime. This is especially useful if you use manual handler
/// registration instead of assembly scanning - it validates that every query/command type has a corresponding handler.
/// </para>
/// </remarks>
public static class InterlocutorStartupValidation
{
    /// <summary>The resolved open generic <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}"/> method, closed reflectively per candidate pair by <see cref="SupportsCase"/>.</summary>
    private static readonly MethodInfo ConstructMethod = typeof(IOutcomeCaseRegistry).GetMethod(
        nameof(IOutcomeCaseRegistry.Construct)
    )!;

    /// <summary>
    /// The fixed set of four marker-interface-gated behaviors <see cref="ValidatePipelineCaseFidelity"/>
    /// checks, mirroring <c>Interlocutor.Generator.Analysis.PipelineBehaviorCaseFidelityAnalyzer</c>'s own
    /// compile-time equivalent (ADR-0023) - reflectively, against an already-built <see cref="IServiceProvider"/>,
    /// rather than symbolically against a compilation.
    /// </summary>
    private static readonly MarkerCaseRequirement[] MarkerCaseRequirements =
    [
        new(typeof(IRequiresPolicyRequest), typeof(Forbidden), "AuthorizationBehavior"),
        new(typeof(ICancellableRequest), typeof(Cancelled), "CancellationBehavior"),
        new(typeof(IRateLimitedRequest), typeof(RateLimited), "RateLimitedBehavior"),
        new(typeof(ITimeoutRequest), typeof(Timeout), "TimeoutBehavior"),
    ];

    /// <summary>Validates that all handler types referenced by query and command types in the specified assemblies are registered in the DI container.</summary>
    /// <param name="provider">The service provider to validate against.</param>
    /// <param name="assembliesToScan">The assemblies to scan for request types.</param>
    /// <returns>The same <paramref name="provider"/> instance, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when one or more handler types referenced by the scanned request types are not registered.</exception>
    [RequiresUnreferencedCode(
        "Scans the given assemblies with Assembly.GetTypes()/Type.GetInterfaces() - an explicitly opt-in "
            + "startup check, not part of the mediator's dispatch path. See documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "Closes IQueryHandler<,>/ICommandHandler<,> reflectively via Type.MakeGenericType for every scanned "
            + "request type. See documentation/trim-and-aot-support.md."
    )]
    public static IServiceProvider ValidateHandlerRegistrations(
        this IServiceProvider provider,
        params Assembly[] assembliesToScan
    )
    {
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var missing = new List<string>();

        // Scan for query and command types (those implementing IQuery<T> or ICommand<T>).
        var requestTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var requestType in requestTypes)
        {
            // For each query/command type, derive the expected handler interface and verify it's registered.
            // For example, if MyQuery implements IQuery<UserDto>, we look for a registered IQueryHandler<MyQuery, UserDto>.
            foreach (var iface in requestType.GetInterfaces().Where(i => i.IsGenericType))
            {
                var handlerType = iface.GetGenericTypeDefinition() switch
                {
                    var def when def == typeof(IQuery<>) => typeof(IQueryHandler<,>).MakeGenericType(
                        requestType,
                        iface.GetGenericArguments()[0]
                    ),
                    var def when def == typeof(ICommand<>) => typeof(ICommandHandler<,>).MakeGenericType(
                        requestType,
                        iface.GetGenericArguments()[0]
                    ),
                    _ => null,
                };

                // Try to resolve the handler from the service provider. If it's not registered, track the missing registration.
                if (handlerType is not null && sp.GetService(handlerType) is null)
                {
                    missing.Add($"{requestType.Name} implements {iface.Name} but no {handlerType.Name} is registered.");
                }
            }
        }

        // If any handlers are missing, throw with a detailed list so the developer can fix registrations immediately,
        // rather than discovering missing handlers at runtime when a request tries to execute.
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Interlocutor startup validation failed - missing handler registrations:\n"
                    + string.Join("\n", missing.Select(m => $"  - {m}"))
            );
        }

        return provider;
    }

    /// <summary>
    /// Validates that every scanned request type's response is shaped to satisfy the pipeline behaviors its
    /// marker interfaces (and, for <c>FluentValidation</c>, its actually-registered validators) opt it into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the runtime, DI-registration-time counterpart to the <c>Interlocutor.Generator</c> compile-time
    /// diagnostic <c>INTERLOC015</c> (ADR-0023, "Analyzer scope: the four marker-interface-gated behaviors
    /// only") - the primary safety net for a consumer on the reflection/<c>ScanAssembly</c> discovery path
    /// (e.g. the tutorial project), who has no access to that build-time signal. For each request type found
    /// while scanning <paramref name="assembliesToScan"/> that declares a response via
    /// <see cref="ICommand{TResponse}"/>/<see cref="IQuery{TResponse}"/>, this method checks:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// For each of the four marker-interface-gated behaviors - <see cref="IRequiresPolicyRequest"/> (needs
    /// <see cref="Forbidden"/>), <see cref="ICancellableRequest"/> (needs <see cref="Cancelled"/>),
    /// <see cref="IRateLimitedRequest"/> (needs <see cref="RateLimited"/>), and
    /// <see cref="ITimeoutRequest"/> (needs <see cref="Timeout"/>) - whether a request implementing that
    /// marker declares a response with the exact matching case, using the same <see cref="IOutcomeCaseRegistry"/>
    /// resolved from <paramref name="provider"/> (falling back to <see cref="DefaultOutcomeCaseRegistry"/> when
    /// nothing is registered) that the corresponding runtime behavior itself uses to short-circuit - never a
    /// re-implementation of that construction logic, so a consumer's custom registry/factory is honored here too.
    /// </description></item>
    /// <item><description>
    /// Additionally - the one check <c>INTERLOC015</c> cannot make, since it depends on a DI-registration fact
    /// invisible to static analysis - for every request type with at least one <c>FluentValidation.IValidator&lt;TRequest&gt;</c>
    /// actually registered in <paramref name="provider"/>, whether its response declares either a
    /// <see cref="ValidationErrors"/> or a <c>FluentValidationErrors</c> case (whichever of
    /// <c>ValidationBehavior</c>/<c>FluentValidationErrorsBehavior</c> a consumer has wired up). This probe is
    /// itself reflective: neither <c>FluentValidation.IValidator&lt;T&gt;</c> nor
    /// <c>Interlocutor.FluentValidationErrors</c> is resolved by a compile-time reference (this project must
    /// never depend on the <c>Interlocutor.FluentValidation</c> package - see <c>tests/Interlocutor.Architecture.Tests</c>),
    /// so both are looked up by name and silently skipped when the consuming application doesn't reference
    /// FluentValidation at all.
    /// </description></item>
    /// </list>
    /// <para>
    /// Opt-in, like <see cref="ValidateHandlerRegistrations"/>: call it explicitly from Program.cs after DI
    /// registration. Not part of the mediator's dispatch path.
    /// </para>
    /// </remarks>
    /// <param name="provider">The service provider to validate against.</param>
    /// <param name="assembliesToScan">The assemblies to scan for request types.</param>
    /// <returns>The same <paramref name="provider"/> instance, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more scanned request types pair a marker interface (or a registered
    /// <c>FluentValidation.IValidator&lt;TRequest&gt;</c>) with a response type that has no matching case.
    /// </exception>
    [RequiresUnreferencedCode(
        "Scans the given assemblies with Assembly.GetTypes()/Type.GetInterfaces(), and probes IOutcomeCaseRegistry "
            + "case construction via MakeGenericMethod - an explicitly opt-in startup check, not part of the "
            + "mediator's dispatch path. See documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "Closes IOutcomeCaseRegistry.Construct reflectively via MethodInfo.MakeGenericMethod for every "
            + "candidate (response type, case type) pair. See documentation/trim-and-aot-support.md."
    )]
    public static IServiceProvider ValidatePipelineCaseFidelity(
        this IServiceProvider provider,
        params Assembly[] assembliesToScan
    )
    {
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var caseRegistry = sp.GetService<IOutcomeCaseRegistry>() ?? DefaultOutcomeCaseRegistry.Instance;
        var validatorInterface = TryGetType("FluentValidation.IValidator`1, FluentValidation");
        var fluentValidationErrorsCase = TryGetType("Interlocutor.FluentValidationErrors, Interlocutor.FluentValidation");
        var mismatches = new List<string>();

        // Scan for query and command types, exactly as ValidateHandlerRegistrations does.
        var requestTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var requestType in requestTypes)
        {
            var responseBearingInterfaces = requestType
                .GetInterfaces()
                .Where(i =>
                    i.IsGenericType
                    && (i.GetGenericTypeDefinition() == typeof(IQuery<>) || i.GetGenericTypeDefinition() == typeof(ICommand<>))
                );

            foreach (var iface in responseBearingInterfaces)
            {
                // The response type is guaranteed to satisfy IUnion here - both IQuery<TResponse> and
                // ICommand<TResponse> constrain their own TResponse to it, so a closed instantiation could
                // not otherwise exist.
                var responseType = iface.GetGenericArguments()[0];

                var unmetRequirements = MarkerCaseRequirements.Where(requirement =>
                    requirement.MarkerType.IsAssignableFrom(requestType)
                    && !SupportsCase(caseRegistry, responseType, requirement.CaseType)
                );

                foreach (var requirement in unmetRequirements)
                {
                    mismatches.Add(
                        $"{requestType.Name} implements {requirement.MarkerType.Name} but its response "
                            + $"type {responseType.Name} has no {requirement.CaseType.Name} case for "
                            + $"{requirement.BehaviorName} to construct - add a {requirement.CaseType.Name} "
                            + $"case to the response type, or remove {requirement.MarkerType.Name}."
                    );
                }

                if (validatorInterface is not null && HasRegisteredValidator(sp, validatorInterface, requestType))
                {
                    var hasValidationErrorsCase = SupportsCase(caseRegistry, responseType, typeof(ValidationErrors));
                    var hasFluentValidationErrorsCase =
                        fluentValidationErrorsCase is not null
                        && SupportsCase(caseRegistry, responseType, fluentValidationErrorsCase);

                    if (!hasValidationErrorsCase && !hasFluentValidationErrorsCase)
                    {
                        mismatches.Add(
                            $"{requestType.Name} has at least one FluentValidation.IValidator<{requestType.Name}> "
                                + $"registered but its response type {responseType.Name} has neither a "
                                + $"{nameof(ValidationErrors)} nor a FluentValidationErrors case for "
                                + $"ValidationBehavior/FluentValidationErrorsBehavior to construct - add one of "
                                + "those cases to the response type."
                        );
                    }
                }
            }
        }

        // If any mismatches were found, throw with a detailed list so the developer can fix every response
        // shape immediately, rather than discovering the mismatch at runtime the first time a behavior
        // actually short-circuits for it.
        if (mismatches.Count > 0)
        {
            throw new InvalidOperationException(
                "Interlocutor startup validation failed - pipeline behavior case fidelity mismatches:\n"
                    + string.Join("\n", mismatches.Select(m => $"  - {m}"))
            );
        }

        return provider;
    }

    /// <summary>
    /// Tests whether <paramref name="requestType"/> has at least one <c>FluentValidation.IValidator&lt;TRequest&gt;</c>
    /// actually registered in <paramref name="sp"/> - a DI-registration fact, not something derivable from
    /// <paramref name="requestType"/>'s own declared shape.
    /// </summary>
    /// <param name="sp">The service provider to check for a registered validator.</param>
    /// <param name="validatorInterface">The open generic <c>FluentValidation.IValidator&lt;&gt;</c> type, resolved by name.</param>
    /// <param name="requestType">The concrete request type to close <paramref name="validatorInterface"/> over.</param>
    /// <returns><see langword="true"/> if at least one matching validator is registered; otherwise, <see langword="false"/>.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2055:MakeGenericType",
        Justification = "requestType is always a concrete, non-generic request type discovered by the same "
            + "Assembly.GetTypes() scan ValidateHandlerRegistrations already performs under its own "
            + "RequiresUnreferencedCode annotation - closing the resolved FluentValidation.IValidator<> over "
            + "it reflectively is this method's whole purpose. See documentation/trim-and-aot-support.md."
    )]
    private static bool HasRegisteredValidator(IServiceProvider sp, Type validatorInterface, Type requestType) =>
        sp.GetServices(validatorInterface.MakeGenericType(requestType)).Any();

    /// <summary>
    /// Tests whether <paramref name="caseRegistry"/> can construct <paramref name="responseType"/> from
    /// <paramref name="caseType"/>, by actually attempting <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}"/>
    /// reflectively and discarding the result - the same construction path
    /// <see cref="OutcomeCaseRegistryExtensions.ConstructOrThrow{TResponse, TCase}"/> uses at runtime, so a
    /// consumer's custom <see cref="IOutcomeCaseRegistry"/>/factory registered via
    /// <see cref="OutcomeCaseRegistryBuilder"/> is honored rather than assumed unsupported.
    /// </summary>
    /// <remarks>
    /// The case value passed to <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}"/> is an
    /// uninitialized instance of <paramref name="caseType"/> obtained via
    /// <see cref="RuntimeHelpers.GetUninitializedObject"/> - safe here because this method only asks whether
    /// construction succeeds, never uses the constructed response. Unlike <see cref="Activator.CreateInstance(Type)"/>,
    /// this works uniformly whether <paramref name="caseType"/> is a value type with a required-parameter
    /// primary constructor (e.g. <see cref="RateLimited"/>) or a reference type with no parameterless
    /// constructor at all, without needing to discover or satisfy either shape's constructor arguments.
    /// </remarks>
    /// <param name="caseRegistry">The registry to probe for support of the (<paramref name="responseType"/>, <paramref name="caseType"/>) pairing.</param>
    /// <param name="responseType">The <see cref="IUnion"/>-constrained response type construction is attempted for.</param>
    /// <param name="caseType">The case type construction is attempted from.</param>
    /// <returns><see langword="true"/> if <paramref name="caseRegistry"/> could construct the pairing; otherwise, <see langword="false"/>.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067:DynamicallyAccessedMembers",
        Justification = "GetUninitializedObject never actually invokes a constructor - it exists precisely to "
            + "bypass one - so the PublicConstructors/NonPublicConstructors requirement the trimmer wants "
            + "satisfied on caseType is moot here. caseType is always resolved either from this compilation's "
            + "own well-known case types (Forbidden/Cancelled/RateLimited/Timeout/ValidationErrors) or from a "
            + "request type's own declared ICommand<TResponse>/IQuery<TResponse> response, discovered by the "
            + "same Assembly.GetTypes() scan ValidateHandlerRegistrations already performs under its own "
            + "RequiresUnreferencedCode annotation. See documentation/trim-and-aot-support.md."
    )]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2060:MakeGenericMethod",
        Justification = "ConstructMethod is IOutcomeCaseRegistry.Construct<TResponse, TCase>'s own open generic "
            + "MethodInfo, resolved once above - this is exactly the same reflective construction path "
            + "OutcomeCaseRegistryExtensions.ConstructOrThrow's callers already close at compile time (via a "
            + "generic type argument) instead of via MakeGenericMethod; responseType/caseType are resolved the "
            + "same way IL2067's suppression above documents. See documentation/trim-and-aot-support.md."
    )]
    private static bool SupportsCase(IOutcomeCaseRegistry caseRegistry, Type responseType, Type caseType)
    {
        var caseValue = RuntimeHelpers.GetUninitializedObject(caseType);
        try
        {
            ConstructMethod.MakeGenericMethod(responseType, caseType).Invoke(caseRegistry, [caseValue]);
            return true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is OutcomeCaseNotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to resolve a type by its assembly-qualified name, returning <see langword="null"/> if the
    /// type (or the assembly it lives in) is not loaded - the same graceful-fallback probe
    /// <c>Interlocutor.DependencyInjection.Abstractions.InterlocutorPipelineBuilder</c> uses to detect an
    /// optional peer package by name without taking a compile-time reference to it.
    /// </summary>
    /// <param name="typeName">The assembly-qualified type name to resolve.</param>
    /// <returns>The resolved <see cref="Type"/>, or <see langword="null"/> if it (or its assembly) is not loaded.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2057:UnrecognizedReflectionPattern",
        Justification = "Best-effort probe for FluentValidation's IValidator<> / Interlocutor.FluentValidation's "
            + "FluentValidationErrors by name only, so Interlocutor.Abstractions never takes a compile-time "
            + "dependency on either package - returning null when trimming has removed an unreferenced optional "
            + "package is the same, correct 'that package is absent' outcome as it never having been referenced "
            + "at all. See documentation/trim-and-aot-support.md."
    )]
    private static Type? TryGetType(string typeName)
    {
        try
        {
            return Type.GetType(typeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// One marker-interface-gated behavior's requirement: the marker interface that opts a request in, the
    /// exact case type its behavior constructs, and the behavior's own name for the mismatch message.
    /// </summary>
    /// <param name="MarkerType">The marker interface that opts a request into the behavior.</param>
    /// <param name="CaseType">The exact case type the behavior constructs when it short-circuits.</param>
    /// <param name="BehaviorName">The behavior's own simple name, for the mismatch message.</param>
    private readonly record struct MarkerCaseRequirement(Type MarkerType, Type CaseType, string BehaviorName);
}
