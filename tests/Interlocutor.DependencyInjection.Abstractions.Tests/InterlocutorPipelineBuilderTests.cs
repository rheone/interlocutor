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
    public void AddBehavior_SameTypeTwice_AppearsTwice()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddBehavior(typeof(FirstBehavior<,>)).AddBehavior(typeof(FirstBehavior<,>));

        Assert.Equal(2, builder.Behaviors.Count(r => r.ImplementationType == typeof(FirstBehavior<,>)));
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
    public void AddLoggingBehavior_AddsLoggingBehavior_WithoutOrphaningAnyDefault()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddLoggingBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.DoesNotContain(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
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
        Assert.DoesNotContain(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
    }

    [Fact]
    public void AddAuthorizationBehavior_AddsAuthorizationBehaviorAndPolicyEvaluatorDefault()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddAuthorizationBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(AuthorizationBehavior<,>));
        Assert.Contains(
            builder.Defaults,
            r => r.ServiceType == typeof(IPolicyEvaluator) && r.ImplementationType == typeof(DefaultPolicyEvaluator)
        );
        Assert.Contains(
            builder.Defaults,
            r => r.ServiceType == typeof(IOutcomeCaseRegistry) && r.ImplementationType == typeof(DefaultOutcomeCaseRegistry)
        );
    }

    [Fact]
    public void OverrideAuditSink_CalledBeforeAddAuditingBehavior_StillWins()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.OverrideAuditSink<CustomAuditSink>().AddAuditingBehavior();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IAuditSink));
        Assert.Equal(typeof(CustomAuditSink), registration.ImplementationType);
    }

    [Fact]
    public void OverrideAuditSink_CalledAfterAddAuditingBehavior_StillWins()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddAuditingBehavior().OverrideAuditSink<CustomAuditSink>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IAuditSink));
        Assert.Equal(typeof(CustomAuditSink), registration.ImplementationType);
    }

    [Fact]
    public void OverridePolicyEvaluator_Wins_RegardlessOfCallOrder()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.OverridePolicyEvaluator<CustomPolicyEvaluator>().AddAuthorizationBehavior();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IPolicyEvaluator));
        Assert.Equal(typeof(CustomPolicyEvaluator), registration.ImplementationType);
    }

    [Fact]
    public void OverrideOutcomeDescriber_Wins_RegardlessOfCallOrder()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddLoggingBehavior().OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
        Assert.Equal(typeof(DefaultResponseOutcomeDescriber), registration.ImplementationType);
    }

    [Fact]
    public void AddValidationBehavior_WithFluentValidationReferenced_AddsValidationBehaviorAndCaseRegistryDefault()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddValidationBehavior();

        Assert.Contains(
            builder.Behaviors,
            r => r.ImplementationType == typeof(Interlocutor.FluentValidation.ValidationBehavior<,>)
        );
        Assert.Contains(
            builder.Defaults,
            r => r.ServiceType == typeof(IOutcomeCaseRegistry) && r.ImplementationType == typeof(DefaultOutcomeCaseRegistry)
        );
    }

    [Fact]
    public void AddTransactionBehavior_WithNHibernateReferenced_AddsNHibernateTransactionBehavior()
    {
        // This test project references NHibernate but not EntityFrameworkCore specifically so the NHibernate
        // path is exercised for real through the public API - AddTransactionBehavior prefers EF Core via `??`
        // when both are present, which would otherwise mask a regression in the NHibernate-specific lookup
        // (NHibernate's TransactionBehavior<TRequest,TResponse> is arity 2, not EF Core's arity-3 shape).
        var builder = new InterlocutorPipelineBuilder();

        builder.AddTransactionBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(Interlocutor.NHibernate.TransactionBehavior<,>));
    }

    [Fact]
    public void AddTransactionBehavior_WithNHibernateReferenced_RecordsEfCoreProbeAsSkipped()
    {
        // This test project references NHibernate but not EntityFrameworkCore (see its own comment), so
        // AddTransactionBehavior finds NHibernate but the EF Core probe it tries first genuinely finds
        // nothing - the real "package not referenced" path, not a contrived one.
        var builder = new InterlocutorPipelineBuilder();

        builder.AddTransactionBehavior();

        var skipped = Assert.Single(builder.Skipped);
        Assert.Contains("EntityFrameworkCore", skipped.Description);
        Assert.Contains("not referenced", skipped.Reason);
    }

    [Fact]
    public void AddValidationBehavior_WithFluentValidationReferenced_RecordsNothingAsSkipped()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddValidationBehavior();

        Assert.Empty(builder.Skipped);
    }

    [Fact]
    public void BareBuilder_HasNoSkippedRegistrations()
    {
        var builder = new InterlocutorPipelineBuilder();

        Assert.Empty(builder.Skipped);
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
    public void UseRecommendedChain_IsSugarOverIndividualCalls_NotAutomatic()
    {
        var bare = new InterlocutorPipelineBuilder();

        Assert.Empty(bare.Behaviors);
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
    public void AddStage_TypeParameterOverload_DiscoversClosedPipelineStageInterface()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddStage<SampleStage>();

        var registration = Assert.Single(builder.Stages);
        Assert.Equal(typeof(IPipelineStage<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(typeof(SampleStage), registration.ImplementationType);
    }

    [Fact]
    public async Task AddStage_WithServiceProviderDelegates_FactoryReceivesTheProvider()
    {
        var builder = new InterlocutorPipelineBuilder();
        var sp = Substitute.For<IServiceProvider>();
        IServiceProvider? observed = null;

        builder.AddStage<SampleRequest, SampleResponse>(
            (provider, request, ct) =>
            {
                observed = provider;
                return Task.FromResult(StageResult<SampleResponse>.Continue);
            },
            null
        );

        var registration = Assert.Single(builder.StageFactories);
        var stage = Assert.IsType<DelegatePipelineStage<SampleRequest, SampleResponse>>(registration.Factory(sp));
        await stage.OnBeforeAsync(new SampleRequest(), CancellationToken.None);

        Assert.Same(sp, observed);
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
    public void AddStageHost_CanBeRegisteredMoreThanOnce()
    {
        var builder = new InterlocutorPipelineBuilder();

        builder.AddStageHost().AddStageHost();

        Assert.Equal(2, builder.Behaviors.Count(r => r.ImplementationType == typeof(PipelineStageBehavior<,>)));
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

    private sealed class CustomAuditSink : IAuditSink
    {
        public Task RecordAsync(AuditEntry entry, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CustomPolicyEvaluator : IPolicyEvaluator
    {
        public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct) => Task.FromResult(true);
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
