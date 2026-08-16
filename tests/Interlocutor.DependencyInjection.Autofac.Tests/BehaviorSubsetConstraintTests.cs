namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves ticket 06's behavior-subset-constraints outcome against Autofac: a marker-constrained behavior
/// (<see cref="AuditingBehavior{TRequest,TResponse}"/>, <see cref="AuthorizationBehavior{TRequest,TResponse}"/>)
/// is filtered purely by Autofac's own open-generic constraint handling when the container resolves
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/> — no custom filtering code in this library decides
/// whether a behavior applies to a request. The Microsoft DI counterpart of this test class proves the
/// identical three-behavior (logging, auditing, authorization) subset split.
/// </summary>
public class BehaviorSubsetConstraintTests
{
    [Fact]
    public void AuditingBehavior_NeverResolves_ForRequestWithoutMarker()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuditingBehavior()
        );

        var container = containerBuilder.Build();

        Assert.Empty(container.Resolve<IEnumerable<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>>());
    }

    [Fact]
    public void AuditingBehavior_Resolves_ForRequestWithMarker()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuditingBehavior()
        );

        var container = containerBuilder.Build();

        var resolved = container
            .Resolve<IEnumerable<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>>()
            .ToList();
        Assert.Single(resolved);
        Assert.IsType<AuditingBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>(resolved[0]);
    }

    [Fact]
    public void AuthorizationBehavior_NeverResolves_ForRequestWithoutMarker()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuthorizationBehavior()
        );

        var container = containerBuilder.Build();

        Assert.Empty(container.Resolve<IEnumerable<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>>());
    }

    [Fact]
    public void AuthorizationBehavior_Resolves_ForRequestWithMarker()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuthorizationBehavior()
        );

        var container = containerBuilder.Build();

        var resolved = container
            .Resolve<IEnumerable<IPipelineBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>>()
            .ToList();
        Assert.Single(resolved);
        Assert.IsType<AuthorizationBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>(resolved[0]);
    }

    [Fact]
    public void RelativeOrder_AgainstUnconstrainedLoggingBehavior_IsPreserved()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddLoggingBehavior().AddAuditingBehavior()
        );

        var container = containerBuilder.Build();

        // AuditableCommand satisfies AuditingBehavior's constraint: both behaviors resolve, in registration order.
        var auditableChain = container
            .Resolve<IEnumerable<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuditingBehavior`2"], auditableChain);

        // PlainCommand does not satisfy AuditingBehavior's constraint: only the unconstrained LoggingBehavior
        // resolves, at the same relative position — filtering one entry out of the list doesn't reorder the rest.
        var plainChain = container
            .Resolve<IEnumerable<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2"], plainChain);
    }

    [Fact]
    public void ThreeRequestTypes_DrawDifferentSubsetsFromSameRegistrationList()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly)
                .AddLoggingBehavior()
                .AddAuditingBehavior()
                .AddAuthorizationBehavior()
        );

        var container = containerBuilder.Build();

        // AuditableCommand: satisfies IAuditableRequest but not IRequiresPolicy -> logging + auditing only.
        var auditableChain = container
            .Resolve<IEnumerable<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuditingBehavior`2"], auditableChain);

        // PolicyCommand: satisfies IRequiresPolicy (and the Forbidden response shape) but not IAuditableRequest
        // -> logging + authorization only. Drawn from the exact same registration list as the other two chains.
        var policyChain = container
            .Resolve<IEnumerable<IPipelineBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuthorizationBehavior`2"], policyChain);

        // PlainCommand: satisfies neither marker -> logging only.
        var plainChain = container
            .Resolve<IEnumerable<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2"], plainChain);
    }

    [Fact]
    public void ConstrainedBehaviorRegisteredTwice_ResolvesTwiceForMatchingRequest_NoneForNonMatching()
    {
        var containerBuilder = NewContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddAuditingBehavior().AddAuditingBehavior()
        );

        var container = containerBuilder.Build();

        Assert.Equal(
            2,
            container.Resolve<IEnumerable<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>>().Count()
        );
        Assert.Empty(container.Resolve<IEnumerable<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>>());
    }

    /// <summary>
    /// A fresh <see cref="ContainerBuilder"/> with a null <see cref="ILogger{TCategoryName}"/> registered — the
    /// default audit sink <c>InterlocutorBuilder.AddAuditingBehavior</c> try-adds depends on it, and these tests
    /// care about which behaviors resolve, not about logging output.
    /// </summary>
    private static ContainerBuilder NewContainerBuilder()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>));
        return containerBuilder;
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
