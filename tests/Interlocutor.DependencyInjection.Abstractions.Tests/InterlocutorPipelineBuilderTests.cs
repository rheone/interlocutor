namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using NSubstitute;

/// <summary>
/// Exercises <see cref="InterlocutorPipelineBuilder"/> standalone - constructed directly, not through
/// <see cref="InterlocutorBuilder"/> - to prove the extracted pipeline-composition responsibility holds
/// up in isolation, independent of discovery/defaults concerns.
/// </summary>
public class InterlocutorPipelineBuilderTests
{
    [Fact]
    public void BareBuilder_RegistersZeroBehaviors()
    {
        var builder = new InterlocutorPipelineBuilder();

        Assert.Empty(builder.Behaviors);
    }

    [Fact]
    public void BareBuilder_HasNoDefaults()
    {
        var builder = new InterlocutorPipelineBuilder();

        Assert.Empty(builder.Defaults);
    }

    [Fact]
    public void AddBehavior_AppendsInCallOrder()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddBehavior(typeof(FirstBehavior<,>)).AddBehavior(typeof(SecondBehavior<,>));

        Assert.Equal(
            [typeof(FirstBehavior<,>), typeof(SecondBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void AddBehavior_RegistersAgainstOpenPipelineBehaviorContract()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddBehavior(typeof(FirstBehavior<,>));

        var registration = Assert.Single(builder.Behaviors);
        Assert.Equal(typeof(IPipelineBehavior<,>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Behavior, registration.Kind);
    }

    [Fact]
    public void AddAuditingBehavior_TryAddsAuditSinkDefault()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddAuditingBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(AuditingBehavior<,>));
        Assert.Contains(
            builder.Defaults,
            r => r.ServiceType == typeof(IAuditSink) && r.ImplementationType == typeof(LoggingAuditSink)
        );
    }

    [Fact]
    public void UseRecommendedChain_AddsBehaviorsInCanonicalOrder()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.UseRecommendedChain();

        Assert.Equal(
            [
                typeof(LoggingBehavior<,>),
                typeof(AuditingBehavior<,>),
                typeof(AuthorizationBehavior<,>),
                typeof(Interlocutor.FluentValidation.ValidationBehavior<,>),
                typeof(Interlocutor.NHibernate.TransactionBehavior<,>),
            ],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void OverrideTimeoutPolicy_CalledBeforeAddTimeoutBehavior_StillWins()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.OverrideTimeoutPolicy<CustomTimeoutPolicy>().AddTimeoutBehavior();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(ITimeoutPolicy));
        Assert.Equal(typeof(CustomTimeoutPolicy), registration.ImplementationType);
    }

    [Fact]
    public void OverrideTimeoutPolicy_CalledAfterAddTimeoutBehavior_StillWins()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddTimeoutBehavior().OverrideTimeoutPolicy<CustomTimeoutPolicy>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(ITimeoutPolicy));
        Assert.Equal(typeof(CustomTimeoutPolicy), registration.ImplementationType);
    }

    [Fact]
    public void OverrideRateLimiter_Wins_RegardlessOfCallOrder()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.OverrideRateLimiter<CustomRateLimiter>().AddRateLimitedBehavior();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IRateLimiter));
        Assert.Equal(typeof(CustomRateLimiter), registration.ImplementationType);
    }

    [Fact]
    public void OverrideOutcomeCaseRegistry_Wins_RegardlessOfCallOrder()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddCancellationBehavior().OverrideOutcomeCaseRegistry<CustomOutcomeCaseRegistry>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IOutcomeCaseRegistry));
        Assert.Equal(typeof(CustomOutcomeCaseRegistry), registration.ImplementationType);
    }

    [Fact]
    public void AddStage_DiscoversClosedPipelineStageInterface()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddStage(typeof(SampleStage));

        var registration = Assert.Single(builder.Stages);
        Assert.Equal(typeof(IPipelineStage<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(typeof(SampleStage), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Stage, registration.Kind);
    }

    [Fact]
    public void AddStage_WithDelegates_RegistersFactoryForClosedPipelineStageInterface()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddStage<SampleRequest, SampleResponse>(
            (request, ct) => Task.FromResult(StageResult<SampleResponse>.Continue),
            (request, response, ct) => Task.CompletedTask
        );

        var registration = Assert.Single(builder.StageFactories);
        Assert.Equal(typeof(IPipelineStage<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Stage, registration.Kind);
        Assert.IsType<DelegatePipelineStage<SampleRequest, SampleResponse>>(
            registration.Factory(Substitute.For<IServiceProvider>())
        );
    }

    [Fact]
    public void AddStageHost_AddsPipelineStageBehaviorAtCallPosition()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddLoggingBehavior().AddStageHost().AddAuditingBehavior();

        Assert.Equal(
            [typeof(LoggingBehavior<,>), typeof(PipelineStageBehavior<,>), typeof(AuditingBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void ConstructedStandalone_DoesNotShareDefaultsAcrossInstances()
    {
        var first = new InterlocutorPipelineBuilder();
        var second = new InterlocutorPipelineBuilder();

        first.AddAuditingBehavior();

        Assert.NotEmpty(first.Defaults);
        Assert.Empty(second.Defaults);
    }

    private sealed class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed class CustomTimeoutPolicy : ITimeoutPolicy
    {
        public TimeSpan ResolveBudget(IHasTimeout request) => TimeSpan.FromSeconds(1);
    }

    private sealed class CustomRateLimiter : IRateLimiter
    {
        public Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct) =>
            Task.FromResult(new RateLimitResult(true));
    }

    private sealed class CustomOutcomeCaseRegistry : IOutcomeCaseRegistry
    {
        public TResponse Construct<TResponse, TCase>(TCase caseValue)
            where TResponse : IOperationOutcome => throw new NotSupportedException();

        public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
            where TResponse : IOperationOutcome
        {
            caseType = typeof(TResponse);
            caseValue = null;
            return false;
        }
    }

    private sealed record SampleRequest;

    private sealed record SampleResponse;

    private sealed class SampleStage : IPipelineStage<SampleRequest, SampleResponse>
    {
        public Task<StageResult<SampleResponse>> OnBeforeAsync(SampleRequest request, CancellationToken ct) =>
            Task.FromResult(StageResult<SampleResponse>.Continue);

        public Task OnAfterAsync(SampleRequest request, SampleResponse result, CancellationToken ct) => Task.CompletedTask;
    }
}
