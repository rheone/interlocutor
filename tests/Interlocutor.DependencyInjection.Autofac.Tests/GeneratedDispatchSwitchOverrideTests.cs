namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves ticket 04's guarantee specifically against the compile-time generated dispatch switch (ticket 13,
/// <see cref="IGeneratedRequestDispatcher"/>) under Autofac - the mirror of
/// <c>Interlocutor.DependencyInjection.Microsoft.Tests.GeneratedDispatchSwitchOverrideTests</c>. See that
/// class's remarks for why the reflection-fallback sub-path needs no separate proof.
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
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(log).AsSelf();
        containerBuilder.AddInterlocutor(builder =>
            builder
                .AddGeneratedRegistrations(
                    Interlocutor
                        .Generated
                        .Interlocutor_DependencyInjection_Autofac_Tests
                        .InterlocutorGeneratedRegistrations
                        .Registrations
                )
                .Pipeline.AddBehavior(typeof(DroppedGeneratedSwitchBehavior<,>))
                .OverridePipelineFor<GeneratedSwitchOverriddenQuery>("kept-only", typeof(KeptGeneratedSwitchBehavior<,>))
        );
        var container = containerBuilder.Build();
        await using var scope = container.BeginLifetimeScope();
        var sender = scope.Resolve<ISender>();
        object request = new GeneratedSwitchOverriddenQuery(21);

        // Act
        await sender.Send(request, TestContext.Current.CancellationToken);

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
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(log).AsSelf();
        containerBuilder.AddInterlocutor(builder =>
            builder
                .AddGeneratedRegistrations(
                    Interlocutor
                        .Generated
                        .Interlocutor_DependencyInjection_Autofac_Tests
                        .InterlocutorGeneratedRegistrations
                        .Registrations
                )
                .Pipeline.AddBehavior(typeof(DroppedGeneratedSwitchBehavior<,>))
                .OverridePipelineFor<GeneratedSwitchOverriddenQuery>("kept-only", typeof(KeptGeneratedSwitchBehavior<,>))
        );
        var container = containerBuilder.Build();
        await using var scope = container.BeginLifetimeScope();
        var sender = scope.Resolve<ISender>();
        object request = new GeneratedSwitchUnrelatedQuery(7);

        // Act
        await sender.Send(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["dropped"], log);
    }

    public sealed record GeneratedSwitchOverriddenQuery(int Value) : IQuery<OperationOutcome<int, Failure>>, IOverrideTarget;

    public sealed record GeneratedSwitchUnrelatedQuery(int Value) : IQuery<OperationOutcome<int, Failure>>;

    public sealed class GeneratedSwitchOverriddenQueryHandler
        : IQueryHandler<GeneratedSwitchOverriddenQuery, OperationOutcome<int, Failure>>
    {
        public Task<OperationOutcome<int, Failure>> Handle(GeneratedSwitchOverriddenQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<int, Failure>>(query.Value * 2);
    }

    public sealed class GeneratedSwitchUnrelatedQueryHandler
        : IQueryHandler<GeneratedSwitchUnrelatedQuery, OperationOutcome<int, Failure>>
    {
        public Task<OperationOutcome<int, Failure>> Handle(GeneratedSwitchUnrelatedQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<int, Failure>>(query.Value * 2);
    }

    public sealed class KeptGeneratedSwitchBehavior<TRequest, TResponse>(List<string> log)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IOverrideTarget
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Add("kept");
            return next();
        }
    }

    public sealed class DroppedGeneratedSwitchBehavior<TRequest, TResponse>(List<string> log)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Add("dropped");
            return next();
        }
    }
}
