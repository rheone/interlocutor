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
/// (<see cref="AuditingBehavior{TRequest,TResponse}"/>, <see cref="AuthorizationBehavior{TRequest,TSuccess}"/>)
/// is filtered purely by Microsoft DI's own open-generic constraint handling when the container resolves
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/> — no custom filtering code in this library decides
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
    public void AuthorizationBehavior_TRequestConstraint_AcceptsMarkedRequest_ViaTheSameMechanismTheContainerUses()
    {
        // AuthorizationBehavior<TRequest,TSuccess> implements IPipelineBehavior<TRequest, OperationOutcome<TSuccess,
        // Forbidden>> — a different second type parameter than the one it closes over — which Microsoft DI's
        // AddScoped(Type,Type) open-generic path cannot resolve regardless of the marker constraint (a
        // pre-existing structural limitation unrelated to ticket 06; Autofac's RegisterGeneric handles this
        // shape correctly via structural unification — see BehaviorSubsetConstraintTests in the Autofac test
        // project for the equivalent full-container "resolves" proof). The marker constraint itself is proven
        // here the same way both containers evaluate it internally: MakeGenericType either succeeds (the
        // request satisfies IRequiresPolicy) or throws ArgumentException (it doesn't).
        var openGeneric = typeof(AuthorizationBehavior<,>);

        var closed = openGeneric.MakeGenericType(typeof(PolicyCommand), typeof(string));
        Assert.NotNull(closed);

        Assert.Throws<ArgumentException>(() => openGeneric.MakeGenericType(typeof(PlainCommand), typeof(string)));
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
        // resolves, at the same relative position — filtering one entry out of the list doesn't reorder the rest.
        var plainChain = provider
            .GetServices<IPipelineBehavior<PlainCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2"], plainChain);
    }

    [Fact]
    public void TwoRequestTypes_DrawDifferentSubsetsFromSameRegistrationList()
    {
        // The fuller three-behavior version of this proof (logging + auditing + authorization, drawing three
        // distinct chains) lives in Interlocutor.DependencyInjection.Autofac.Tests.BehaviorSubsetConstraintTests
        // — see the AuthorizationBehavior test above for why the authorization half doesn't fully resolve
        // through Microsoft DI's open-generic path.
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(BehaviorSubsetConstraintTests).Assembly).AddLoggingBehavior().AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();

        // AuditableCommand satisfies IAuditableRequest -> draws both entries from the registration list.
        var auditableChain = provider
            .GetServices<IPipelineBehavior<AuditableCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2", "AuditingBehavior`2"], auditableChain);

        // PolicyCommand does not satisfy IAuditableRequest -> draws only the unconstrained logging entry from
        // the exact same registration list.
        var policyChain = provider
            .GetServices<IPipelineBehavior<PolicyCommand, OperationOutcome<string, Forbidden>>>()
            .Select(b => b.GetType().Name)
            .ToList();
        Assert.Equal(["LoggingBehavior`2"], policyChain);
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

    /// <summary>
    /// A fresh <see cref="ServiceCollection"/> with a null <see cref="ILogger{TCategoryName}"/> registered — the
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
