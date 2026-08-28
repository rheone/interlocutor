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
        // Arrange & Act
        var builder = new InterlocutorBuilder();

        // Assert
        Assert.Empty(builder.Behaviors);
    }

    [Fact]
    public void BareBuilder_SeedsOnlyRequestContextAccessorDefault()
    {
        // Arrange & Act
        var builder = new InterlocutorBuilder();

        // Assert
        var registration = Assert.Single(builder.Defaults);
        Assert.Equal(typeof(IRequestContextAccessor), registration.ServiceType);
        Assert.Equal(typeof(AmbientRequestContextAccessor), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Default, registration.Kind);
    }

    [Fact]
    public void ScanAssembly_AddsAssemblyToScanList()
    {
        // Arrange
        var builder = new InterlocutorBuilder();
        var assembly = typeof(InterlocutorBuilderTests).Assembly;

        // Act
        builder.ScanAssembly(assembly);

        // Assert
        Assert.Contains(assembly, builder.AssembliesToScan);
    }

    [Fact]
    public void ScanAssemblies_AddsAllAssembliesInOrder()
    {
        // Arrange
        var builder = new InterlocutorBuilder();
        var first = typeof(InterlocutorBuilderTests).Assembly;
        var second = typeof(object).Assembly;

        // Act
        builder.ScanAssemblies(first, second);

        // Assert
        Assert.Equal([first, second], builder.AssembliesToScan);
    }

    [Fact]
    public void ScanAssemblyContaining_ScansTheMarkerTypesAssembly()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssemblyContaining<InterlocutorBuilderTests>();

        // Assert
        Assert.Contains(typeof(InterlocutorBuilderTests).Assembly, builder.AssembliesToScan);
    }

    [Fact]
    public void ScanEntryAssembly_ScansTheProcessEntryAssembly()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanEntryAssembly();

        // Assert
        Assert.Contains(Assembly.GetEntryAssembly(), builder.AssembliesToScan);
    }

    [Fact]
    public void ScanAssembly_DefaultsToScopedLifetime()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        // Assert
        Assert.Contains(
            builder.Handlers,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Lifetime == RegistrationLifetime.Scoped
        );
    }

    [Fact]
    public void ScanAssembly_WithExplicitLifetime_AppliesItToEveryHandlerFromThatAssembly()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly, RegistrationLifetime.Singleton);

        // Assert
        Assert.Contains(
            builder.Handlers,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Lifetime == RegistrationLifetime.Singleton
        );
    }

    [Fact]
    public void ScanAssemblies_WithExplicitLifetime_AppliesItToAllGivenAssemblies()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssemblies(RegistrationLifetime.Singleton, typeof(InterlocutorBuilderTests).Assembly);

        // Assert
        Assert.Contains(
            builder.Handlers,
            r => r.ImplementationType == typeof(SampleQueryHandler) && r.Lifetime == RegistrationLifetime.Singleton
        );
    }

    [Fact]
    public void Handlers_ReflectsScannedRegistrations_WithScannedProvenance()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        // Assert
        var registration = Assert.Single(builder.Handlers, r => r.ImplementationType == typeof(SampleQueryHandler));
        Assert.Equal(RegistrationProvenance.Scanned, registration.Provenance);
    }

    [Fact]
    public void AddHandler_RegistersOneEntryPerMatchingContract_WithExplicitProvenance()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.AddHandler(typeof(SampleQueryHandler));

        // Assert
        var registration = Assert.Single(builder.Handlers, r => r.ImplementationType == typeof(SampleQueryHandler));
        Assert.Equal(typeof(IQueryHandler<SampleQuery, OperationOutcome>), registration.ServiceType);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
        Assert.Equal(RegistrationProvenance.Explicit, registration.Provenance);
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddHandler_WithLifetime_RegistersAtThatLifetime()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton);

        // Assert
        var registration = Assert.Single(builder.Handlers, r => r.ImplementationType == typeof(SampleQueryHandler));
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
    }

    [Fact]
    public void AddHandler_TypeImplementingNoHandlerContract_RegistersNothing()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.AddHandler(typeof(NotAHandler));

        // Assert
        Assert.DoesNotContain(builder.Handlers, r => r.ImplementationType == typeof(NotAHandler));
    }

    [Fact]
    public void AddHandler_TakesPrecedenceOverADuplicateFoundByScanning()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton);

        // Assert
        var matches = builder.Handlers.Where(r => r.ImplementationType == typeof(SampleQueryHandler)).ToList();
        var registration = Assert.Single(matches);
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
        Assert.Equal(RegistrationProvenance.Explicit, registration.Provenance);
    }

    [Fact]
    public void AddHandler_CalledBeforeScanAssembly_StillTakesPrecedence()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton)
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        // Assert
        var matches = builder.Handlers.Where(r => r.ImplementationType == typeof(SampleQueryHandler)).ToList();
        var registration = Assert.Single(matches);
        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
        Assert.Equal(RegistrationProvenance.Explicit, registration.Provenance);
    }

    [Fact]
    public void DescribeRegistrations_IncludesHandlersBehaviorsStagesAndDefaults()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .Pipeline.AddAuditingBehavior()
            .AddStage(typeof(SampleStage));

        // Act
        var report = builder.DescribeRegistrations();

        // Assert
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
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        var report = builder.DescribeRegistrations();

        // Assert
        Assert.Empty(report.Problems);
    }

    [Fact]
    public void DescribeRegistrations_SurfacesSkippedOptionalPackageProbes()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        builder.Pipeline.AddUnitOfWorkBehavior();

        // Act
        var report = builder.DescribeRegistrations();

        // Assert
        Assert.Contains(report.Skipped, s => s.Description.Contains("EntityFrameworkCore"));
    }

    [Fact]
    public void DescribeRegistrations_Format_ProducesNonEmptyReadableText()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).Pipeline.AddAuditingBehavior();

        // Act
        var text = builder.DescribeRegistrations().Format();

        // Assert
        Assert.Contains("Registrations", text);
        Assert.Contains(nameof(SampleQueryHandler), text);
    }

    [Fact]
    public void AddTypeMatch_WithHandlerTarget_RegistersEveryMatchingTypeAsAHandler()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler);

        // Assert
        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal(typeof(IQueryHandler<SampleQuery, OperationOutcome>), registration.ServiceType);
        Assert.Equal(typeof(SampleQueryHandler), registration.ImplementationType);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
        Assert.Equal(RegistrationProvenance.Matched, registration.Provenance);
    }

    [Fact]
    public void AddTypeMatch_WithBehaviorTarget_RegistersAgainstOpenPipelineBehaviorContract()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(FirstBehavior<,>), TypeMatchTarget.Behavior, RegistrationLifetime.Singleton);

        // Assert
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
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler);

        // Assert
        Assert.Empty(builder.MatchedRegistrations);
    }

    [Fact]
    public void AddTypeMatch_WithExcept_CarvesOutSpecificMatches()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(
                t => t == typeof(SampleQueryHandler) || t == typeof(NotAHandler),
                TypeMatchTarget.Handler,
                except: t => t == typeof(NotAHandler)
            );

        // Assert
        Assert.Single(builder.MatchedRegistrations);
        Assert.DoesNotContain(builder.MatchedRegistrations, r => r.ImplementationType == typeof(NotAHandler));
    }

    [Fact]
    public void AddTypeMatch_TwoRulesMatchingSameTypeAsSameServiceAndLifetime_CoexistAsDuplicates()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler);

        // Assert
        Assert.Equal(2, builder.MatchedRegistrations.Count);
    }

    [Fact]
    public void AddTypeMatch_DoesNotAffectMarkerInterfaceScopedBehaviors()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.AddTypeMatch(_ => false, TypeMatchTarget.Handler).Pipeline.AddAuthorizationBehavior();

        // Assert
        Assert.Contains(builder.Behaviors, r => r.ImplementationType == typeof(AuthorizationBehavior<,>));
    }

    [Fact]
    public void AddTypeMatch_NamedRule_IdentifiesItselfOnEveryRegistrationItProduces()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, name: "sample-handlers");

        // Assert
        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal("sample-handlers", registration.MatchedByRule);
    }

    [Fact]
    public void AddTypeMatch_UnnamedRule_IdentifiesItselfByPosition_DistinguishingMultipleRules()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t == typeof(NotAHandler), TypeMatchTarget.Handler) // matches nothing, occupies position 0
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler);

        // Assert
        var registration = Assert.Single(builder.MatchedRegistrations);
        Assert.Equal("rule 1", registration.MatchedByRule);
    }

    [Fact]
    public void AddHandler_HasNoMatchedByRule_SinceItsProvenanceIsExplicitNotMatched()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.AddHandler(typeof(SampleQueryHandler));

        // Assert
        var registration = Assert.Single(builder.Handlers);
        Assert.Null(registration.MatchedByRule);
    }

    [Fact]
    public void AddTypeMatch_RecordsRequireFlag_WithoutEnforcingIt()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(InterlocutorBuilderTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true);

        // Assert
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
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly).ValidateOnBuild();

        // Assert
        Assert.Contains(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnhandledRequestType && p.Description.Contains(nameof(UnhandledQuery))
        );
    }

    [Fact]
    public void DescribeRegistrations_WithEveryScannedTypeLoadingSuccessfully_HasEmptyTypeLoadFailures()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(InterlocutorBuilderTests).Assembly);

        // Assert
        Assert.Empty(builder.DescribeRegistrations().TypeLoadFailures);
    }

    [Fact]
    public void Pipeline_IsNeverNull()
    {
        // Arrange & Act
        var builder = new InterlocutorBuilder();

        // Assert
        Assert.NotNull(builder.Pipeline);
    }

    [Fact]
    public void Pipeline_ReturnsTheSameInstanceAcrossCalls()
    {
        // Arrange & Act
        var builder = new InterlocutorBuilder();

        // Assert
        Assert.Same(builder.Pipeline, builder.Pipeline);
    }

    [Fact]
    public void OverrideRequestContextAccessor_ReplacesSeededDefault()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.OverrideRequestContextAccessor<CustomRequestContextAccessor>();

        // Assert
        var registration = Assert.Single(builder.Defaults, r => r.ServiceType == typeof(IRequestContextAccessor));
        Assert.Equal(typeof(CustomRequestContextAccessor), registration.ImplementationType);
    }

    [Fact]
    public void OverrideRequestContextAccessor_AlsoVisibleThroughPipelineDefaults()
    {
        // Pipeline.Defaults and this type's own Defaults expose the same shared dictionary (see
        // InterlocutorBuilder's constructor remarks) - an infrastructure-only override made here must be
        // visible from the pipeline side too, not just this type's own projection.
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.OverrideRequestContextAccessor<CustomRequestContextAccessor>();

        // Assert
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
