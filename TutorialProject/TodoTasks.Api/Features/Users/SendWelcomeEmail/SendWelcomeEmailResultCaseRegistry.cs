namespace TodoTasks.Api.Features.Users.SendWelcomeEmail;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Teaches <see cref="IOutcomeCaseRegistry"/> how to construct <see cref="SendWelcomeEmailResult"/>'s
/// cases from a bare case value - the piece a hand-rolled, non-compiler-generated <see cref="IUnion"/>
/// implementer needs that a compiler-generated <c>Outcome&lt;...&gt;</c> union doesn't, per
/// <c>documentation/extensibility.md</c>'s "Custom IUnion implementations" section. Registered as the
/// pipeline's default <see cref="IOutcomeCaseRegistry"/> via
/// <c>InterlocutorPipelineBuilder.OverrideOutcomeCaseRegistry{TRegistry}</c> in <c>Program.cs</c>.
/// </summary>
/// <remarks>
/// Extracting <see cref="SendWelcomeEmailResult"/>'s active case needs no help - reading any
/// <see cref="IUnion"/>'s <c>Value</c> generically already works against the zero-config
/// <see cref="DefaultOutcomeCaseRegistry"/>, so both members below just check the two factories
/// registered here before delegating to it. Any other response type this project uses (every
/// compiler-generated <c>Outcome&lt;...&gt;</c> arity) is untouched by that fallback, so overriding the
/// pipeline-wide default with this type doesn't disturb their construction/extraction.
/// </remarks>
public sealed class SendWelcomeEmailResultCaseRegistry : IOutcomeCaseRegistry
{
    private static readonly IOutcomeCaseRegistry Inner = new OutcomeCaseRegistryBuilder()
        .RegisterFactory<SendWelcomeEmailResult, NotFound>(SendWelcomeEmailResult.FromFailure)
        .RegisterFactory<SendWelcomeEmailResult, Failure>(SendWelcomeEmailResult.FromFailure)
        .Build();

    /// <inheritdoc/>
    public TResponse Construct<TResponse, TCase>(TCase caseValue)
        where TResponse : IUnion => Inner.Construct<TResponse, TCase>(caseValue);

    /// <inheritdoc/>
    public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
        where TResponse : IUnion => Inner.TryGetActiveCase(response, out caseType, out caseValue);
}
