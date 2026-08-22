namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using System.Reflection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
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
    public void ScanAssembly_DefaultsToScopedLifetime()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        Assert.Contains(
            builder.Handlers,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Lifetime == RegistrationLifetime.Scoped
        );
    }

    [Fact]
    public void ScanAssembly_WithExplicitLifetime_AppliesItToEveryHandlerFromThatAssembly()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly, RegistrationLifetime.Singleton);

        Assert.Contains(
            builder.Handlers,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Lifetime == RegistrationLifetime.Singleton
        );
    }

    [Fact]
    public void ScanAssemblies_WithExplicitLifetime_AppliesItToAllGivenAssemblies()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssemblies(RegistrationLifetime.Singleton, typeof(InterlocutorBuilderTests).Assembly);

        Assert.Contains(
            builder.Handlers,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Lifetime == RegistrationLifetime.Singleton
        );
    }

    [Fact]
    public void Handlers_ReflectsScannedRegistrations_WithScannedProvenance()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        var registration = Assert.Single(builder.Handlers, r => r.ImplementationType == typeof(SampleQueryHandler));
        Assert.Equal(RegistrationProvenance.Scanned, registration.Provenance);
    }

    [Fact]
    public void AddHandler_RegistersOneEntryPerMatchingContract_WithExplicitProvenance()
    {
        var builder = new InterlocutorBuilder();

        builder.AddHandler(typeof(SampleQueryHandler));

        var registration = Assert.Single(builder.Handlers, r => r.ImplementationType == typeof(SampleQueryHandler));
        Assert.Equal(typeof(IQueryHandler<SampleQuery, OperationOutcome>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
        Assert.Equal(RegistrationProvenance.Explicit, registration.Provenance);
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddHandler_WithLifetime_RegistersAtThatLifetime()
    {
        var builder = new InterlocutorBuilder();

        builder.AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton);

        var registration = Assert.Single(builder.Handlers, r => r.ImplementationType == typeof(SampleQueryHandler));
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
    }

    [Fact]
    public void AddHandler_TypeImplementingNoHandlerContract_RegistersNothing()
    {
        var builder = new InterlocutorBuilder();

        builder.AddHandler(typeof(NotAHandler));

        Assert.DoesNotContain(builder.Handlers, r => r.ImplementationType == typeof(NotAHandler));
    }

    [Fact]
    public void AddHandler_TakesPrecedenceOverADuplicateFoundByScanning()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton);

        var matches = builder.Handlers.Where(r => r.ImplementationType == typeof(SampleQueryHandler)).ToList();
        var registration = Assert.Single(matches);
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
        Assert.Equal(RegistrationProvenance.Explicit, registration.Provenance);
    }

    [Fact]
    public void AddHandler_CalledBeforeScanAssembly_StillTakesPrecedence()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton)
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        var matches = builder.Handlers.Where(r => r.ImplementationType == typeof(SampleQueryHandler)).ToList();
        var registration = Assert.Single(matches);
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
        Assert.Equal(RegistrationProvenance.Explicit, registration.Provenance);
    }

    [Fact]
    public void DescribeRegistrations_IncludesHandlersBehaviorsStagesAndDefaults()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).AddAuditingBehavior().AddStage(typeof(SampleStage));

        var report = builder.DescribeRegistrations();

        Assert.Contains(
            report.Registrations,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Kind == RegistrationKind.Handler
        );
        Assert.Contains(
            report.Registrations,
            r => r.ImplementationType == typeof(AuditingBehavior<,>) && r.Kind == RegistrationKind.Behavior
        );
        Assert.Contains(
            report.Registrations,
            r => r.ImplementationType == typeof(SampleStage) && r.Kind == RegistrationKind.Stage
        );
        Assert.Contains(report.Registrations, r => r.ServiceType == typeof(IAuditSink) && r.Kind == RegistrationKind.Default);
    }

    [Fact]
    public void DescribeRegistrations_HasEmptyProblems_WhenValidateOnBuildNotConfigured()
    {
        var builder = new InterlocutorBuilder();

        var report = builder.DescribeRegistrations();

        Assert.Empty(report.Problems);
    }

    [Fact]
    public void DescribeRegistrations_SurfacesSkippedOptionalPackageProbes()
    {
        var builder = new InterlocutorBuilder();

        builder.AddTransactionBehavior();

        var report = builder.DescribeRegistrations();

        Assert.Contains(report.Skipped, s => s.Description.Contains("EntityFrameworkCore"));
    }

    [Fact]
    public void DescribeRegistrations_Format_ProducesNonEmptyReadableText()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).AddAuditingBehavior();

        var text = builder.DescribeRegistrations().Format();

        Assert.Contains("Registrations", text);
        Assert.Contains(nameof(SampleQueryHandler), text);
    }

    [Fact]
    public void AddTypeMatch_WithHandlerTarget_RegistersEveryMatchingTypeAsAHandler()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler);

        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal(typeof(IQueryHandler<SampleQuery, OperationOutcome>), registration.ServiceType);
        Assert.Equal(typeof(SampleQueryHandler), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
        Assert.Equal(RegistrationProvenance.Matched, registration.Provenance);
    }

    [Fact]
    public void AddTypeMatch_WithBehaviorTarget_RegistersAgainstOpenPipelineBehaviorContract()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(FirstBehavior<,>), TypeMatchTarget.Behavior, RegistrationLifetime.Singleton);

        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal(typeof(IPipelineBehavior<,>), registration.ServiceType);
        Assert.Equal(typeof(FirstBehavior<,>), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Behavior, registration.Kind);
        Assert.Equal(RegistrationProvenance.Matched, registration.Provenance);
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
    }

    [Fact]
    public void AddTypeMatch_MatchingZeroTypes_IsLegalAndSilent()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler);

        Assert.Empty(builder.MatchedRegistrations);
    }

    [Fact]
    public void AddTypeMatch_WithExcept_CarvesOutSpecificMatches()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(
                t => t == typeof(SampleQueryHandler) || t == typeof(NotAHandler),
                TypeMatchTarget.Handler,
                except: t => t == typeof(NotAHandler)
            );

        Assert.Single(builder.MatchedRegistrations);
        Assert.DoesNotContain(builder.MatchedRegistrations, r => r.ImplementationType == typeof(NotAHandler));
    }

    [Fact]
    public void AddTypeMatch_TwoRulesMatchingSameTypeAsSameServiceAndLifetime_CoexistAsDuplicates()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler);

        Assert.Equal(2, builder.MatchedRegistrations.Count);
    }

    [Fact]
    public void AddTypeMatch_DoesNotAffectMarkerInterfaceScopedBehaviors()
    {
        var builder = new InterlocutorBuilder();

        builder.AddTypeMatch(_ => false, TypeMatchTarget.Handler).AddAuthorizationBehavior();

        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(AuthorizationBehavior<,>));
    }

    [Fact]
    public void AddTypeMatch_NamedRule_IdentifiesItselfOnEveryRegistrationItProduces()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, name: "sample-handlers");

        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal("sample-handlers", registration.MatchedByRule);
    }

    [Fact]
    public void AddTypeMatch_UnnamedRule_IdentifiesItselfByPosition_DistinguishingMultipleRules()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(NotAHandler), TypeMatchTarget.Handler) // matches nothing, occupies position 0
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler);

        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal("rule 1", registration.MatchedByRule);
    }

    [Fact]
    public void AddHandler_HasNoMatchedByRule_SinceItsProvenanceIsExplicitNotMatched()
    {
        var builder = new InterlocutorBuilder();

        builder.AddHandler(typeof(SampleQueryHandler));

        var registration = Assert.Single(builder.Handlers);
        Assert.Null(registration.MatchedByRule);
    }

    [Fact]
    public void AddTypeMatch_RecordsRequireFlag_WithoutEnforcingIt()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true);

        var rule = Assert.Single(builder.MatchRules);
        Assert.True(rule.Require);
        Assert.Empty(builder.MatchedRegistrations);
    }

    [Fact]
    public void ValidateOnBuild_NotConfigured_LeavesProblemsEmpty_EvenWithAnUnhandledRequestType()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        Assert.Empty(builder.DescribeRegistrations().Problems);
    }

    [Fact]
    public void ValidateOnBuild_DetectsAnUnhandledRequestType()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).ValidateOnBuild();

        Assert.Contains(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnhandledRequestType && p.Description.Contains(nameof(UnhandledQuery))
        );
    }

    [Fact]
    public void ValidateOnBuild_WithAHandlerRegistered_HasNoUnhandledRequestTypeProblem()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddHandler(typeof(HandledByValidateOnBuildTestQueryHandler))
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .ValidateOnBuild();

        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Description.Contains(nameof(HandledByValidateOnBuildTestQuery))
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsAnUnmatchedRequiredTypeMatchRule()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        Assert.Contains(builder.DescribeRegistrations().Problems, p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule);
    }

    [Fact]
    public void ValidateOnBuild_SatisfiedRequiredTypeMatchRule_HasNoUnmatchedRuleProblem()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsConflictingLifetimesForTheSameServiceAndImplementationPair()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Scoped)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, RegistrationLifetime.Singleton)
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .ValidateOnBuild();

        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingLifetime
        );
        Assert.Contains(nameof(SampleQueryHandler), problem.Description);
    }

    [Fact]
    public void ValidateOnBuild_SameServiceAndImplementationAtTheSameLifetime_IsNotAConflict()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .ValidateOnBuild();

        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingLifetime
        );
    }

    [Fact]
    public void ValidateOnBuild_AggregatesEveryProblemIntoOneReport_NotJustTheFirst()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Scoped)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, RegistrationLifetime.Singleton)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        var problems = builder.DescribeRegistrations().Problems;
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.UnhandledRequestType);
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule);
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.ConflictingLifetime);
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
        Assert.Contains(
            builder.Defaults,
            r => r.ServiceType == typeof(IOutcomeCaseRegistry) && r.ImplementationType == typeof(DefaultOutcomeCaseRegistry)
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
    public void AddValidationBehavior_WithFluentValidationReferenced_AddsValidationBehaviorAndCaseRegistryDefault()
    {
        var builder = new InterlocutorBuilder();

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

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    private sealed record UnhandledQuery : IQuery<OperationOutcome>;

    // Uniquely named (unlike SampleQuery/SampleQueryHandler, which several sibling test classes in this
    // shared assembly also declare with the same simple name) so ValidateOnBuild_WithAHandlerRegistered_...'s
    // description-text assertion can't accidentally match another class's unrelated request type.
    private sealed record HandledByValidateOnBuildTestQuery : IQuery<OperationOutcome>;

    private sealed class HandledByValidateOnBuildTestQueryHandler
        : IQueryHandler<HandledByValidateOnBuildTestQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(HandledByValidateOnBuildTestQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class NotAHandler;
}
