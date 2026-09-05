namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves ticket 04's guarantee specifically against the compile-time generated dispatch switch (ticket 13,
/// <see cref="IGeneratedRequestDispatcher"/>) rather than the reflection fallback: a per-request-type
/// override resolves identically whichever runtime-typed dispatch sub-path handles the request. This is the
/// one sub-path <c>Interlocutor.Generator</c>'s <c>DispatchWrapperEmitter</c> emits its own source for (see
/// that emitter's remarks) - the reflection fallback re-invokes the ordinary typed <c>Send</c> overload
/// instead of reproducing chain-building itself, so it is already covered by
/// <c>Interlocutor.Abstractions.Tests.MediatorTests</c>' override coverage and needs no separate proof here.
/// </summary>
public sealed class GeneratedDispatchSwitchOverrideTests
{
    /// <summary>
    /// The request type the override rule targets implements this marker so <see cref="KeptGeneratedSwitchBehavior{TRequest,TResponse}"/>
    /// can be constrained to it - without this, an unconstrained override behavior would also resolve for
    /// <see cref="GeneratedSwitchUnrelatedQuery"/> via ordinary open-generic resolution, since declaring it in
    /// an override rule registers it exactly as an ordinary <c>AddBehavior</c> call would.
    /// </summary>
    public interface IOverrideTarget;

    [Fact]
    public async Task Send_Object_MatchedByOverrideRule_RunsOnlyDeclaredBehaviorsThroughTheGeneratedSwitch()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddInterlocutor(builder =>
            builder
                .AddGeneratedRegistrations(
                    Interlocutor
                        .Generated
                        .Interlocutor_DependencyInjection_Microsoft_Tests
                        .InterlocutorGeneratedRegistrations
                        .Registrations
                )
                .Pipeline.AddBehavior(typeof(DroppedGeneratedSwitchBehavior<,>))
                .OverridePipelineFor<GeneratedSwitchOverriddenQuery>("kept-only", typeof(KeptGeneratedSwitchBehavior<,>))
        );
        services.AddSingleton(log);
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        object request = new GeneratedSwitchOverriddenQuery(21);

        // Act
        await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert - "dropped" must never appear: this request is matched by the override rule, so only the
        // declared KeptGeneratedSwitchBehavior may run, in place of the general-purpose baseline entirely.
        Assert.Equal(["kept"], log);
    }

    [Fact]
    public async Task Send_Object_UnmatchedByOverrideRule_RunsGeneralPurposeBaselineThroughTheGeneratedSwitch()
    {
        // Arrange - an override registry is present (targeting GeneratedSwitchOverriddenQuery), but
        // GeneratedSwitchUnrelatedQuery isn't matched by it, so it must still resolve the general-purpose
        // baseline (DroppedGeneratedSwitchBehavior, registered unconstrained) through the generated switch.
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddInterlocutor(builder =>
            builder
                .AddGeneratedRegistrations(
                    Interlocutor
                        .Generated
                        .Interlocutor_DependencyInjection_Microsoft_Tests
                        .InterlocutorGeneratedRegistrations
                        .Registrations
                )
                .Pipeline.AddBehavior(typeof(DroppedGeneratedSwitchBehavior<,>))
                .OverridePipelineFor<GeneratedSwitchOverriddenQuery>("kept-only", typeof(KeptGeneratedSwitchBehavior<,>))
        );
        services.AddSingleton(log);
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        object request = new GeneratedSwitchUnrelatedQuery(7);

        // Act
        await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["dropped"], log);
    }

    public sealed record GeneratedSwitchOverriddenQuery(int Value) : IQuery<Outcome<int, Failure>>, IOverrideTarget;

    public sealed record GeneratedSwitchUnrelatedQuery(int Value) : IQuery<Outcome<int, Failure>>;

    public sealed class GeneratedSwitchUnrelatedQueryHandler
        : IQueryHandler<GeneratedSwitchUnrelatedQuery, Outcome<int, Failure>>
    {
        public Task<Outcome<int, Failure>> HandleAsync(GeneratedSwitchUnrelatedQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<int, Failure>>(query.Value * 2);
    }

    public sealed class GeneratedSwitchOverriddenQueryHandler
        : IQueryHandler<GeneratedSwitchOverriddenQuery, Outcome<int, Failure>>
    {
        public Task<Outcome<int, Failure>> HandleAsync(GeneratedSwitchOverriddenQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<int, Failure>>(query.Value * 2);
    }

    public sealed class KeptGeneratedSwitchBehavior<TRequest, TResponse>(List<string> log)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IOverrideTarget
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Add("kept");
            return next();
        }
    }

    public sealed class DroppedGeneratedSwitchBehavior<TRequest, TResponse>(List<string> log)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Add("dropped");
            return next();
        }
    }
}
