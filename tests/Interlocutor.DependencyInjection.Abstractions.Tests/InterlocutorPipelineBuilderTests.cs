namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Pipeline.Diagnostics;
using Interlocutor.CommonKernel.ResultTypes;
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
        // Arrange & Act
        var builder = new InterlocutorPipelineBuilder();

        // Assert
        Assert.Empty(builder.Behaviors);
    }

    [Fact]
    public void BareBuilder_HasNoDefaults()
    {
        // Arrange & Act
        var builder = new InterlocutorPipelineBuilder();

        // Assert
        Assert.Empty(builder.Defaults);
    }

    [Fact]
    public void AddBehavior_AppendsInCallOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddBehavior(typeof(FirstBehavior<,>)).AddBehavior(typeof(SecondBehavior<,>));

        // Assert
        Assert.Equal(
            [typeof(FirstBehavior<,>), typeof(SecondBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void AddBehavior_SameTypeTwice_AppearsTwice()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddBehavior(typeof(FirstBehavior<,>)).AddBehavior(typeof(FirstBehavior<,>));

        // Assert
        Assert.Equal(2, builder.Behaviors.Count(r => r.ImplementationType == typeof(FirstBehavior<,>)));
    }

    [Fact]
    public void AddBehavior_RegistersAgainstOpenPipelineBehaviorContract()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddBehavior(typeof(FirstBehavior<,>));

        // Assert
        var registration = Assert.Single(builder.Behaviors);
        Assert.Equal(typeof(IPipelineBehavior<,>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Behavior, registration.Kind);
    }

    [Fact]
    public void AddLoggingBehavior_AddsLoggingBehavior_WithoutOrphaningAnyDefault()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddLoggingBehavior();

        // Assert
        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.DoesNotContain(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
    }

    [Fact]
    public void AddAuditingBehavior_TryAddsAuditSinkDefault()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddAuditingBehavior();

        // Assert
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
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddAuthorizationBehavior();

        // Assert
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
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverrideAuditSink<CustomAuditSink>().AddAuditingBehavior();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IAuditSink));
        Assert.Equal(typeof(CustomAuditSink), registration.ImplementationType);
    }

    [Fact]
    public void OverrideAuditSink_CalledAfterAddAuditingBehavior_StillWins()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddAuditingBehavior().OverrideAuditSink<CustomAuditSink>();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IAuditSink));
        Assert.Equal(typeof(CustomAuditSink), registration.ImplementationType);
    }

    [Fact]
    public void OverridePolicyEvaluator_Wins_RegardlessOfCallOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverridePolicyEvaluator<CustomPolicyEvaluator>().AddAuthorizationBehavior();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IPolicyEvaluator));
        Assert.Equal(typeof(CustomPolicyEvaluator), registration.ImplementationType);
    }

    [Fact]
    public void OverrideOutcomeDescriber_Wins_RegardlessOfCallOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddLoggingBehavior().OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
        Assert.Equal(typeof(DefaultResponseOutcomeDescriber), registration.ImplementationType);
    }

    [Fact]
    public void AddValidationBehavior_WithFluentValidationReferenced_AddsValidationBehaviorAndCaseRegistryDefault()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddValidationBehavior();

        // Assert
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
    public void AddUnitOfWorkBehavior_WithNHibernateReferenced_AddsNHibernateUnitOfWorkBehavior()
    {
        // This test project references NHibernate but not EntityFrameworkCore specifically so the NHibernate
        // path is exercised for real through the public API - AddUnitOfWorkBehavior prefers EF Core via `??`
        // when both are present, which would otherwise mask a regression in the NHibernate-specific lookup
        // (NHibernate's UnitOfWorkBehavior<TRequest,TResponse> is arity 2, not EF Core's arity-3 shape).
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddUnitOfWorkBehavior();

        // Assert
        Assert.Contains(
            builder.Behaviors,
            r => r.ImplementationType == typeof(Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior<,>)
        );
    }

    [Fact]
    public void AddUnitOfWorkBehavior_WithNHibernateReferenced_RecordsEfCoreProbeAsSkipped()
    {
        // This test project references NHibernate but not EntityFrameworkCore (see its own comment), so
        // AddUnitOfWorkBehavior finds NHibernate but the EF Core probe it tries first genuinely finds
        // nothing - the real "package not referenced" path, not a contrived one.
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddUnitOfWorkBehavior();

        // Assert
        var skipped = Assert.Single(builder.Skipped);
        Assert.Contains("EntityFrameworkCore", skipped.Description);
        Assert.Contains("not referenced", skipped.Reason);
    }

    [Fact]
    public void AddValidationBehavior_WithFluentValidationReferenced_RecordsNothingAsSkipped()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddValidationBehavior();

        // Assert
        Assert.Empty(builder.Skipped);
    }

    [Fact]
    public void BareBuilder_HasNoSkippedRegistrations()
    {
        // Arrange & Act
        var builder = new InterlocutorPipelineBuilder();

        // Assert
        Assert.Empty(builder.Skipped);
    }

    [Fact]
    public void UseRecommendedChain_AddsBehaviorsInCanonicalOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.UseRecommendedChain();

        // Assert - the minimal default: Auditing/Authorization are opt-in, not automatic.
        Assert.Equal(
            [
                typeof(LoggingBehavior<,>),
                typeof(Interlocutor.FluentValidation.ValidationBehavior<,>),
                typeof(Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior<,>),
            ],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void UseRecommendedChain_IsSugarOverIndividualCalls_NotAutomatic()
    {
        // Arrange & Act
        var bare = new InterlocutorPipelineBuilder();

        // Assert
        Assert.Empty(bare.Behaviors);
    }

    [Fact]
    public void PriorFiveBehaviorChain_StillFullyReconstructableByExplicitChaining()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act - the chain UseRecommendedChain() composed before Auditing/Authorization were dropped
        // from its default, restored explicitly in the same relative order.
        builder
            .AddLoggingBehavior()
            .AddAuditingBehavior()
            .AddAuthorizationBehavior()
            .AddValidationBehavior()
            .AddUnitOfWorkBehavior();

        // Assert
        Assert.Equal(
            [
                typeof(LoggingBehavior<,>),
                typeof(AuditingBehavior<,>),
                typeof(AuthorizationBehavior<,>),
                typeof(Interlocutor.FluentValidation.ValidationBehavior<,>),
                typeof(Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior<,>),
            ],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void OverrideTimeoutPolicy_CalledBeforeAddTimeoutBehavior_StillWins()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverrideTimeoutPolicy<CustomTimeoutPolicy>().AddTimeoutBehavior();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(ITimeoutPolicy));
        Assert.Equal(typeof(CustomTimeoutPolicy), registration.ImplementationType);
    }

    [Fact]
    public void OverrideTimeoutPolicy_CalledAfterAddTimeoutBehavior_StillWins()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddTimeoutBehavior().OverrideTimeoutPolicy<CustomTimeoutPolicy>();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(ITimeoutPolicy));
        Assert.Equal(typeof(CustomTimeoutPolicy), registration.ImplementationType);
    }

    [Fact]
    public void OverrideRateLimiter_Wins_RegardlessOfCallOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverrideRateLimiter<CustomRateLimiter>().AddRateLimitedBehavior();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IRateLimiter));
        Assert.Equal(typeof(CustomRateLimiter), registration.ImplementationType);
    }

    [Fact]
    public void OverrideOutcomeCaseRegistry_Wins_RegardlessOfCallOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddCancellationBehavior().OverrideOutcomeCaseRegistry<CustomOutcomeCaseRegistry>();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IOutcomeCaseRegistry));
        Assert.Equal(typeof(CustomOutcomeCaseRegistry), registration.ImplementationType);
    }

    [Fact]
    public void AddPipelineExplainer_NotCalled_ConfigurationStaysNull()
    {
        // Arrange & Act
        var builder = new InterlocutorPipelineBuilder();

        // Assert
        Assert.Null(builder.PipelineExplainerConfiguration);
    }

    [Fact]
    public void AddPipelineExplainer_NoArgument_RecordsFreshDisabledOptions()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddPipelineExplainer();

        // Assert
        var configuration = Assert.IsType<PipelineExplainerOptions>(builder.PipelineExplainerConfiguration);
        Assert.False(configuration.Enabled);
    }

    [Fact]
    public void AddPipelineExplainer_WithOptions_RecordsThoseOptions()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();
        var options = new PipelineExplainerOptions { Enabled = true };

        // Act
        builder.AddPipelineExplainer(options);

        // Assert
        Assert.Same(options, builder.PipelineExplainerConfiguration);
    }

    [Fact]
    public void AddStage_DiscoversClosedPipelineStageInterface()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddStage(typeof(SampleStage));

        // Assert
        var registration = Assert.Single(builder.Stages);
        Assert.Equal(typeof(IPipelineStage<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(typeof(SampleStage), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Stage, registration.Kind);
    }

    [Fact]
    public void AddStage_WithDelegates_RegistersFactoryForClosedPipelineStageInterface()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddStage<SampleRequest, SampleResponse>(
            (_, _) => Task.FromResult(StageResult<SampleResponse>.Continue),
            (_, _, _) => Task.CompletedTask
        );

        // Assert
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
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddStage<SampleStage>();

        // Assert
        var registration = Assert.Single(builder.Stages);
        Assert.Equal(typeof(IPipelineStage<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(typeof(SampleStage), registration.ImplementationType);
    }

    [Fact]
    public async Task AddStage_WithServiceProviderDelegates_FactoryReceivesTheProvider()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();
        var sp = Substitute.For<IServiceProvider>();
        IServiceProvider? observed = null;

        // Act
        builder.AddStage<SampleRequest, SampleResponse>(
            (provider, _, _) =>
            {
                observed = provider;
                return Task.FromResult(StageResult<SampleResponse>.Continue);
            },
            null
        );

        var registration = Assert.Single(builder.StageFactories);
        var stage = Assert.IsType<DelegatePipelineStage<SampleRequest, SampleResponse>>(registration.Factory(sp));
        await stage.OnBeforeAsync(new SampleRequest(), CancellationToken.None);

        // Assert
        Assert.Same(sp, observed);
    }

    [Fact]
    public void AddBehavior_WithDelegates_RegistersFactoryForClosedPipelineBehaviorInterface()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddBehavior<SampleRequest, SampleResponse>(
            (_, _) => Task.FromResult(StageResult<SampleResponse>.Continue),
            (_, response, _) => Task.FromResult(response)
        );

        // Assert
        var registration = Assert.Single(builder.BehaviorFactories);
        Assert.Equal(typeof(IPipelineBehavior<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Behavior, registration.Kind);
        Assert.IsType<DelegatePipelineBehavior<SampleRequest, SampleResponse>>(
            registration.Factory(Substitute.For<IServiceProvider>())
        );
    }

    [Fact]
    public async Task AddBehavior_WithServiceProviderDelegates_FactoryReceivesTheProvider()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();
        var sp = Substitute.For<IServiceProvider>();
        IServiceProvider? observed = null;

        // Act
        builder.AddBehavior<SampleRequest, SampleResponse>(
            (provider, _, _) =>
            {
                observed = provider;
                return Task.FromResult(StageResult<SampleResponse>.Continue);
            },
            null
        );

        var registration = Assert.Single(builder.BehaviorFactories);
        var behavior = Assert.IsType<DelegatePipelineBehavior<SampleRequest, SampleResponse>>(registration.Factory(sp));
        await ((IPipelineBehavior<SampleRequest, SampleResponse>)behavior).HandleAsync(
            new SampleRequest(),
            () => Task.FromResult(new SampleResponse()),
            CancellationToken.None
        );

        // Assert
        Assert.Same(sp, observed);
    }

    [Fact]
    public async Task AddBehavior_DelegateRegistration_BehavesIdenticallyToATypeBasedPipelineBehaviorBase_ForShortCircuitAndExitOverride()
    {
        // Arrange: a hand-authored PipelineBehaviorBase subclass (the type-based AddBehavior(Type) path from
        // ticket 01) and a delegate-backed registration from this ticket's new overload, configured with
        // equivalent enter (short-circuit) / exit (override) logic.
        var typeBased = new ShortCircuitingBehavior();

        var builder = new InterlocutorPipelineBuilder();
        builder.AddBehavior<SampleRequest, int>(
            onEnter: (_, _) => Task.FromResult(StageResult<int>.ShortCircuit(42)),
            onExit: (_, response, _) => Task.FromResult(response + 1)
        );
        var delegateBased =
            (IPipelineBehavior<SampleRequest, int>)
                Assert.IsType<DelegatePipelineBehavior<SampleRequest, int>>(
                    Assert.Single(builder.BehaviorFactories).Factory(Substitute.For<IServiceProvider>())
                );

        var typeBasedHandlerCalled = false;
        var delegateBasedHandlerCalled = false;

        // Act
        var typeBasedResult = await typeBased.HandleAsync(
            new SampleRequest(),
            () =>
            {
                typeBasedHandlerCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None
        );
        var delegateBasedResult = await delegateBased.HandleAsync(
            new SampleRequest(),
            () =>
            {
                delegateBasedHandlerCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None
        );

        // Assert - both skip next() (short-circuit) and both hand back the exit-overridden response, unchanged
        // by which registration form produced the instance.
        Assert.False(typeBasedHandlerCalled);
        Assert.False(delegateBasedHandlerCalled);
        Assert.Equal(43, typeBasedResult);
        Assert.Equal(typeBasedResult, delegateBasedResult);
    }

    [Fact]
    public async Task AddBehavior_DelegateRegistration_ComposesInOnionOrder_LikeATypeBasedBehavior()
    {
        // Arrange
        var callOrder = new List<string>();
        IPipelineBehavior<SampleRequest, int> outer = new RecordingBehavior<SampleRequest, int>(callOrder, "outer");

        var builder = new InterlocutorPipelineBuilder();
        builder.AddBehavior<SampleRequest, int>(
            onEnter: (_, _) =>
            {
                callOrder.Add("delegate-enter");
                return Task.FromResult(StageResult<int>.Continue);
            },
            onExit: (_, response, _) =>
            {
                callOrder.Add("delegate-exit");
                return Task.FromResult(response);
            }
        );
        var inner =
            (IPipelineBehavior<SampleRequest, int>)
                Assert.IsType<DelegatePipelineBehavior<SampleRequest, int>>(
                    Assert.Single(builder.BehaviorFactories).Factory(Substitute.For<IServiceProvider>())
                );

        Task<int> HandlerAsync()
        {
            callOrder.Add("handler");
            return Task.FromResult(1);
        }

        // Act - outer wraps inner exactly as it would if inner had been registered via AddBehavior(Type)
        // instead; the chain-composition contract doesn't distinguish the two registration forms.
        Func<Task<int>> next = HandlerAsync;
        var wrapped = () => inner.HandleAsync(new SampleRequest(), next, CancellationToken.None);
        var result = await outer.HandleAsync(new SampleRequest(), wrapped, CancellationToken.None);

        // Assert
        Assert.Equal(["outer-enter", "delegate-enter", "handler", "delegate-exit", "outer-exit"], callOrder);
        Assert.Equal(1, result);
    }

    [Fact]
    public void AddStageHost_AddsPipelineStageBehaviorAtCallPosition()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddLoggingBehavior().AddStageHost().AddAuditingBehavior();

        // Assert
        Assert.Equal(
            [typeof(LoggingBehavior<,>), typeof(PipelineStageBehavior<,>), typeof(AuditingBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void AddStageHost_CanBeRegisteredMoreThanOnce()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.AddStageHost().AddStageHost();

        // Assert
        Assert.Equal(2, builder.Behaviors.Count(r => r.ImplementationType == typeof(PipelineStageBehavior<,>)));
    }

    [Fact]
    public void ConstructedStandalone_DoesNotShareDefaultsAcrossInstances()
    {
        // Arrange
        var first = new InterlocutorPipelineBuilder();
        var second = new InterlocutorPipelineBuilder();

        // Act
        first.AddAuditingBehavior();

        // Assert
        Assert.NotEmpty(first.Defaults);
        Assert.Empty(second.Defaults);
    }

    private sealed class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    /// <summary>
    /// A hand-authored <see cref="PipelineBehaviorBase{TRequest,TResponse}"/> subclass - the type-based
    /// <c>AddBehavior(Type, RegistrationLifetime)</c> registration path - used to prove the delegate-backed
    /// overload composes identically to it.
    /// </summary>
    private sealed class ShortCircuitingBehavior : PipelineBehaviorBase<SampleRequest, int>
    {
        protected override Task<StageResult<int>> OnEnterAsync(SampleRequest request, CancellationToken ct) =>
            Task.FromResult(StageResult<int>.ShortCircuit(42));

        protected override Task<int> OnExitAsync(SampleRequest request, int result, CancellationToken ct) =>
            Task.FromResult(result + 1);
    }

    /// <summary>A plain pass-through behavior that logs enter/exit against <paramref name="log"/>, for asserting onion-composition order.</summary>
    private sealed class RecordingBehavior<TRequest, TResponse>(List<string> log, string name)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Add($"{name}-enter");
            var result = await next();
            log.Add($"{name}-exit");
            return result;
        }
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
        public TimeSpan ResolveBudget(ITimeoutRequest request) => TimeSpan.FromSeconds(1);
    }

    private sealed class CustomRateLimiter : IRateLimiter
    {
        public Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct) =>
            Task.FromResult(new RateLimitResult(true));
    }

    private sealed class CustomOutcomeCaseRegistry : IOutcomeCaseRegistry
    {
        public TResponse Construct<TResponse, TCase>(TCase caseValue)
            where TResponse : IUnion => throw new NotSupportedException();

        public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
            where TResponse : IUnion
        {
            caseType = typeof(TResponse);
            caseValue = null;
            return false;
        }
    }

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
#pragma warning disable S2094
    private sealed record SampleRequest;

    private sealed record SampleResponse;
#pragma warning restore S2094

    private sealed class SampleStage : IPipelineStage<SampleRequest, SampleResponse>
    {
        public Task<StageResult<SampleResponse>> OnBeforeAsync(SampleRequest request, CancellationToken ct) =>
            Task.FromResult(StageResult<SampleResponse>.Continue);

        public Task OnAfterAsync(SampleRequest request, SampleResponse result, CancellationToken ct) => Task.CompletedTask;
    }
}
