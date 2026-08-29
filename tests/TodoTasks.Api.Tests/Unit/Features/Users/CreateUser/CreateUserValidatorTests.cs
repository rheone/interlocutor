namespace TodoTasks.Api.Tests.Unit.Features.Users.CreateUser;

using TodoTasks.Api.Features.Users.CreateUser;

/// <summary>Unit tests for <see cref="CreateUserValidator"/>'s rules, independent of the handler.</summary>
public sealed class CreateUserValidatorTests
{
    private readonly CreateUserValidator validator = new();

    [Fact]
    public void Validate_WellFormedCommand_HasNoErrors_Test()
    {
        // Arrange
        var command = new CreateUserCommand("Ada Lovelace", "ada@example.com");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyDisplayName_HasAnError_Test(string displayName)
    {
        // Arrange
        var command = new CreateUserCommand(displayName, "ada@example.com");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.DisplayName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_HasAnError_Test(string email)
    {
        // Arrange
        var command = new CreateUserCommand("Ada Lovelace", email);

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void Validate_DisplayNameOverMaximumLength_HasAnError_Test()
    {
        // Arrange
        var command = new CreateUserCommand(new string('a', 201), "ada@example.com");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.DisplayName));
    }
}
