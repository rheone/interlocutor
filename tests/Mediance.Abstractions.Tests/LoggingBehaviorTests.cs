using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel.ResultTypes;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Mediance.Abstractions.Tests;

public class LoggingBehaviorTests
{
    public record LoggedQuery(int Value) : IQuery<int>;

    [Fact]
    public async Task Handle_LogsRequestTypeAndOutcome()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggedQuery, int>>>();
        var behavior = new LoggingBehavior<LoggedQuery, int>(logger);
        var request = new LoggedQuery(42);
        var response = 84;
        var next = () => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(84, result);
        // Verify that logging occurred (at least one call to LogInformation)
        logger.Received(1).Log(
            Arg.Is(LogLevel.Information),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task Handle_LogsSuccessOutcome()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggedQuery, Success>>>();
        var behavior = new LoggingBehavior<LoggedQuery, Success>(logger);
        var request = new LoggedQuery(1);
        var response = new Success();
        var next = () => Task.FromResult(response);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        logger.Received(1).Log(
            Arg.Is(LogLevel.Information),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task Handle_LogsFailureOutcome()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggedQuery, Failure>>>();
        var behavior = new LoggingBehavior<LoggedQuery, Failure>(logger);
        var request = new LoggedQuery(1);
        var response = new Failure("Operation failed");
        var next = () => Task.FromResult(response);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        logger.Received(1).Log(
            Arg.Is(LogLevel.Information),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task Handle_LogsExceptionAsError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggedQuery, int>>>();
        var behavior = new LoggingBehavior<LoggedQuery, int>(logger);
        var request = new LoggedQuery(1);
        var nextException = new InvalidOperationException("Handler failed");
        Task<int> Next() => throw nextException;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, Next, CancellationToken.None)
        );

        // Assert - Should log error
        logger.Received(1).Log(
            Arg.Is(LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is(nextException),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public async Task Handle_ReturnsOriginalResponse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggedQuery, int>>>();
        var behavior = new LoggingBehavior<LoggedQuery, int>(logger);
        var request = new LoggedQuery(21);
        var next = () => Task.FromResult(42);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Handle_DoesNotInterfereWithRequestHandling()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggedQuery, int>>>();
        var behavior = new LoggingBehavior<LoggedQuery, int>(logger);
        var request = new LoggedQuery(99);
        var expectedValue = 198;

        Task<int> Next() => Task.FromResult(expectedValue);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, result);
    }
}
