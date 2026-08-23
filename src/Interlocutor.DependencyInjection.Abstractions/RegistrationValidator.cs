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
    /// Checks <paramref name="registrations"/>, <paramref name="candidates"/>, and <paramref name="matchRules"/>
    /// for every problem <see cref="InterlocutorBuilder.ValidateOnBuild"/> documents: a query or command request
    /// type discovered while scanning with no matching handler, a required type-matching rule that matched
    /// nothing, and two registrations of the same (service type, implementation type) pair at conflicting
    /// lifetimes.
    /// </summary>
    /// <param name="registrations">Every registration accumulated so far, as <see cref="InterlocutorBuilder.DescribeRegistrations"/> assembled it.</param>
    /// <param name="candidates">Every candidate type found while scanning, as <see cref="InterlocutorBuilder"/> assembled it.</param>
    /// <param name="matchRules">Every predicate-based type-matching rule added via <see cref="InterlocutorBuilder.AddTypeMatch"/>.</param>
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
        IReadOnlyList<TypeMatchRule> matchRules
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

        return problems;
    }
}
