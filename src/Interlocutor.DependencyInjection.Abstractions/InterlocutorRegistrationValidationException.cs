namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Thrown by <c>AddInterlocutor()</c> when <see cref="InterlocutorBuilder.ValidateOnBuild"/> is configured
/// and <see cref="InterlocutorBuilder.DescribeRegistrations"/> finds one or more problems in the
/// accumulated registration list - every problem found is aggregated into this single throw, rather than
/// failing on the first.
/// </summary>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set.
// This exception's Report is load-bearing - the whole point of catching it is to read every problem found,
// not just the formatted message - so a parameterless or message-only constructor would produce a useless
// exception missing the one thing callers need. Suppressed here rather than adding constructors that would
// only confuse callers into using them.
#pragma warning disable RCS1194
public sealed class InterlocutorRegistrationValidationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="InterlocutorRegistrationValidationException"/> class.</summary>
    /// <param name="report">
    /// The registration report whose <see cref="RegistrationReport.Problems"/> triggered this exception -
    /// the same object <see cref="InterlocutorBuilder.DescribeRegistrations"/> returns, not a second, parallel structure.
    /// </param>
    public InterlocutorRegistrationValidationException(RegistrationReport report)
        : base(BuildMessage(report))
    {
        this.Report = report;
    }

    /// <summary>
    /// Gets the registration report whose <see cref="RegistrationReport.Problems"/> triggered this exception.
    /// </summary>
    /// <value>The registration report whose <see cref="RegistrationReport.Problems"/> triggered this exception.</value>
    public RegistrationReport Report { get; }

    private static string BuildMessage(RegistrationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return "Interlocutor registration validation failed:\n"
            + string.Join("\n", report.Problems.Select(problem => $"  - [{problem.Kind}] {problem.Description}"));
    }
}
#pragma warning restore RCS1194
