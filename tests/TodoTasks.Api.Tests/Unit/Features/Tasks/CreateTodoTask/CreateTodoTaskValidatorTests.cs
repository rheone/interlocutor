namespace TodoTasks.Api.Tests.Unit.Features.Tasks.CreateTodoTask;

using TodoTasks.Api.Features.Tasks.CreateTodoTask;

/// <summary>Unit tests for <see cref="CreateTodoTaskValidator"/>'s rules, independent of the handler.</summary>
public sealed class CreateTodoTaskValidatorTests
{
    private readonly CreateTodoTaskValidator validator = new();

    [Fact]
    public void Validate_WellFormedCommand_HasNoErrors_Test()
    {
        // Arrange
        var command = new CreateTodoTaskCommand("Buy milk", "2%", "owner-1");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WellFormedCommand_WithNoNotes_HasNoErrors_Test()
    {
        // Arrange
        var command = new CreateTodoTaskCommand("Buy milk", null, "owner-1");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyTitle_HasAnError_Test(string title)
    {
        // Arrange
        var command = new CreateTodoTaskCommand(title, null, "owner-1");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTodoTaskCommand.Title));
    }

    [Fact]
    public void Validate_TitleOverMaximumLength_HasAnError_Test()
    {
        // Arrange
        var command = new CreateTodoTaskCommand(new string('a', 201), null, "owner-1");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTodoTaskCommand.Title));
    }

    [Fact]
    public void Validate_NotesOverMaximumLength_HasAnError_Test()
    {
        // Arrange
        var command = new CreateTodoTaskCommand("Buy milk", new string('a', 2001), "owner-1");

        // Act
        var result = this.validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTodoTaskCommand.Notes));
    }
}
