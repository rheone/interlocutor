namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

/// <summary>
/// PROTOTYPE (spike/eject-withcase-case-fidelity): proves the full <see cref="Eject.WithCase{TCase}(TCase)"/> /
/// <see cref="PipelineEjectCaseException"/> mechanism end-to-end through a real <see cref="Mediator"/> dispatch
/// - not just <see cref="AuthorizationBehaviorTests"/>'s direct behavior calls, which stand in for the catch
/// site by hand. This proves the actual production catch site
/// (<see cref="PipelineChainExecutor"/>, reached here through <see cref="QueryHandlerWrapper{TQuery, TResponse}"/>)
/// resolves <see cref="IOutcomeCaseRegistry"/> from a real <see cref="IServiceProvider"/> and performs the
/// construct-and-return call correctly, for both the success path (response has a <see cref="Forbidden"/>
/// case) and the fail-fast path (it doesn't).
/// </summary>
public class AuthorizationBehaviorEjectCaseEndToEndTests
{
    [Fact]
    public async Task SendAsync_DeniedRequest_ReturnsForbiddenConstructedByRealDispatchMachinery()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(policyEvaluator);
        services.AddSingleton(DefaultOutcomeCaseRegistry.Instance);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped<IQueryHandler<ProtectedQuery, Outcome<string, Forbidden>>, ProtectedQueryHandler>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = new Mediator(scope.ServiceProvider);

        // Act - the full round trip: AuthorizationBehavior ejects Forbidden with no IOutcomeCaseRegistry of
        // its own, PipelineEjectCaseException carries it to QueryHandlerWrapper's shared
        // PipelineChainExecutor catch site, which resolves IOutcomeCaseRegistry from this real provider and
        // constructs Outcome<string, Forbidden> from it.
        var result = await mediator.SendAsync(new ProtectedQuery(), TestContext.Current.CancellationToken);

        // Assert
        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Contains("CanRead", forbidden.Reason ?? string.Empty);
    }

    [Fact]
    public async Task SendAsync_DeniedRequest_ResponseTypeWithNoForbiddenCase_ThrowsNamedExceptionFromRealDispatchMachinery()
    {
        // Arrange - same mismatch AuthorizationBehaviorTests.Handle_WithResponseTypeThatHasNoForbiddenCase_ThrowsNamedExceptionOnDenial
        // proves at the behavior level, proven here at the real catch site instead.
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(policyEvaluator);
        services.AddSingleton(DefaultOutcomeCaseRegistry.Instance);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped<IQueryHandler<MisshapenProtectedQuery, Outcome<string, NotFound>>, MisshapenProtectedQueryHandler>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = new Mediator(scope.ServiceProvider);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new MisshapenProtectedQuery(), TestContext.Current.CancellationToken)
        );

        // Assert
        Assert.Contains("Forbidden", ex.Message);
    }

    private sealed record ProtectedQuery : IQuery<Outcome<string, Forbidden>>, IRequiresPolicyRequest
    {
        public string PolicyName => "CanRead";
    }

    private sealed class ProtectedQueryHandler : IQueryHandler<ProtectedQuery, Outcome<string, Forbidden>>
    {
        public Task<Outcome<string, Forbidden>> HandleAsync(ProtectedQuery query, CancellationToken ct) =>
            throw new InvalidOperationException("Should not be called - authorization denies first.");
    }

    /// <summary>A request declaring a policy whose response has no <see cref="Forbidden"/> case at all.</summary>
    private sealed record MisshapenProtectedQuery : IQuery<Outcome<string, NotFound>>, IRequiresPolicyRequest
    {
        public string PolicyName => "CanRead";
    }

    private sealed class MisshapenProtectedQueryHandler : IQueryHandler<MisshapenProtectedQuery, Outcome<string, NotFound>>
    {
        public Task<Outcome<string, NotFound>> HandleAsync(MisshapenProtectedQuery query, CancellationToken ct) =>
            throw new InvalidOperationException("Should not be called - authorization denies first.");
    }
}
