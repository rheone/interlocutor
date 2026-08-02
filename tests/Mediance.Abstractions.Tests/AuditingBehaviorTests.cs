using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel.ResultTypes;
using NSubstitute;

namespace Mediance.Abstractions.Tests;

public class AuditingBehaviorTests
{
    public record AuditableQuery(string Name) : IQuery<string>, IAuditableRequest
    {
        public string ActionName => "Query.Read";
    }

    public record UnsauditableQuery(string Name) : IQuery<string>;

    [Fact]
    public async Task Handle_RecordsAuditEntryForAuditableRequest()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        var response = "success";
        var next = () => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        await sink.Received(1)
            .RecordAsync(
                Arg.Is<AuditEntry>(e =>
                    e.ActionName == "Query.Read" && e.RequestType == typeof(AuditableQuery).Name && e.Outcome == "String" // The response type name
                ),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task Handle_PassesThroughForNonAuditableRequest()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<UnsauditableQuery, string>(sink);
        var request = new UnsauditableQuery("test");
        var response = "success";
        var next = () => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        // Sink should not be called for non-auditable requests
        await sink.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncludesActionNameInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        var next = () => Task.FromResult("response");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.ActionName == "Query.Read"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_IncludesRequestTypeInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        var next = () => Task.FromResult("response");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.RequestType == "AuditableQuery"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_IncludesOutcomeDescriptionInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, Failure>(sink);
        var request = new AuditableQuery("test");
        var response = new Failure("Operation failed");
        var next = () => Task.FromResult(response);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.Outcome == "Failure"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_RecordsAuditEntryAfterHandlerCompletes()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        var callOrder = new List<string>();

        async Task<string> Next()
        {
            callOrder.Add("handler");
            return "response";
        }

        // Override sink to track call order
        sink.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0))
            .AndDoes(_ => callOrder.Add("audit"));

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - Handler should be called before audit is recorded
        Assert.Equal(new[] { "handler", "audit" }, callOrder);
    }

    [Fact]
    public async Task Handle_IncludesTimestampInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        var next = () => Task.FromResult("response");
        var beforeCall = DateTime.UtcNow;

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        var afterCall = DateTime.UtcNow;

        // Assert - Timestamp should be within expected range
        await sink.Received(1)
            .RecordAsync(
                Arg.Is<AuditEntry>(e => e.Timestamp >= beforeCall && e.Timestamp <= afterCall),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task Handle_ReturnsOriginalResponse()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        var expectedResponse = "my-response";
        var next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task Handle_DoesNotInterfereWithResponseHandling()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, int>(sink);
        var request = new AuditableQuery("test");
        var expectedValue = 42;
        Task<int> Next() => Task.FromResult(expectedValue);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task Handle_RecordsAuditEntryEvenOnNullResponse()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string?>(sink);
        var request = new AuditableQuery("test");
        var next = () => Task.FromResult<string?>(null);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Null(result);
        await sink.Received(1).RecordAsync(Arg.Any<AuditEntry>(), CancellationToken.None);
    }
}
