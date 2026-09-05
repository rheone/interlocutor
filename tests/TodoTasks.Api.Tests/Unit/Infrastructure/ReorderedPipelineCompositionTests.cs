namespace TodoTasks.Api.Tests.Unit.Infrastructure;

using global::FluentValidation;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.FluentValidation;
using NSubstitute;
using GuardedResponse = Interlocutor.CommonKernel.Outcome<
    string,
    Interlocutor.CommonKernel.ResultTypes.ValidationErrors,
    Interlocutor.CommonKernel.ResultTypes.Forbidden
>;

/// <summary>
/// Proves the mechanical consequence of swapping <see cref="AuthorizationBehavior{TRequest,TResponse}"/> and
/// <see cref="ValidationBehavior{TRequest,TResponse}"/>'s registration order, per
/// <c>documentation/beginners-guide-todo-api.md</c>'s "When the recommended order doesn't fit" section. Not
/// wired into this project's own <c>Program.cs</c> pipeline - <c>Program.cs</c> keeps the recommended
/// Authorization-before-Validation order for every real endpoint, since reordering there would silently
/// change every other feature's behavior. This is an isolated composition of the two real behaviors, in both
/// orders, against a request whose validator and policy would both reject it, so which one short-circuits
/// first is unambiguous.
/// </summary>
public sealed class ReorderedPipelineCompositionTests
{
    [Fact]
    public async Task RecommendedOrder_AuthorizationBeforeValidation_DeniesWithoutEverRunningTheValidator_Test()
    {
        // Arrange - both the policy and the validator would reject this request; only one gets the chance to.
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(false);
        var validator = new RejectEverythingValidator();
        var authorization = new AuthorizationBehavior<GuardedRequest, GuardedResponse>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var validation = new ValidationBehavior<GuardedRequest, GuardedResponse>(
            [validator],
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new GuardedRequest();
        var handlerInvoked = false;
        Task<GuardedResponse> HandlerAsync()
        {
            handlerInvoked = true;
            return Task.FromResult<GuardedResponse>("unreachable");
        }

        // Act - Authorization registered first -> outermost, so its ingress runs before Validation's ever does.
        var result = await authorization.HandleAsync(
            request,
            () => validation.HandleAsync(request, HandlerAsync, TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.IsType<Forbidden>(result.Value);
        Assert.False(handlerInvoked);
        Assert.False(validator.WasInvoked, "The recommended order never reaches Validation once Authorization denies.");
    }

    [Fact]
    public async Task ReorderedChain_ValidationBeforeAuthorization_RejectsWithoutEverRunningThePolicyCheck_Test()
    {
        // Arrange - the same request, the same rejecting policy and validator, only the nesting order differs.
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(false);
        var validator = new RejectEverythingValidator();
        var authorization = new AuthorizationBehavior<GuardedRequest, GuardedResponse>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var validation = new ValidationBehavior<GuardedRequest, GuardedResponse>(
            [validator],
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new GuardedRequest();
        var handlerInvoked = false;
        Task<GuardedResponse> HandlerAsync()
        {
            handlerInvoked = true;
            return Task.FromResult<GuardedResponse>("unreachable");
        }

        // Act - Validation registered first this time -> outermost, so its ingress runs before Authorization's.
        var result = await validation.HandleAsync(
            request,
            () => authorization.HandleAsync(request, HandlerAsync, TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken
        );

        // Assert - the mechanical consequence stated directly: a validation failure now short-circuits
        // before authorization's ingress ever runs, cheap or not. The trade-off this buys: the recommended
        // order (proven above) saves the validator's work for a request the caller wasn't allowed to make at
        // all, at the cost of always paying for authorization's check - typically the more expensive step
        // (e.g. a network call to a permissions service) - even on input that was never well-formed to begin
        // with. This reordering gives that saving up in exchange for the opposite: authorization's check
        // (here, `policyEvaluator.EvaluateAsync`) never runs for a request that fails validation, but every
        // well-formed request now pays for validation *and* authorization in sequence, where the recommended
        // order could have skipped validation's cost entirely for a request authorization was always going
        // to deny.
        Assert.IsType<ValidationErrors>(result.Value);
        Assert.False(handlerInvoked);
        await policyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A request guarded by both a policy and a validator, so which one runs first is observable.</summary>
    public sealed record GuardedRequest : IRequiresPolicy
    {
        /// <inheritdoc/>
        public string PolicyName => "AlwaysDenied";
    }

    /// <summary>A validator that rejects every request, tracking whether it ran at all.</summary>
    public sealed class RejectEverythingValidator : AbstractValidator<GuardedRequest>
    {
        public RejectEverythingValidator() => this.RuleFor(r => r).Must(_ => false).WithMessage("Always rejected.");

        public bool WasInvoked { get; private set; }

        public override Task<global::FluentValidation.Results.ValidationResult> ValidateAsync(
            ValidationContext<GuardedRequest> context,
            CancellationToken cancellation = default
        )
        {
            this.WasInvoked = true;
            return base.ValidateAsync(context, cancellation);
        }
    }
}
