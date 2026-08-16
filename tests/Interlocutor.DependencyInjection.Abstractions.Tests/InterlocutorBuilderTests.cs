namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using System.Reflection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using NSubstitute;

public class InterlocutorBuilderTests
{
    [Fact]
    public void BareBuilder_RegistersZeroBehaviors()
    {
        var builder = new InterlocutorBuilder();

        Assert.Empty(builder.Behaviors);
    }

    [Fact]
    public void BareBuilder_SeedsOnlyRequestContextAccessorDefault()
    {
        var builder = new InterlocutorBuilder();

        var registration = Assert.Single(builder.Defaults);
        Assert.Equal(typeof(IRequestContextAccessor), registration.ServiceType);
        Assert.Equal(typeof(AmbientRequestContextAccessor), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Default, registration.Kind);
    }

    [Fact]
    public void ScanAssembly_AddsAssemblyToScanList()
    {
        var builder = new InterlocutorBuilder();
        var assembly = typeof(InterlocutorBuilderTests).Assembly;

        builder.ScanAssembly(assembly);

        Assert.Contains(assembly, builder.AssembliesToScan);
    }

    [Fact]
    public void ScanAssemblies_AddsAllAssembliesInOrder()
    {
        var builder = new InterlocutorBuilder();
        var first = typeof(InterlocutorBuilderTests).Assembly;
        var second = typeof(object).Assembly;

        builder.ScanAssemblies(first, second);

        Assert.Equal([first, second], builder.AssembliesToScan);
    }

    [Fact]
    public void ScanAssemblyContaining_ScansTheMarkerTypesAssembly()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssemblyContaining<InterlocutorBuilderTests>();

        Assert.Contains(typeof(InterlocutorBuilderTests).Assembly, builder.AssembliesToScan);
    }

    [Fact]
    public void ScanEntryAssembly_ScansTheProcessEntryAssembly()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanEntryAssembly();

        Assert.Contains(Assembly.GetEntryAssembly(), builder.AssembliesToScan);
    }

    [Fact]
    public void AddBehavior_AppendsInCallOrder()
    {
        var builder = new InterlocutorBuilder();

        builder.AddBehavior(typeof(FirstBehavior<,>)).AddBehavior(typeof(SecondBehavior<,>));

        Assert.Equal(
            [typeof(FirstBehavior<,>), typeof(SecondBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void AddBehavior_SameTypeTwice_AppearsTwice()
    {
        var builder = new InterlocutorBuilder();

        builder.AddBehavior(typeof(FirstBehavior<,>)).AddBehavior(typeof(FirstBehavior<,>));

        Assert.Equal(2, builder.Behaviors.Count(r => r.ImplementationType == typeof(FirstBehavior<,>)));
    }

    [Fact]
    public void AddBehavior_RegistersAgainstOpenPipelineBehaviorContract()
    {
        var builder = new InterlocutorBuilder();

        builder.AddBehavior(typeof(FirstBehavior<,>));

        var registration = Assert.Single(builder.Behaviors);
        Assert.Equal(typeof(IPipelineBehavior<,>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Behavior, registration.Kind);
    }

    [Fact]
    public void AddLoggingBehavior_AddsLoggingBehavior_WithoutOrphaningAnyDefault()
    {
        var builder = new InterlocutorBuilder();

        builder.AddLoggingBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.DoesNotContain(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
    }

    [Fact]
    public void AddAuditingBehavior_AddsAuditingBehaviorAndAuditSinkDefault_ButNotDescriberDefault()
    {
        var builder = new InterlocutorBuilder();

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
        var builder = new InterlocutorBuilder();

        builder.AddAuthorizationBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(AuthorizationBehavior<,>));
        Assert.Contains(
            builder.Defaults,
            r => r.ServiceType == typeof(IPolicyEvaluator) && r.ImplementationType == typeof(DefaultPolicyEvaluator)
        );
    }

    [Fact]
    public void OverrideAuditSink_CalledBeforeAddAuditingBehavior_StillWins()
    {
        var builder = new InterlocutorBuilder();

        builder.OverrideAuditSink<CustomAuditSink>().AddAuditingBehavior();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IAuditSink));
        Assert.Equal(typeof(CustomAuditSink), registration.ImplementationType);
    }

    [Fact]
    public void OverrideAuditSink_CalledAfterAddAuditingBehavior_StillWins()
    {
        var builder = new InterlocutorBuilder();

        builder.AddAuditingBehavior().OverrideAuditSink<CustomAuditSink>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IAuditSink));
        Assert.Equal(typeof(CustomAuditSink), registration.ImplementationType);
    }

    [Fact]
    public void OverridePolicyEvaluator_Wins_RegardlessOfCallOrder()
    {
        var builder = new InterlocutorBuilder();

        builder.OverridePolicyEvaluator<CustomPolicyEvaluator>().AddAuthorizationBehavior();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IPolicyEvaluator));
        Assert.Equal(typeof(CustomPolicyEvaluator), registration.ImplementationType);
    }

    [Fact]
    public void OverrideRequestContextAccessor_ReplacesSeededDefault()
    {
        var builder = new InterlocutorBuilder();

        builder.OverrideRequestContextAccessor<CustomRequestContextAccessor>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IRequestContextAccessor));
        Assert.Equal(typeof(CustomRequestContextAccessor), registration.ImplementationType);
    }

    [Fact]
    public void OverrideOutcomeDescriber_Wins_RegardlessOfCallOrder()
    {
        var builder = new InterlocutorBuilder();

        builder.AddLoggingBehavior().OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>();

        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IResponseOutcomeDescriber));
        Assert.Equal(typeof(DefaultResponseOutcomeDescriber), registration.ImplementationType);
    }

    [Fact]
    public void AddValidationBehavior_WithFluentValidationReferenced_AddsValidationBehavior()
    {
        var builder = new InterlocutorBuilder();

        builder.AddValidationBehavior();

        Assert.Contains(
            builder.Behaviors,
            r => r.ImplementationType == typeof(Interlocutor.FluentValidation.ValidationBehavior<,>)
        );
    }

    [Fact]
    public void AddTransactionBehavior_WithNHibernateReferenced_AddsNHibernateTransactionBehavior()
    {
        // This test project references NHibernate but not EntityFrameworkCore specifically so the NHibernate
        // path is exercised for real through the public API - AddTransactionBehavior prefers EF Core via `??`
        // when both are present, which would otherwise mask a regression in the NHibernate-specific lookup
        // (NHibernate's TransactionBehavior<TRequest,TResponse> is arity 2, not EF Core's arity-3 shape).
        var builder = new InterlocutorBuilder();

        builder.AddTransactionBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(Interlocutor.NHibernate.TransactionBehavior<,>));
    }

    [Fact]
    public void UseRecommendedChain_AddsBehaviorsInCanonicalOrder()
    {
        var builder = new InterlocutorBuilder();

        builder.UseRecommendedChain();

        // The full canonical order, including validation and transaction - proven here rather than only
        // logging/auditing/authorization, since this test project references FluentValidation and NHibernate
        // and can therefore observe where those two land relative to the rest of the chain.
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
        var bare = new InterlocutorBuilder();

        Assert.Empty(bare.Behaviors);
    }

    [Fact]
    public void AddStage_DiscoversClosedPipelineStageInterface()
    {
        var builder = new InterlocutorBuilder();

        builder.AddStage(typeof(SampleStage));

        var registration = Assert.Single(builder.Stages);
        Assert.Equal(typeof(IPipelineStage<SampleRequest, SampleResponse>), registration.ServiceType);
        Assert.Equal(typeof(SampleStage), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Stage, registration.Kind);
    }

    [Fact]
    public void AddStage_WithDelegates_RegistersFactoryForClosedPipelineStageInterface()
    {
        var builder = new InterlocutorBuilder();

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
    public async Task AddStage_WithServiceProviderDelegates_FactoryReceivesTheProvider()
    {
        var builder = new InterlocutorBuilder();
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
        var builder = new InterlocutorBuilder();

        builder.AddLoggingBehavior().AddStageHost().AddAuditingBehavior();

        Assert.Equal(
            [typeof(LoggingBehavior<,>), typeof(PipelineStageBehavior<,>), typeof(AuditingBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void AddStageHost_CanBeRegisteredMoreThanOnce()
    {
        var builder = new InterlocutorBuilder();

        builder.AddStageHost().AddStageHost();

        Assert.Equal(2, builder.Behaviors.Count(r => r.ImplementationType == typeof(PipelineStageBehavior<,>)));
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

    private sealed class CustomRequestContextAccessor : IRequestContextAccessor
    {
        public RequestContext? Context { get; set; }
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
