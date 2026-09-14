namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;

/// <summary>Tests for <see cref="ConditionalBehavior{TRequest, TResponse}"/>.</summary>
public class ConditionalBehaviorTests
{
    [Fact]
    public async Task HandleAsync_PredicateTrue_RunsInnerInsteadOfCallingNextDirectly()
    {
        // Arrange
        var inner = new RecordingBehavior();
        var sut = new ConditionalBehavior<string, string>(inner, _ => true);

        // Act
        var result = await sut.HandleAsync("request", () => Task.FromResult("next-result"), CancellationToken.None);

        // Assert
        Assert.True(inner.WasInvoked);
        Assert.Equal("inner-result", result);
    }

    [Fact]
    public async Task HandleAsync_PredicateFalse_SkipsInnerAndCallsNextDirectly()
    {
        // Arrange
        var inner = new RecordingBehavior();
        var sut = new ConditionalBehavior<string, string>(inner, _ => false);

        // Act
        var result = await sut.HandleAsync("request", () => Task.FromResult("next-result"), CancellationToken.None);

        // Assert
        Assert.False(inner.WasInvoked);
        Assert.Equal("next-result", result);
    }

    [Fact]
    public async Task HandleAsync_PredicateEvaluatedAgainstTheRequestInstance()
    {
        // Arrange
        var seen = new List<string>();
        var sut = new ConditionalBehavior<string, string>(
            new RecordingBehavior(),
            request =>
            {
                seen.Add(request);
                return true;
            }
        );

        // Act
        await sut.HandleAsync("canary-me", () => Task.FromResult("next-result"), CancellationToken.None);

        // Assert
        Assert.Equal(["canary-me"], seen);
    }

    /// <summary>A behavior that records whether it ran, returning a fixed result rather than calling <c>next</c>.</summary>
    private sealed class RecordingBehavior : IPipelineBehavior<string, string>
    {
        public bool WasInvoked { get; private set; }

        public Task<string> HandleAsync(string request, Func<Task<string>> next, CancellationToken ct)
        {
            this.WasInvoked = true;
            return Task.FromResult("inner-result");
        }
    }
}
