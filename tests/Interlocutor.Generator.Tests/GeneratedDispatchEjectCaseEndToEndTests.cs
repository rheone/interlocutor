namespace Interlocutor.Generator.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;
using Interlocutor.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Proves ticket 01's gap and its fix end-to-end through a real, emitted-and-loaded generator-produced
/// dispatch wrapper (<c>DispatchWrapperEmitter</c>) rather than the hand-written
/// <c>QueryHandlerWrapper</c>/<c>PipelineChainExecutor</c> path
/// <c>Interlocutor.Abstractions.Tests.AuthorizationBehaviorEjectCaseEndToEndTests</c> already covers.
/// </summary>
/// <remarks>
/// Before ticket 01's fix, the generator emitted its own hand-templated fold/eject-catch logic that only
/// caught <c>PipelineEjectException&lt;TResponse&gt;</c>, never <see cref="PipelineEjectCaseException"/> - so
/// a request dispatched through generator-emitted code that ejected via <see cref="Eject.WithCase{TCase}(TCase)"/>
/// (as <see cref="AuthorizationBehavior{TRequest, TResponse}"/> does) propagated the exception unhandled
/// instead of recovering. After the fix, the emitted wrapper calls the shared
/// <c>Interlocutor.Abstractions.Pipeline.PipelineChainExecutor.ExecuteAsync</c> implementation instead of
/// re-deriving the fold, so it recovers identically to the hand-written path.
/// </remarks>
public sealed class GeneratedDispatchEjectCaseEndToEndTests
{
    private const string LibrarySource = """
        namespace TestApp;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Markers;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        public sealed record ProtectedQuery : IQuery<Outcome<string, Forbidden>>, IRequiresPolicyRequest
        {
            public string PolicyName => "CanRead";
        }

        public sealed class ProtectedQueryHandler : IQueryHandler<ProtectedQuery, Outcome<string, Forbidden>>
        {
            public Task<Outcome<string, Forbidden>> HandleAsync(ProtectedQuery query, CancellationToken ct) =>
                throw new System.InvalidOperationException("Should not be called - authorization denies first.");
        }
        """;

    [Fact]
    public async Task SendAsync_DeniedRequest_ThroughGeneratedDispatch_RecoversForbiddenInsteadOfPropagatingEjectException()
    {
        // Arrange - emit and load a real assembly so the request travels through the generator's actual
        // emitted dispatch wrapper (via the runtime-typed SendAsync(object, ct) overload, which is the only
        // overload that consults IGeneratedRequestDispatcher - see Mediator.DispatchAsync's remarks), not the
        // hand-written QueryHandlerWrapper the generic SendAsync<TResponse> overloads always use.
        var (assembly, _) = GeneratorTestHarness.EmitAndLoadAssembly(LibrarySource, $"GeneratedEjectCase_{Guid.NewGuid():N}");

        var moduleType = assembly
            .GetTypes()
            .Single(static t =>
                t.Name == "InterlocutorGeneratedRegistrations"
                && t.Namespace!.StartsWith("Interlocutor.Generated.", StringComparison.Ordinal)
            );
        var registrations =
            (IReadOnlyList<InterlocutorRegistration>)
                moduleType.GetProperty("Registrations", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        var request = Activator.CreateInstance(assembly.GetType("TestApp.ProtectedQuery")!)!;

        var services = new ServiceCollection();
        services.AddSingleton<IPolicyEvaluator>(new DenyEverythingPolicyEvaluator());
        services.AddSingleton(DefaultOutcomeCaseRegistry.Instance);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddInterlocutor(builder => builder.AddGeneratedRegistrations(registrations));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act - AuthorizationBehavior ejects Forbidden via Eject.WithCase (it has no IOutcomeCaseRegistry
        // dependency of its own); the generated dispatch wrapper must recover it the same way the hand-written
        // PipelineChainExecutor-based wrappers do.
        var outcome = await mediator.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var outcomeType = outcome.GetType();
        var activeCaseType = (Type?)outcomeType.GetProperty("ActiveCaseType")!.GetValue(outcome);
        Assert.Equal(typeof(Forbidden), activeCaseType);
    }

    /// <summary>An <see cref="IPolicyEvaluator"/> that always denies, forcing <see cref="AuthorizationBehavior{TRequest, TResponse}"/> to eject.</summary>
    private sealed class DenyEverythingPolicyEvaluator : IPolicyEvaluator
    {
        public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct) => Task.FromResult(false);
    }
}
