namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves ticket 06's behavior-subset-constraints outcome against Microsoft DI: a marker-constrained behavior
/// (<see cref="AuditingBehavior{TRequest,TResponse}"/>, <see cref="AuthorizationBehavior{TRequest,TResponse}"/>)
/// is filtered purely by Microsoft DI's own open-generic constraint handling when the container resolves
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/> - no custom filtering code in this library decides
/// whether a behavior applies to a request.
/// </summary>
public class BehaviorSubsetConstraintTests
{
    [Fact]
    public void AuditingBehavior_NeverResolves_ForRequestWithoutMarker()
    {
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>());
    }

    [Fact]
    public void AuditingBehavior_Resolves_ForRequestWithMarker()
    {
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();

        var resolved = provider
            .GetServices<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>()
            .ToList();
        Assert.Single(resolved);
        Assert.IsType<AuditingBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>(resolved[0]);
    }

    [Fact]
    public void AuthorizationBehavior_NeverResolves_ForRequestWithoutMarker()
    {
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuthorizationBehavior()
        );

        var provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>());
    }

    [Fact]
    public void AuthorizationBehavior_Resolves_ForRequestWithMarker()
    {
        // AuthorizationBehavior<TRequest,TResponse> maps its type parameters 1:1 onto
        // IPipelineBehavior<TRequest,TResponse> (see its own XML remarks for why that shape is load-bearing
        // for Microsoft DI specifically) - a real resolution through a built ServiceProvider, not merely a
        // registration-list check or a MakeGenericType probe.
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuthorizationBehavior()
        );

        var provider = services.BuildServiceProvider();

        var resolved = provider.GetServices<IPipelineBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>().ToList();
        Assert.Single(resolved);
        Assert.IsType<AuthorizationBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>(resolved[0]);
    }

    [Fact]
    public void RelativeOrder_AgainstUnconstrainedLoggingBehavior_IsPreserved()
    {
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddLoggingBehavior().AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();

        // AuditableCommand satisfies AuditingBehavior's constraint: both behaviors resolve, in registration order.
        var auditableChain = provider
            .GetServices<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuditingBehavior`2"], auditableChain);

        // PlainCommand does not satisfy AuditingBehavior's constraint: only the unconstrained LoggingBehavior
        // resolves, at the same relative position - filtering one entry out of the list doesn't reorder the rest.
        var plainChain = provider
            .GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2"], plainChain);
    }

    [Fact]
    public void ThreeRequestTypes_DrawDifferentSubsetsFromSameRegistrationList()
    {
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly)
                .AddLoggingBehavior()
                .AddAuditingBehavior()
                .AddAuthorizationBehavior()
        );

        var provider = services.BuildServiceProvider();

        // AuditableCommand: satisfies IAuditableRequest but not IRequiresPolicy -> logging + auditing only.
        var auditableChain = provider
            .GetServices<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuditingBehavior`2"], auditableChain);

        // PolicyCommand: satisfies IRequiresPolicy (and the Forbidden response shape) but not IAuditableRequest
        // -> logging + authorization only. Drawn from the exact same registration list as the other two chains.
        var policyChain = provider
            .GetServices<IPipelineBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuthorizationBehavior`2"], policyChain);

        // PlainCommand: satisfies neither marker -> logging only.
        var plainChain = provider
            .GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2"], plainChain);
    }

    [Fact]
    public void ConstrainedBehaviorRegisteredTwice_ResolvesTwiceForMatchingRequest_NoneForNonMatching()
    {
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuditingBehavior().AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();

        Assert.Equal(
            2,
            provider.GetServices<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>().Count()
        );
        Assert.Empty(provider.GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>());
    }

    [Fact]
    public void ValidationBehavior_Resolves_ThroughARealContainer()
    {
        // ValidationBehavior<TRequest,TResponse> has the identical structural shape as
        // AuthorizationBehavior<TRequest,TResponse> and was fixed for the same reason at the same time (see
        // both types' XML remarks) - proven here the same way, via a real ServiceProvider resolution rather
        // than a registration-list check.
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddValidationBehavior()
        );

        var provider = services.BuildServiceProvider();

        var resolved = provider
            .GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, ValidationErrors>>>()
            .ToList();
        Assert.Single(resolved);
        Assert.IsType<Interlocutor.FluentValidation.ValidationBehavior<
            PlainCommand,
            OperationOutcome<string, ValidationErrors>
        >>(resolved[0]);
    }

    /// <summary>
    /// A fresh <see cref="ServiceCollection"/> with a null <see cref="ILogger{TCategoryName}"/> registered - the
    /// default audit sink <c>InterlocutorBuilder.AddAuditingBehavior</c> try-adds depends on it, and these
    /// tests care about which behaviors resolve, not about logging output.
    /// </summary>
    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private sealed record AuditableCommand(string Name) : ICommand<OperationOutcome<string, Forbidden>>, IAuditableRequest
    {
        public string ActionName => "Test.Audit";
    }

    private sealed record PolicyCommand(string Name) : ICommand<OperationOutcome<string, Forbidden>>, IRequiresPolicy
    {
        public string PolicyName => "Test.Policy";
    }

    private sealed record PlainCommand(string Name) : ICommand<OperationOutcome<string, Forbidden>>;
}
