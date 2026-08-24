namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests the uniform <c>ToString()</c> convention across the single-value ResultTypes case types:
/// <c>"TypeName: detail"</c> when a detail is present, bare <c>"TypeName"</c> otherwise.
/// </summary>
public class ResultTypeToStringTests
{
    [Fact]
    public void Success_ToString_ReturnsBareTypeName()
    {
        // Arrange
        var success = default(Success);

        // Act
        var result = success.ToString();

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public void NotFound_WithNoMessage_ReturnsBareTypeName()
    {
        // Arrange
        var notFound = default(NotFound);

        // Act
        var result = notFound.ToString();

        // Assert
        Assert.Equal("NotFound", result);
    }

    [Fact]
    public void NotFound_WithMessage_IncludesMessage()
    {
        // Arrange
        var notFound = new NotFound("User 42 was not found");

        // Act
        var result = notFound.ToString();

        // Assert
        Assert.Equal("NotFound: User 42 was not found", result);
    }

    [Fact]
    public void NotFoundOfTId_WithNoMessage_IncludesEntityTypeAndId()
    {
        // Arrange
        var notFound = new NotFound<int>(typeof(string), 42);

        // Act
        var result = notFound.ToString();

        // Assert
        Assert.Equal("NotFound: String 42", result);
    }

    [Fact]
    public void NotFoundOfTId_WithMessage_IncludesEntityTypeIdAndMessage()
    {
        // Arrange
        var notFound = new NotFound<int>(typeof(string), 42, "User 42 was not found");

        // Act
        var result = notFound.ToString();

        // Assert
        Assert.Equal("NotFound: String 42 - User 42 was not found", result);
    }

    [Fact]
    public void Forbidden_WithNoReason_ReturnsBareTypeName()
    {
        // Arrange
        var forbidden = default(Forbidden);

        // Act
        var result = forbidden.ToString();

        // Assert
        Assert.Equal("Forbidden", result);
    }

    [Fact]
    public void Forbidden_WithReason_IncludesReason()
    {
        // Arrange
        var forbidden = new Forbidden("Insufficient permissions");

        // Act
        var result = forbidden.ToString();

        // Assert
        Assert.Equal("Forbidden: Insufficient permissions", result);
    }

    [Fact]
    public void Conflict_IncludesBothCodeAndMessage()
    {
        // Arrange
        var conflict = new Conflict("Resource already exists", "DUPLICATE_KEY");

        // Act
        var result = conflict.ToString();

        // Assert
        Assert.Equal("Conflict: DUPLICATE_KEY - Resource already exists", result);
    }

    [Fact]
    public void ValidationError_IncludesMessage()
    {
        // Arrange
        var error = new ValidationError("Email is required");

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("ValidationError: Email is required", result);
    }

    [Fact]
    public void ValidationErrorOfTErrorCode_IncludesMessageAndErrorCode()
    {
        // Arrange
        var error = new ValidationError<string>("Email is invalid", "email_format");

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("ValidationError: email_format - Email is invalid", result);
    }
}
