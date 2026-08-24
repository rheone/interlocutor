namespace Interlocutor.DependencyInjection.Abstractions.Tests;

// Auto Generated, verify expected behavior:
// InterlocutorGeneratedHandlerAttribute had no direct test coverage - only exercised by the generator itself
// (not part of this project) and referenced in doc comments elsewhere in this project's tests. Covers the
// constructor's property assignment and its ArgumentNullException.ThrowIfNull(requestType) guard, since
// ResponseType legitimately accepts null (void-command handlers) while RequestType does not.
public class InterlocutorGeneratedHandlerAttributeTests
{
    [Fact]
    public void Constructor_AssignsRequestTypeResponseTypeAndKind()
    {
        // Arrange & Act
        var attribute = new InterlocutorGeneratedHandlerAttribute(
            typeof(SampleRequest),
            typeof(SampleResponse),
            GeneratedHandlerKind.Query
        );

        // Assert
        Assert.Equal(typeof(SampleRequest), attribute.RequestType);
        Assert.Equal(typeof(SampleResponse), attribute.ResponseType);
        Assert.Equal(GeneratedHandlerKind.Query, attribute.Kind);
    }

    [Fact]
    public void Constructor_WithNullResponseType_IsAccepted_ForVoidCommandHandlers()
    {
        // Arrange & Act
        var attribute = new InterlocutorGeneratedHandlerAttribute(
            typeof(SampleRequest),
            null,
            GeneratedHandlerKind.VoidCommand
        );

        // Assert
        Assert.Null(attribute.ResponseType);
        Assert.Equal(GeneratedHandlerKind.VoidCommand, attribute.Kind);
    }

    [Fact]
    public void Constructor_WithNullRequestType_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new InterlocutorGeneratedHandlerAttribute(null!, typeof(SampleResponse), GeneratedHandlerKind.Query)
        );

        // Assert
        Assert.Equal("requestType", ex.ParamName);
    }

    private sealed record SampleRequest;

    private sealed record SampleResponse;
}
