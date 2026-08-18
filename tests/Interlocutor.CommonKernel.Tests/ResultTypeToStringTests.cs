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
        Assert.Equal("Success", default(Success).ToString());
    }

    [Fact]
    public void NotFound_WithNoMessage_ReturnsBareTypeName()
    {
        Assert.Equal("NotFound", default(NotFound).ToString());
    }

    [Fact]
    public void NotFound_WithMessage_IncludesMessage()
    {
        Assert.Equal("NotFound: User 42 was not found", new NotFound("User 42 was not found").ToString());
    }

    [Fact]
    public void NotFoundOfTId_WithNoMessage_IncludesEntityTypeAndId()
    {
        var notFound = new NotFound<int>(typeof(string), 42);
        Assert.Equal("NotFound: String 42", notFound.ToString());
    }

    [Fact]
    public void NotFoundOfTId_WithMessage_IncludesEntityTypeIdAndMessage()
    {
        var notFound = new NotFound<int>(typeof(string), 42, "User 42 was not found");
        Assert.Equal("NotFound: String 42 - User 42 was not found", notFound.ToString());
    }

    [Fact]
    public void Forbidden_WithNoReason_ReturnsBareTypeName()
    {
        Assert.Equal("Forbidden", default(Forbidden).ToString());
    }

    [Fact]
    public void Forbidden_WithReason_IncludesReason()
    {
        Assert.Equal("Forbidden: Insufficient permissions", new Forbidden("Insufficient permissions").ToString());
    }

    [Fact]
    public void Conflict_IncludesBothCodeAndMessage()
    {
        var conflict = new Conflict("Resource already exists", "DUPLICATE_KEY");
        Assert.Equal("Conflict: DUPLICATE_KEY - Resource already exists", conflict.ToString());
    }

    [Fact]
    public void ValidationError_IncludesMessage()
    {
        Assert.Equal("ValidationError: Email is required", new ValidationError("Email is required").ToString());
    }

    [Fact]
    public void ValidationErrorOfTErrorCode_IncludesMessageAndErrorCode()
    {
        var error = new ValidationError<string>("Email is invalid", "email_format");
        Assert.Equal("ValidationError: email_format - Email is invalid", error.ToString());
    }
}
