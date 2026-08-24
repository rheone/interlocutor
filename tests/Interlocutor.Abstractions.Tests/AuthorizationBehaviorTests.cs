namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

// AuthorizationBehavior<TRequest, TResponse> now constrains TRequest : IRequiresPolicy, so a request that
// doesn't require a policy cannot close this generic at all - there is no runtime "skip" branch left to unit
// test here. The "a request without the marker never resolves the behavior" guarantee is proven at the
// container level instead: see BehaviorSubsetConstraintTests in the DependencyInjection.Microsoft/Autofac
// test projects.
//
// TResponse is the whole outcome type directly - not decomposed into a success type parameter - so this
// behavior's type parameters map 1:1 onto IPipelineBehavior<TRequest,TResponse>, exactly like every other
// built-in behavior. This is load-bearing, not a style choice: Microsoft.Extensions.DependencyInjection's
// open-generic resolver does positional substitution (implementationType.MakeGenericType(serviceType's
// generic arguments)) and cannot resolve an implementation whose type parameter is nested inside the
// service's generic argument (the previous <TRequest,TSuccess> shape, implementing
// IPipelineBehavior<TRequest, OperationOutcome<TSuccess,Forbidden>>) - it throws ArgumentException for
// every request, unconditionally, the moment that shape is registered. Autofac's RegisterGeneric performs
// real structural unification and would have tolerated the old shape, which is why this went uncaught until
// a real Microsoft DI resolution was attempted (see BehaviorSubsetConstraintTests).
public class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_AllowsAccessWhenPolicyEvaluatesTrue()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedQuery, OperationOutcome<string, Forbidden>>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result.Value);
        await policyEvaluator.Received(1).EvaluateAsync("CanRead", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ReturnsForbidenWhenPolicyEvaluatesFalse()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, OperationOutcome<string, Forbidden>>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            throw new InvalidOperationException("Handler should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert - Should return Forbidden instead of calling next
        Assert.IsType<Forbidden>(result.Value);
        await policyEvaluator.Received(1).EvaluateAsync("CanRead", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_IncludesPolicyNameInForbiddenReason()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, OperationOutcome<string, Forbidden>>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Contains("CanRead", forbidden.Reason ?? string.Empty);
    }

    [Fact]
    public async Task Handle_DoesNotCallNextWhenAuthorizationFails()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, OperationOutcome<string, Forbidden>>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ProtectedQuery("test");
        var nextCalled = false;

        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert - next should not be called
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_PassesRequestToPolicyEvaluator()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedQuery, OperationOutcome<string, Forbidden>>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ProtectedQuery("test-value");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert - the exact request instance should be passed to the evaluator, not merely an equal copy.
        await policyEvaluator
            .Received(1)
            .EvaluateAsync("CanRead", Arg.Is<object>(r => ReferenceEquals(r, request)), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithResponseTypeThatHasNoForbiddenCase_ThrowsNamedExceptionOnDenial()
    {
        // TResponse is no longer decomposed at compile time into a success-type parameter with Forbidden
        // pinned as the failure case - it's an arbitrary IOperationOutcome the caller chose, constructed
        // reflectively from a Forbidden case constructor when authorization is denied. A response shape with
        // no such case (like OperationOutcome<string, NotFound> here) can't express "denied" at all, so
        // denial must fail loudly and specifically rather than silently succeeding or crashing obscurely.
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<
            MisshapenProtectedQuery,
            OperationOutcome<string, Interlocutor.CommonKernel.ResultTypes.NotFound>
        >(policyEvaluator, DefaultOutcomeCaseRegistry.Instance);
        var request = new MisshapenProtectedQuery();
        Func<Task<OperationOutcome<string, Interlocutor.CommonKernel.ResultTypes.NotFound>>> next = () =>
            throw new InvalidOperationException("Should not be called");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, next, CancellationToken.None)
        );

        // Assert
        Assert.Contains("Forbidden", ex.Message);
    }

    [Fact]
    public async Task Handle_HandRolledNonUnionResponse_ShortCircuitsWithForbiddenViaCustomRegistry()
    {
        // Proves IOutcomeCaseRegistry is a genuine drop-in for UnionCaseFactory, not just for compiler
        // unions: HandRolledOutcome has no case constructor and no IUnion marker, so DefaultOutcomeCaseRegistry
        // alone could never construct it - it only works here because a custom factory is registered.
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var caseRegistry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<HandRolledOutcome, Forbidden>(HandRolledOutcome.FromForbidden)
            .Build();

        var behavior = new AuthorizationBehavior<ProtectedHandRolledQuery, HandRolledOutcome>(policyEvaluator, caseRegistry);
        var request = new ProtectedHandRolledQuery();
        Func<Task<HandRolledOutcome>> next = () => throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(typeof(Forbidden), result.CaseType);
        var forbidden = Assert.IsType<Forbidden>(result.CaseValue);
        Assert.Contains("CanRead", forbidden.Reason ?? string.Empty);
    }

    public record ProtectedQuery(string Name) : IQuery<OperationOutcome<string, Forbidden>>, IRequiresPolicy
    {
        public string PolicyName => "CanRead";
    }

    /// <summary>A request declaring a policy whose response has no <see cref="Forbidden"/> case at all.</summary>
    public record MisshapenProtectedQuery
        : IQuery<OperationOutcome<string, Interlocutor.CommonKernel.ResultTypes.NotFound>>,
            IRequiresPolicy
    {
        public string PolicyName => "CanRead";
    }

    /// <summary>A request whose response type is a hand-rolled, non-union <see cref="IOperationOutcome"/>.</summary>
    public record ProtectedHandRolledQuery : IRequiresPolicy
    {
        public string PolicyName => "CanRead";
    }

    /// <summary>A hand-rolled, non-union <see cref="IOperationOutcome"/> implementation - no compiler-generated
    /// case constructor and no <see cref="System.Runtime.CompilerServices.IUnion"/> marker, so it can only participate in
    /// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> via a custom <see cref="IOutcomeCaseRegistry"/>
    /// registration, never via <see cref="DefaultOutcomeCaseRegistry"/> alone.</summary>
    public sealed class HandRolledOutcome : IOperationOutcome
    {
        private HandRolledOutcome(Type caseType, object? caseValue)
        {
            this.CaseType = caseType;
            this.CaseValue = caseValue;
        }

        public Type CaseType { get; }

        public object? CaseValue { get; }

        public static HandRolledOutcome FromForbidden(Forbidden forbidden) => new(typeof(Forbidden), forbidden);
    }
}
