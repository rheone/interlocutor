namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves ticket 01's per-request-type pipeline override guarantees against Microsoft DI, end to end through
/// a real <see cref="IMediator"/> dispatch - not just a registration-list check. Method-for-method identical to
/// <c>Interlocutor.DependencyInjection.Autofac.Tests.PipelineOverrideParityTests</c> (ticket 02), so a
/// divergence between the two containers shows up as one of the two failing while the other passes.
/// </summary>
public class PipelineOverrideParityTests
{
    /// <summary>
    /// A request type an override rule targets implements this marker so <see cref="KeptOverrideBehavior{TRequest,TResponse}"/>
    /// can be constrained to it - the same way a real override behavior would scope itself to its own domain
    /// (see <c>InterlocutorPipelineBuilder.OverridePipelineFor</c>'s remarks on the general-purpose-baseline
    /// leak an unconstrained override behavior would otherwise cause). Without this constraint, declaring the
    /// behavior in an override rule would also make it resolve for every other, unrelated request type via
    /// the container's ordinary open-generic resolution, since <c>OverridePipelineFor</c> registers it exactly
    /// as an ordinary <c>AddBehavior</c> call would.
    /// </summary>
    private interface IOverrideTarget;

    [Fact]
    public async Task MatchedRequest_RunsExactlyTheDeclaredOverrideBehaviors()
    {
        // Arrange
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineOverrideParityTests).Assembly)
                .Pipeline.AddBehavior(typeof(DroppedOverrideBehavior<,>))
                .OverridePipelineFor<OverriddenCommand>("kept-only", typeof(KeptOverrideBehavior<,>))
        );
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ExecutionLog>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new OverriddenCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], log.Entries);
    }

    [Fact]
    public async Task UnmatchedRequest_StillRunsTheGeneralPurposeBaseline()
    {
        // Arrange
        var services = NewServices();
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineOverrideParityTests).Assembly)
                .Pipeline.AddBehavior(typeof(DroppedOverrideBehavior<,>))
                .OverridePipelineFor<OverriddenCommand>("kept-only", typeof(KeptOverrideBehavior<,>))
        );
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ExecutionLog>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act - UnrelatedCommand is not matched by the override rule, so it must draw from the unaffected
        // general-purpose baseline: DroppedOverrideBehavior, registered unconstrained.
        await mediator.Send(new UnrelatedCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(["dropped"], log.Entries);
    }

    [Fact]
    public async Task MatchedRequest_SatisfyingAnAuditableMarker_DoesNotRunAuditingBehaviorUnlessNamed()
    {
        // Arrange - AuditableOverriddenCommand structurally satisfies AuditingBehavior's IAuditableRequest
        // constraint, so it would resolve for it under ordinary baseline resolution; the override rule
        // matches this request but names only KeptOverrideBehavior, so AuditingBehavior must not run.
        var services = NewServices();
        var auditSink = new RecordingAuditSink();
        services.AddSingleton<IAuditSink>(auditSink);
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineOverrideParityTests).Assembly)
                .Pipeline.AddAuditingBehavior()
                .OverridePipelineFor<AuditableOverriddenCommand>("kept-only", typeof(KeptOverrideBehavior<,>))
        );
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ExecutionLog>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new AuditableOverriddenCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], log.Entries);
        Assert.False(auditSink.WasCalled);
    }

    /// <summary>
    /// A fresh <see cref="ServiceCollection"/> with a null <see cref="ILogger{TCategoryName}"/> registered and
    /// <see cref="ExecutionLog"/> seeded as a singleton so every behavior instance the container constructs
    /// for a given test shares the same log.
    /// </summary>
    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ExecutionLog>();
        return services;
    }

    private sealed class ExecutionLog
    {
        public List<string> Entries { get; } = [];
    }

    private sealed class KeptOverrideBehavior<TRequest, TResponse>(ExecutionLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IOverrideTarget
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Entries.Add("kept");
            return next();
        }
    }

    private sealed class DroppedOverrideBehavior<TRequest, TResponse>(ExecutionLog log) : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            log.Entries.Add("dropped");
            return next();
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public bool WasCalled { get; private set; }

        public Task RecordAsync(AuditEntry entry, CancellationToken ct)
        {
            this.WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed record OverriddenCommand(string Name) : ICommand<OperationOutcome<string, Forbidden>>, IOverrideTarget;

    // The three handler classes below are resolved only via AddInterlocutor()'s assembly-scanning DI
    // registration - never new'd or typeof'd directly in this file - so their constructors are
    // invisible to static analysis.
#pragma warning disable S1144
    private sealed class OverriddenCommandHandler : ICommandHandler<OverriddenCommand, OperationOutcome<string, Forbidden>>
    {
        public Task<OperationOutcome<string, Forbidden>> Handle(OverriddenCommand command, CancellationToken ct) =>
            Task.FromResult(new OperationOutcome<string, Forbidden>(command.Name));
    }

    private sealed record UnrelatedCommand(string Name) : ICommand<OperationOutcome<string, Forbidden>>;

    private sealed class UnrelatedCommandHandler : ICommandHandler<UnrelatedCommand, OperationOutcome<string, Forbidden>>
    {
        public Task<OperationOutcome<string, Forbidden>> Handle(UnrelatedCommand command, CancellationToken ct) =>
            Task.FromResult(new OperationOutcome<string, Forbidden>(command.Name));
    }

    private sealed record AuditableOverriddenCommand(string Name)
        : ICommand<OperationOutcome<string, Forbidden>>,
            IAuditableRequest,
            IOverrideTarget
    {
        public string ActionName => "Test.Audit";
    }

    private sealed class AuditableOverriddenCommandHandler
        : ICommandHandler<AuditableOverriddenCommand, OperationOutcome<string, Forbidden>>
    {
        public Task<OperationOutcome<string, Forbidden>> Handle(AuditableOverriddenCommand command, CancellationToken ct) =>
            Task.FromResult(new OperationOutcome<string, Forbidden>(command.Name));
    }
#pragma warning restore S1144
}
