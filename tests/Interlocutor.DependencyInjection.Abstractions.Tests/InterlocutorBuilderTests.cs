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

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .Pipeline.AddAuditingBehavior()
            .AddStage(typeof(SampleStage));

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

        builder.Pipeline.AddTransactionBehavior();

        var report = builder.DescribeRegistrations();

        Assert.Contains(report.Skipped, s => s.Description.Contains("EntityFrameworkCore"));
    }

    [Fact]
    public void DescribeRegistrations_Format_ProducesNonEmptyReadableText()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).Pipeline.AddAuditingBehavior();

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

        builder.AddTypeMatch(_ => false, TypeMatchTarget.Handler).Pipeline.AddAuthorizationBehavior();

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
    public void ValidateOnBuild_ConfiguredWithAnUnhandledRequestType_ReachesTheValidator()
    {
        // Smoke test only - the full ValidateOnBuild problem-kind matrix (unhandled request types, unmatched
        // required type-match rules, conflicting lifetimes, and their aggregation into one report) lives in
        // RegistrationValidatorTests, split off the same way InterlocutorPipelineBuilderTests was split off
        // under ADR-0015.
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).ValidateOnBuild();

        Assert.Contains(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnhandledRequestType && p.Description.Contains(nameof(UnhandledQuery))
        );
    }

    [Fact]
    public void Pipeline_IsNeverNull()
    {
        var builder = new InterlocutorBuilder();

        Assert.NotNull(builder.Pipeline);
    }

    [Fact]
    public void Pipeline_ReturnsTheSameInstanceAcrossCalls()
    {
        var builder = new InterlocutorBuilder();

        Assert.Same(builder.Pipeline, builder.Pipeline);
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
    public void OverrideRequestContextAccessor_AlsoVisibleThroughPipelineDefaults()
    {
        // Pipeline.Defaults and this type's own Defaults expose the same shared dictionary (see
        // InterlocutorBuilder's constructor remarks) - an infrastructure-only override made here must be
        // visible from the pipeline side too, not just this type's own projection.
        var builder = new InterlocutorBuilder();

        builder.OverrideRequestContextAccessor<CustomRequestContextAccessor>();

        var registration = Assert.Single(builder.Pipeline.Defaults, r => r.ServiceType == typeof(IRequestContextAccessor));
        Assert.Equal(typeof(CustomRequestContextAccessor), registration.ImplementationType);
    }

    private sealed class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
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

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class NotAHandler;
}
