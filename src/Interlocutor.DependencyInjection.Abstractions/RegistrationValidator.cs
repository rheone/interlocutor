namespace Interlocutor.DependencyInjection.Abstractions;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Queries;

/// <summary>
/// Computes every <see cref="RegistrationProblem"/> <see cref="InterlocutorBuilder.ValidateOnBuild"/> opts into
/// checking, purely from the accumulated registration list and the other builder state it needs - no live
/// container involved.
/// </summary>
/// <remarks>
/// Factored out of <see cref="InterlocutorBuilder"/> so the checking logic itself is a self-contained,
/// independently testable unit, the same way <see cref="InterlocutorPipelineBuilder"/> factors pipeline
/// composition out of <see cref="InterlocutorBuilder"/> (ADR-0015). <see cref="InterlocutorBuilder.DescribeRegistrations"/>
/// forwards to <see cref="ComputeProblems"/> once <see cref="InterlocutorBuilder.ValidateOnBuild"/> has been
/// called; see that method's remarks for what each check covers and why.
/// </remarks>
internal static class RegistrationValidator
{
    /// <summary>
    /// Checks <paramref name="registrations"/>, <paramref name="candidates"/>, <paramref name="matchRules"/>,
    /// and <paramref name="overrideRules"/> for every problem <see cref="InterlocutorBuilder.ValidateOnBuild"/>
    /// documents: a query or command request type discovered while scanning with no matching handler, a
    /// required type-matching rule that matched nothing, two registrations of the same (service type,
    /// implementation type) pair at conflicting lifetimes, and two pipeline override rules matching the same
    /// request type.
    /// </summary>
    /// <param name="registrations">Every registration accumulated so far, as <see cref="InterlocutorBuilder.DescribeRegistrations"/> assembled it.</param>
    /// <param name="candidates">Every candidate type found while scanning, as <see cref="InterlocutorBuilder"/> assembled it.</param>
    /// <param name="matchRules">Every predicate-based type-matching rule added via <see cref="InterlocutorBuilder.AddTypeMatch"/>.</param>
    /// <param name="uniqueClosureContracts">
    /// Every open-generic contract expected to have at most one implementation per closed instantiation -
    /// the three built-in single-winner handler contracts plus any a consumer opted in via
    /// <see cref="InterlocutorBuilder.RequireUniqueClosure"/>.
    /// </param>
    /// <param name="overrideRules">
    /// Every Tier-1 pipeline override rule added via <see cref="InterlocutorPipelineBuilder.OverridePipelineFor"/>.
    /// </param>
    /// <returns>Every problem found.</returns>
    [RequiresUnreferencedCode(
        "Reflects over every candidate type's implemented interfaces with Type.GetInterfaces() and closes "
            + "IQueryHandler<,>/ICommandHandler<,> reflectively via Type.MakeGenericType for every scanned request "
            + "type - reached only via the opt-in InterlocutorBuilder.ValidateOnBuild. See documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "Closes IQueryHandler<,>/ICommandHandler<,> reflectively via Type.MakeGenericType for every scanned "
            + "request type. See documentation/trim-and-aot-support.md."
    )]
    public static List<RegistrationProblem> ComputeProblems(
        IReadOnlyList<InterlocutorRegistration> registrations,
        IReadOnlyList<Type> candidates,
        IReadOnlyList<TypeMatchRule> matchRules,
        IReadOnlyList<Type> uniqueClosureContracts,
        IReadOnlyList<PipelineOverrideRule> overrideRules
    )
    {
        var problems = new List<RegistrationProblem>();

        // A request type discovered while scanning with no matching handler - the same detection
        // InterlocutorStartupValidation.ValidateHandlerRegistrations uses, but checked against the
        // accumulated registration list instead of a live container.
        foreach (var requestType in candidates)
        {
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

                if (
                    handlerType is not null
                    && !registrations.Any(r => r.Kind == RegistrationKind.Handler && r.ServiceType == handlerType)
                )
                {
                    problems.Add(
                        new RegistrationProblem(
                            RegistrationProblemKind.UnhandledRequestType,
                            $"{requestType.Name} implements {iface.Name} but no {handlerType.Name} is registered."
                        )
                    );
                }
            }
        }

        // A required type-matching rule that matched nothing.
        foreach (var rule in matchRules.Where(r => r.Require))
        {
            var matchedAnything = candidates.Any(candidate =>
                rule.Predicate(candidate) && !(rule.Except?.Invoke(candidate) ?? false)
            );
            if (!matchedAnything)
            {
                problems.Add(
                    new RegistrationProblem(
                        RegistrationProblemKind.UnmatchedRequiredRule,
                        $"A required type-matching rule targeting {rule.Target} matched no candidate type."
                    )
                );
            }
        }

        // Two registrations of the same (service type, implementation type) pair at conflicting lifetimes.
        foreach (var group in registrations.GroupBy(r => (r.ServiceType, r.ImplementationType)))
        {
            var lifetimes = group.Select(r => r.Lifetime).Distinct().ToList();
            if (lifetimes.Count > 1)
            {
                problems.Add(
                    new RegistrationProblem(
                        RegistrationProblemKind.ConflictingLifetime,
                        $"{group.Key.ServiceType.Name} -> {group.Key.ImplementationType.Name} is registered at conflicting lifetimes: {string.Join(", ", lifetimes)}."
                    )
                );
            }
        }

        // More than one distinct implementation type closing the same instantiation of a single-winner
        // contract - the built-in query/command/bare-command handler contracts, plus any consumer contract
        // opted in via InterlocutorBuilder.RequireUniqueClosure. INotificationHandler<> is never in
        // uniqueClosureContracts, so fan-out to many notification handlers is never flagged here.
        foreach (
            var group in registrations
                .Where(r =>
                    r.Kind == RegistrationKind.Handler
                    && r.ServiceType.IsGenericType
                    && uniqueClosureContracts.Contains(r.ServiceType.GetGenericTypeDefinition())
                )
                .GroupBy(r => r.ServiceType)
        )
        {
            var distinctImplementations = group.DistinctBy(r => r.ImplementationType).ToList();
            if (distinctImplementations.Count > 1)
            {
                var conflicts = string.Join(
                    ", ",
                    distinctImplementations.Select(r =>
                        r.MatchedByRule is null
                            ? r.ImplementationType.Name
                            : $"{r.ImplementationType.Name} (matched by {r.MatchedByRule})"
                    )
                );
                problems.Add(
                    new RegistrationProblem(
                        RegistrationProblemKind.AmbiguousClosure,
                        $"{group.Key.Name} has {distinctImplementations.Count} conflicting implementations: {conflicts}."
                    )
                );
            }
        }

        // Two or more override rules whose predicates both match the same request type - checked only
        // against candidates that are actually a query, value-returning command, or bare command, the same
        // request-type scope OverridePipelineFor's rules are ever tested against at dispatch time.
        if (overrideRules.Count > 1)
        {
            foreach (var requestType in candidates.Where(IsRequestType))
            {
                var matchingRules = overrideRules.Where(rule => rule.Predicate(requestType)).ToList();
                if (matchingRules.Count > 1)
                {
                    problems.Add(
                        new RegistrationProblem(
                            RegistrationProblemKind.ConflictingPipelineOverride,
                            $"{requestType.Name} is matched by {matchingRules.Count} pipeline override rules: "
                                + $"{string.Join(", ", matchingRules.Select(rule => rule.Name))}."
                        )
                    );
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// Tests whether <paramref name="candidate"/> is a request type - a query, a value-returning command, or
    /// a bare command - the same scope a <see cref="PipelineOverrideRule"/> is ever matched against at
    /// dispatch time. Unlike the narrower <see cref="RegistrationProblemKind.UnhandledRequestType"/> check
    /// above, this includes the non-generic <see cref="ICommand"/> (bare commands): an override rule matches
    /// on the request's runtime type alone, independent of any declared response type, so a bare command is
    /// just as eligible to be matched - and to conflict - as a query or value-returning command.
    /// </summary>
    /// <param name="candidate">The candidate type found while scanning to test.</param>
    /// <returns><see langword="true"/> if <paramref name="candidate"/> is a query, value-returning command, or bare command.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "Only called from ComputeProblems, itself reached solely via the opt-in "
            + "InterlocutorBuilder.ValidateOnBuild (annotated RequiresUnreferencedCode) - candidate is always "
            + "one of that call's own scanned candidate types. See documentation/trim-and-aot-support.md."
    )]
    private static bool IsRequestType(Type candidate) =>
        typeof(ICommand).IsAssignableFrom(candidate)
        || candidate
            .GetInterfaces()
            .Any(iface =>
                iface.IsGenericType
                && (
                    iface.GetGenericTypeDefinition() == typeof(IQuery<>)
                    || iface.GetGenericTypeDefinition() == typeof(ICommand<>)
                )
            );
}
