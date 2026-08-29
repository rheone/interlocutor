namespace TodoTasks.Api.Infrastructure;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using TodoTasks.Api.Domain;

/// <summary>
/// Extends the library's default outcome classification with this project's own custom success DTO
/// types, so <c>LoggingBehavior</c>, <c>AuditingBehavior</c>, and <c>UnitOfWorkBehavior</c> correctly
/// recognize them as successes instead of falling through the classifier's fail-closed default.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <see cref="OutcomeClassifierBuilder.DefaultClassifier"/> - the classifier
/// every pipeline behavior falls back to when nothing else is registered - is pre-seeded only with
/// this library's own kernel case types (<c>Success</c>, <c>NotFound</c>, <c>Forbidden</c>,
/// <c>Conflict</c>, <c>ValidationErrors</c>, <c>Failure</c>, ...). Any other type used as an
/// <c>OperationOutcome&lt;TSuccess, ...&gt;</c>'s success case - <see cref="UserDto"/>, and every
/// other DTO this project's handlers return - is not one of those, so the default classifier's
/// fail-closed policy treats it as a failure.
/// </para>
/// <para>
/// This was verified the hard way, not assumed: without this registration, every write handler in
/// this project returns a genuinely successful outcome to its HTTP caller while
/// <c>UnitOfWorkBehavior</c> silently rolls back the transaction behind it, because it believes the
/// outcome was a failure. The bug is invisible from the handler's own return value - it only shows up
/// as data that mysteriously never persisted.
/// </para>
/// <para>
/// New feature slices need to add their own success DTO to <see cref="Classifier"/> below. See
/// <c>documentation/pipeline-and-behaviors.md</c> "Outcome Description &amp; Classification" for the
/// full mechanism this builds on, and <c>Program.cs</c> for where this is wired in.
/// </para>
/// <para>
/// <b>Two registrations, not one.</b> <c>UnitOfWorkBehavior</c> takes
/// <see cref="IOutcomeClassifier"/> and <see cref="IResponseOutcomeDescriber"/> as two <em>separate</em>
/// optional constructor parameters, each independently falling back to
/// <see cref="OutcomeClassifierBuilder.DefaultClassifier"/> if nothing is registered for it.
/// <c>interlocutor.Pipeline.OverrideOutcomeDescriber&lt;TodoTasksOutcomeDescriber&gt;()</c> alone only
/// replaces the <see cref="IResponseOutcomeDescriber"/> half - the actual commit/rollback decision
/// reads the <em>other</em> half, so <see cref="Classifier"/> must also be registered directly as
/// <see cref="IOutcomeClassifier"/> in <c>Program.cs</c>. Skipping that second registration was this
/// project's own first attempt at this fix - it made log lines describe outcomes correctly while the
/// commit/rollback decision kept using the unmodified, fail-closed default underneath, silently
/// rolling back every write regardless.
/// </para>
/// </remarks>
public sealed class TodoTasksOutcomeDescriber : IResponseOutcomeDescriber
{
    /// <summary>
    /// The classifier every custom success DTO this project's handlers return must be registered
    /// with, so <c>UnitOfWorkBehavior</c> commits instead of rolling back on a genuine success. Add a
    /// line here whenever a new feature introduces a new success DTO. Internal, not private:
    /// <c>Program.cs</c> registers this same instance directly as <see cref="IOutcomeClassifier"/>
    /// too - see this type's own remarks for why that second registration is required.
    /// </summary>
    internal static readonly IOutcomeClassifier Classifier = new OutcomeClassifierBuilder()
        .RegisterSuccessType<UserDto>("UserDto")
        .RegisterSuccessType<TodoTaskDto>("TodoTaskDto")
        .Build();

    /// <inheritdoc/>
    public string Describe(object? response) => Classifier.GetLabel(response);

    /// <inheritdoc/>
    public bool IsFailure(object? response) => Classifier.IsFailure(response);
}
