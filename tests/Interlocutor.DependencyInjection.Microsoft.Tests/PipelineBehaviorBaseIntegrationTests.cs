namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves a hand-authored <see cref="PipelineBehaviorBase{TRequest, TResponse}"/> subclass registers and
/// runs correctly through the existing <see cref="InterlocutorPipelineBuilder.AddBehavior(System.Type, RegistrationLifetime)"/>
/// path - unchanged by this ticket - end to end through a real built container and a real mediator dispatch,
/// not merely by inspecting the registration list.
/// </summary>
public class PipelineBehaviorBaseIntegrationTests
{
    [Fact]
    public async Task HandAuthoredSubclass_RegisteredViaAddBehavior_RunsEnterAndExitAroundTheHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineBehaviorBaseIntegrationTests).Assembly)
                .Pipeline.AddBehavior(typeof(RecordingPipelineBehavior))
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new SampleBehaviorQuery(), TestContext.Current.CancellationToken);

        // Assert: OnEnterAsync ran before the handler, OnExitAsync ran after it and replaced the response.
        Assert.Equal(["enter", "handler", "exit"], executionLog);
        Assert.True(result.TryGetValue(out string replaced));
        Assert.Equal("replaced-by-exit", replaced);
    }

    [Fact]
    public async Task HandAuthoredSubclass_WhenOnEnterShortCircuits_HandlerNeverRunsButExitStillDoes()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddSingleton(new ShortCircuitSwitch(ShortCircuit: true));

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineBehaviorBaseIntegrationTests).Assembly)
                .Pipeline.AddBehavior(typeof(RecordingPipelineBehavior))
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new SampleBehaviorQuery(), TestContext.Current.CancellationToken);

        // Assert: the handler's own "handler" log entry never appears, but exit still ran, paired with enter.
        Assert.Equal(["enter", "exit"], executionLog);
        Assert.True(result.TryGetValue(out string replaced));
        Assert.Equal("replaced-by-exit", replaced);
    }

    private sealed record ShortCircuitSwitch(bool ShortCircuit);

    private sealed record SampleBehaviorQuery : IQuery<Outcome<string, Failure>>;

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or typeof'd
    // directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class SampleBehaviorQueryHandler(List<string> log)
        : IQueryHandler<SampleBehaviorQuery, Outcome<string, Failure>>
    {
        public Task<Outcome<string, Failure>> HandleAsync(SampleBehaviorQuery query, CancellationToken ct)
        {
            log.Add("handler");
            return Task.FromResult<Outcome<string, Failure>>("handled");
        }
    }
#pragma warning restore S1144

    /// <summary>
    /// A hand-authored <see cref="PipelineBehaviorBase{TRequest, TResponse}"/> subclass - the exact shape
    /// this ticket adds a convenience base class for - closed over <see cref="SampleBehaviorQuery"/> and
    /// registered through the pre-existing <c>AddBehavior(Type, RegistrationLifetime)</c> escape hatch like
    /// any other behavior. Short-circuits when <see cref="ShortCircuitSwitch"/> says to (defaulting to
    /// "always continue" when the switch was never registered), and always replaces the response from
    /// <see cref="OnExitAsync"/> to prove that hook's override-the-response contract, not just
    /// <see cref="IPipelineStage{TRequest, TResponse}"/>'s observe-only one.
    /// </summary>
    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or typeof'd
    // directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class RecordingPipelineBehavior(List<string> log, ShortCircuitSwitch? shortCircuitSwitch = null)
        : PipelineBehaviorBase<SampleBehaviorQuery, Outcome<string, Failure>>
    {
        protected override Task<StageResult<Outcome<string, Failure>>> OnEnterAsync(
            SampleBehaviorQuery request,
            CancellationToken ct
        )
        {
            log.Add("enter");
            return Task.FromResult(
                shortCircuitSwitch is { ShortCircuit: true }
                    ? StageResult<Outcome<string, Failure>>.ShortCircuit("short-circuited")
                    : StageResult<Outcome<string, Failure>>.Continue
            );
        }

        protected override Task<Outcome<string, Failure>> OnExitAsync(
            SampleBehaviorQuery request,
            Outcome<string, Failure> result,
            CancellationToken ct
        )
        {
            log.Add("exit");
            return Task.FromResult<Outcome<string, Failure>>("replaced-by-exit");
        }
    }
#pragma warning restore S1144
}
