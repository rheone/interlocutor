namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>Tests for <see cref="LoggingAuditSink"/>.</summary>
public class LoggingAuditSinkTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LoggingAuditSink(null!));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task RecordAsync_LogsEntryAtInformationLevel()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingAuditSink>>();
        var sink = new LoggingAuditSink(logger);
        var entry = new AuditEntry("Order.Create", "CreateOrderCommand", "Success", DateTime.UtcNow, "user-1");

        // Act
        await sink.RecordAsync(entry, CancellationToken.None);

        // Assert
        logger
            .Received(1)
            .Log(
                Arg.Is(LogLevel.Information),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }
}
