namespace Interlocutor.DependencyInjection.Abstractions;

using System.Text;

/// <summary>
/// A structured, container-neutral snapshot of everything an <see cref="InterlocutorBuilder"/>
/// configuration accumulated - every registration, every optional-package probe that found nothing, and
/// every validation problem, in one shared model.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="InterlocutorBuilder.DescribeRegistrations"/>, buildable and inspectable at any
/// time regardless of whether <see cref="InterlocutorBuilder.ValidateOnBuild"/> is ever called -
/// <see cref="Problems"/> is simply empty until it is.
/// </para>
/// <para>
/// This is the single shared model both consumers build on: <see cref="InterlocutorBuilder.ValidateOnBuild"/>'s
/// aggregate exception carries this same object (with <see cref="Problems"/> populated) rather than a
/// second, parallel structure, and a consumer debugging "why isn't X registered" reads the same report
/// either way.
/// </para>
/// </remarks>
public sealed class RegistrationReport
{
    /// <summary>Initializes a new instance of the <see cref="RegistrationReport"/> class.</summary>
    /// <param name="registrations">Every accumulated <see cref="InterlocutorRegistration"/> entry.</param>
    /// <param name="stageFactories">Every accumulated delegate-backed stage registration.</param>
    /// <param name="skipped">Every registration deliberately not made, and why.</param>
    /// <param name="problems">Every validation problem found, empty until <see cref="InterlocutorBuilder.ValidateOnBuild"/> populates it.</param>
    /// <param name="typeLoadFailures">
    /// Every type that failed to load while scanning, and why. Unlike <paramref name="problems"/>, populated
    /// regardless of whether <see cref="InterlocutorBuilder.ValidateOnBuild"/> was called - a type load
    /// failure is discovered as a side effect of scanning itself, not an opt-in check. Defaults to empty.
    /// </param>
    public RegistrationReport(
        IReadOnlyList<InterlocutorRegistration> registrations,
        IReadOnlyList<InterlocutorFactoryRegistration> stageFactories,
        IReadOnlyList<SkippedRegistration> skipped,
        IReadOnlyList<RegistrationProblem> problems,
        IReadOnlyList<TypeLoadFailure>? typeLoadFailures = null
    )
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(stageFactories);
        ArgumentNullException.ThrowIfNull(skipped);
        ArgumentNullException.ThrowIfNull(problems);

        this.Registrations = registrations;
        this.StageFactories = stageFactories;
        this.Skipped = skipped;
        this.Problems = problems;
        this.TypeLoadFailures = typeLoadFailures ?? [];
    }

    /// <summary>
    /// Gets every accumulated <see cref="InterlocutorRegistration"/> entry - handlers (scanned, explicit,
    /// or matched), behaviors, stages, defaults, and generated registrations alike, each carrying its own
    /// <see cref="InterlocutorRegistration.Kind"/> and <see cref="InterlocutorRegistration.Provenance"/>.
    /// </summary>
    /// <value>Every accumulated <see cref="InterlocutorRegistration"/> entry.</value>
    public IReadOnlyList<InterlocutorRegistration> Registrations { get; }

    /// <summary>Gets every accumulated delegate-backed (lambda-form) stage registration.</summary>
    /// <value>Every accumulated delegate-backed stage registration.</value>
    public IReadOnlyList<InterlocutorFactoryRegistration> StageFactories { get; }

    /// <summary>Gets every registration the builder deliberately did not make, and why.</summary>
    /// <value>Every registration the builder deliberately did not make, and why.</value>
    public IReadOnlyList<SkippedRegistration> Skipped { get; }

    /// <summary>
    /// Gets every validation problem found by <see cref="InterlocutorBuilder.ValidateOnBuild"/>. Empty
    /// unless that method was configured and found something.
    /// </summary>
    /// <value>Every validation problem found.</value>
    public IReadOnlyList<RegistrationProblem> Problems { get; }

    /// <summary>
    /// Gets every type that failed to load while scanning an assembly, and why - non-fatal, since scanning
    /// continues past it. Empty when every scanned type loaded successfully, regardless of whether
    /// <see cref="InterlocutorBuilder.ValidateOnBuild"/> was ever called.
    /// </summary>
    /// <value>Every type that failed to load while scanning, and why.</value>
    public IReadOnlyList<TypeLoadFailure> TypeLoadFailures { get; }

    /// <summary>Renders this report as human-readable text, suitable for logging or console output.</summary>
    /// <returns>The formatted report.</returns>
    public string Format()
    {
        var text = new StringBuilder();
        text.AppendLine("Interlocutor registration report");

        text.AppendLine($"  Registrations ({this.Registrations.Count}):");
        foreach (var group in this.Registrations.GroupBy(r => r.Kind))
        {
            foreach (var registration in group)
            {
                var provenance = registration.MatchedByRule is null
                    ? $"{registration.Provenance}"
                    : $"{registration.Provenance} by {registration.MatchedByRule}";
                text.AppendLine(
                    $"    [{registration.Kind}] {Describe(registration.ServiceType)} -> {Describe(registration.ImplementationType)} "
                        + $"({registration.Lifetime}, {provenance})"
                );
            }
        }

        if (this.StageFactories.Count > 0)
        {
            text.AppendLine($"  Delegate-backed stages ({this.StageFactories.Count}):");
            foreach (var factory in this.StageFactories)
            {
                text.AppendLine($"    [{factory.Kind}] {Describe(factory.ServiceType)} ({factory.Lifetime})");
            }
        }

        if (this.Skipped.Count > 0)
        {
            text.AppendLine($"  Skipped ({this.Skipped.Count}):");
            foreach (var skipped in this.Skipped)
            {
                text.AppendLine($"    {skipped.Description} - {skipped.Reason}");
            }
        }

        if (this.Problems.Count > 0)
        {
            text.AppendLine($"  Problems ({this.Problems.Count}):");
            foreach (var problem in this.Problems)
            {
                text.AppendLine($"    [{problem.Kind}] {problem.Description}");
            }
        }

        if (this.TypeLoadFailures.Count > 0)
        {
            text.AppendLine($"  Type load failures ({this.TypeLoadFailures.Count}):");
            foreach (var failure in this.TypeLoadFailures)
            {
                text.AppendLine($"    {failure.AssemblyName}: {failure.Detail}");
            }
        }

        return text.ToString();
    }

    /// <summary>Renders this report as human-readable text. See <see cref="Format"/>.</summary>
    /// <returns>The formatted report.</returns>
    public override string ToString() => this.Format();

    /// <summary>
    /// Renders a type for display, using its short name plus the open-generic marker (<c>&lt;,&gt;</c>)
    /// rather than the fully-qualified, backtick-arity name reflection would otherwise produce.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <returns>The type's display name.</returns>
    private static string Describe(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        var arity = type.GetGenericArguments().Length;
        return $"{name}<{new string(',', arity - 1)}>";
    }
}
