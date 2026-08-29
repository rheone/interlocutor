namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

// AuditingBehavior<TRequest, TResponse> now constrains TRequest : IAuditableRequest, so a non-auditable
// request cannot close this generic at all - there is no runtime "skip" branch left to unit test here. The
// "a request without the marker never resolves the behavior" guarantee is proven at the container level
// instead: see BehaviorSubsetConstraintTests in the DependencyInjection.Microsoft/Autofac test projects.
public class AuditingBehaviorTests
{
    [Fact]
    public async Task Handle_RecordsAuditEntryForAuditableRequest()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        const string response = "success";
        static Task<string> Next()
        {
            return Task.FromResult(response);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

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
    public async Task Handle_IncludesActionNameInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        static Task<string> Next()
        {
            return Task.FromResult("response");
        }

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

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
        static Task<string> Next()
        {
            return Task.FromResult("response");
        }

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

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
        Task<Failure> Next()
        {
            return Task.FromResult(response);
        }

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.Outcome == "Failure"), CancellationToken.None);
    }

    private static readonly string[] Expected = ["handler", "audit"];

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
        Assert.Equal(Expected, callOrder);
    }

    [Fact]
    public async Task Handle_IncludesTimestampInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        static Task<string> Next()
        {
            return Task.FromResult("response");
        }

        var beforeCall = DateTime.UtcNow;

        // Act
        await behavior.Handle(request, Next, CancellationToken.None);

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
        const string expectedResponse = "my-response";
        static Task<string> Next()
        {
            return Task.FromResult(expectedResponse);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

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
        const int expectedValue = 42;
        static Task<int> Next()
        {
            return Task.FromResult(expectedValue);
        }

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
        static Task<string?> Next()
        {
            return Task.FromResult<string?>(null);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.Null(result);
        await sink.Received(1).RecordAsync(Arg.Any<AuditEntry>(), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithContextAccessorReportingUser_PopulatesUserIdInAuditEntry()
    {
        // Arrange
        var sink = Substitute.For<IAuditSink>();
        var contextAccessor = Substitute.For<IRequestContextAccessor>();
        contextAccessor.Context.Returns(new RequestContext { UserId = "user-42" });
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink, contextAccessor: contextAccessor);
        var request = new AuditableQuery("test");
        static Task<string> Next()
        {
            return Task.FromResult("response");
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - the response is unaffected, and UserId flows through end-to-end from the accessor.
        Assert.Equal("response", result);
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.UserId == "user-42"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithContextAccessorReportingNoUser_LeavesUserIdNull()
    {
        // Arrange - accessor is registered but has no context populated (e.g. an anonymous request).
        var sink = Substitute.For<IAuditSink>();
        var contextAccessor = Substitute.For<IRequestContextAccessor>();
        contextAccessor.Context.Returns((RequestContext?)null);
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink, contextAccessor: contextAccessor);
        var request = new AuditableQuery("test");
        static Task<string> Next()
        {
            return Task.FromResult("response");
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - no exception, auditing still succeeds, UserId stays null.
        Assert.Equal("response", result);
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.UserId == null), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithNoContextAccessorRegistered_LeavesUserIdNull()
    {
        // Arrange - no IRequestContextAccessor supplied at all (the default for the constructor's optional
        // parameter), mirroring a consumer that never registered one.
        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink);
        var request = new AuditableQuery("test");
        static Task<string> Next()
        {
            return Task.FromResult("response");
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - no exception, auditing still succeeds, UserId stays null.
        Assert.Equal("response", result);
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.UserId == null), CancellationToken.None);
    }

    public record AuditableQuery(string Name) : IQuery<OperationOutcome<string, Failure>>, IAuditableRequest
    {
        public string ActionName => "Query.Read";
    }
}
