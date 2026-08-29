namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests that aggregate result types (ValidationErrors, Failures) handle default values safely.
/// When a struct is zero-initialized, its backing array is default, not empty. Accessors must
/// guard against this and return safe inert values.
/// </summary>
public class AggregateResultTypesTests
{
    private static readonly string[] Messages = ["Email is required", "Name is required"];

    [Fact]
    public void ValidationErrors_DefaultValue_CountIsZero()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors);

        // Assert
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void ValidationErrors_DefaultValue_ErrorsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors);

        // Assert
        Assert.Empty(defaultValue.Errors);
    }

    [Fact]
    public void ValidationErrors_DefaultValue_MessagesIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors);

        // Assert
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_CountIsZero()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors<int>);

        // Assert
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_ErrorsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors<int>);

        // Assert
        Assert.Empty(defaultValue.Errors);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_MessagesIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors<int>);

        // Assert
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_ErrorCodesIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(ValidationErrors<int>);

        // Assert
        Assert.Empty(defaultValue.ErrorCodes);
    }

    [Fact]
    public void Failures_DefaultValue_CountIsZero()
    {
        // Arrange & Act
        var defaultValue = default(Failures);

        // Assert
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void Failures_DefaultValue_ItemsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Failures);

        // Assert
        Assert.Empty(defaultValue.Items);
    }

    [Fact]
    public void ValidationErrors_ExplicitEmpty_StillThrows()
    {
        // Arrange & Act
        var ex = Assert.Throws<ArgumentException>(() => new ValidationErrors(Array.Empty<ValidationError>()));

        // Assert
        Assert.Contains("At least one validation error is required", ex.Message);
    }

    [Fact]
    public void ValidationErrors_Generic_ExplicitEmpty_StillThrows()
    {
        // Arrange & Act
        var ex = Assert.Throws<ArgumentException>(() => new ValidationErrors<int>(Array.Empty<ValidationError<int>>()));

        // Assert
        Assert.Contains("At least one validation error is required", ex.Message);
    }

    [Fact]
    public void Failures_ExplicitEmpty_StillThrows()
    {
        // Arrange & Act
        var ex = Assert.Throws<ArgumentException>(() => new Failures(Array.Empty<Failure>()));

        // Assert
        Assert.Contains("At least one failure is required", ex.Message);
    }

    [Fact]
    public void Conflicts_DefaultValue_CountIsZero()
    {
        // Arrange & Act
        var defaultValue = default(Conflicts);

        // Assert
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void Conflicts_DefaultValue_ItemsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Conflicts);

        // Assert
        Assert.Empty(defaultValue.Items);
    }

    [Fact]
    public void Conflicts_DefaultValue_MessagesIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Conflicts);

        // Assert
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void Conflicts_ExplicitEmpty_StillThrows()
    {
        // Arrange & Act
        var ex = Assert.Throws<ArgumentException>(() => new Conflicts(Array.Empty<Conflict>()));

        // Assert
        Assert.Contains("At least one conflict is required", ex.Message);
    }

    [Fact]
    public void Conflicts_SingleItem_IsAccessible()
    {
        // Arrange
        var conflict = new Conflict("Resource already exists", "DUPLICATE_KEY");

        // Act
        var conflicts = new Conflicts(conflict);

        // Assert
        Assert.Equal(1, conflicts.Count);
        var item = Assert.Single(conflicts.Items);
        Assert.Equal(conflict, item);
    }

    [Fact]
    public void Conflicts_MultipleItems_DeduplicatedOrderPreserved()
    {
        // Arrange
        var conflict1 = new Conflict("Concurrency conflict", "CONFLICT_001");
        var conflict2 = new Conflict("Duplicate key", "CONFLICT_002");

        // Act
        var conflicts = new Conflicts(new[] { conflict1, conflict2, conflict1 });

        // Assert
        Assert.Equal(2, conflicts.Count);
        Assert.Equal(conflict1, conflicts.Items[0]);
        Assert.Equal(conflict2, conflicts.Items[1]);
    }

    [Fact]
    public void Conflicts_MessagesProjection_ExtractsDistinctMessages()
    {
        // Arrange
        var conflict1 = new Conflict("State conflict", "STATE_ERROR");
        var conflict2 = new Conflict("Key already exists", "KEY_ERROR");

        // Act
        var conflicts = new Conflicts(new[] { conflict1, conflict2 });

        // Assert
        Assert.Equal(2, conflicts.Messages.Count);
        Assert.Contains("State conflict", conflicts.Messages);
        Assert.Contains("Key already exists", conflicts.Messages);
    }

    /// <summary>
    /// Tests that all plural aggregate types expose a consistent accessor for their collections.
    /// This ensures a uniform API for handling aggregate results.
    /// </summary>
    [Fact]
    public void AllAggregateTypes_ExposePrimaryAccessor_UnifiedNaming()
    {
        // Arrange & Act
        // Verify that each aggregate type exposes the unified accessor pattern.
        // This test documents the unified naming convention: all plurals should have a consistent
        // primary collection accessor.
        var validationErrors = new ValidationErrors("Email is required");
        var failures = new Failures(new Failure("Operation failed"));
        var conflicts = new Conflicts(new Conflict("State conflict", "CONFLICT"));

        // Assert
        // Each type exposes the same canonical primary accessor: .Items. ValidationErrors also
        // exposes .Errors as an alias (a genuinely distinct semantic name for the same data).
        Assert.NotEmpty(validationErrors.Items);
        Assert.NotEmpty(validationErrors.Errors);
        Assert.NotEmpty(failures.Items);
        Assert.NotEmpty(conflicts.Items);
    }

    [Fact]
    public void Failures_SingleItem_IsAccessible()
    {
        // Arrange
        var failure = new Failure("Operation failed");

        // Act
        var failures = new Failures(failure);

        // Assert
        Assert.Equal(1, failures.Count);
        var item = Assert.Single(failures.Items);
        Assert.Equal(failure, item);
    }

    [Fact]
    public void Failures_MultipleItems_DeduplicatedOrderPreserved()
    {
        // Arrange
        var failure1 = new Failure("Timeout");
        var failure2 = new Failure("Disk full");

        // Act
        var failures = new Failures(new[] { failure1, failure2, failure1 });

        // Assert
        Assert.Equal(2, failures.Count);
        Assert.Equal(failure1, failures.Items[0]);
        Assert.Equal(failure2, failures.Items[1]);
    }

    [Fact]
    public void Failures_NullEnumerable_Throws()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Failures(null!));
    }

    [Fact]
    public void Failures_DefaultValue_ReasonsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Failures);

        // Assert
        Assert.Empty(defaultValue.Reasons);
    }

    [Fact]
    public void Failures_ReasonsProjection_ExtractsDistinctNonNullReasons()
    {
        // Arrange & Act
        var failures = new Failures(
            new[] { new Failure("Timeout"), new Failure(Reason: null), new Failure("Disk full"), new Failure("Timeout") }
        );

        // Assert
        Assert.Equal(2, failures.Reasons.Count);
        Assert.Contains("Timeout", failures.Reasons);
        Assert.Contains("Disk full", failures.Reasons);
    }

    [Fact]
    public void Conflicts_NullEnumerable_Throws()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Conflicts(null!));
    }

    [Fact]
    public void ValidationErrors_NullEnumerable_Throws()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationErrors((IEnumerable<ValidationError>)null!));
    }

    [Fact]
    public void ValidationErrors_Generic_MultipleItems_DeduplicatedOrderPreserved()
    {
        // Arrange
        var error1 = new ValidationError<int>("Email is required", 1001);
        var error2 = new ValidationError<int>("Name is required", 1002);

        // Act
        var errors = new ValidationErrors<int>(new[] { error1, error2, error1 });

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Equal(error1, errors.Items[0]);
        Assert.Equal(error2, errors.Items[1]);
    }

    [Fact]
    public void ValidationErrors_Generic_UniqueErrorCodeCount_CountsDistinctCodesAcrossDuplicateMessages()
    {
        // Arrange & Act
        var errors = new ValidationErrors<int>(
            new[] { new ValidationError<int>("Email is required", 1001), new ValidationError<int>("Email is required", 1002) }
        );

        // Assert
        // Same message, different codes: two distinct (message, code) pairs, one unique message,
        // two unique codes.
        Assert.Equal(2, errors.Count);
        Assert.Equal(1, errors.UniqueErrorCount);
        Assert.Equal(2, errors.UniqueErrorCodeCount);
        Assert.Equal(2, errors.ErrorCodes.Count);
    }

    [Fact]
    public void ValidationErrors_Generic_NullEnumerable_Throws()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationErrors<int>((IEnumerable<ValidationError<int>>)null!));
    }

    [Fact]
    public void Failures_Generic_DefaultValue_CountIsZero()
    {
        // Arrange & Act
        var defaultValue = default(Failures<int>);

        // Assert
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void Failures_Generic_DefaultValue_ItemsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Failures<int>);

        // Assert
        Assert.Empty(defaultValue.Items);
    }

    [Fact]
    public void Failures_Generic_DefaultValue_ReasonsIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Failures<int>);

        // Assert
        Assert.Empty(defaultValue.Reasons);
    }

    [Fact]
    public void Failures_Generic_DefaultValue_ErrorCodesIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(Failures<int>);

        // Assert
        Assert.Empty(defaultValue.ErrorCodes);
    }

    [Fact]
    public void Failures_Generic_ExplicitEmpty_StillThrows()
    {
        // Arrange & Act
        var ex = Assert.Throws<ArgumentException>(() => new Failures<int>(Array.Empty<Failure<int>>()));

        // Assert
        Assert.Contains("At least one failure is required", ex.Message);
    }

    [Fact]
    public void Failures_Generic_NullEnumerable_Throws()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Failures<int>((IEnumerable<Failure<int>>)null!));
    }

    [Fact]
    public void Failures_Generic_SingleItem_IsAccessible()
    {
        // Arrange
        var failure = new Failure<int>(1001, "Timeout");

        // Act
        var failures = new Failures<int>(failure);

        // Assert
        Assert.Equal(1, failures.Count);
        var item = Assert.Single(failures.Items);
        Assert.Equal(failure, item);
    }

    [Fact]
    public void Failures_Generic_MultipleItems_DeduplicatedOrderPreserved()
    {
        // Arrange
        var failure1 = new Failure<int>(1001, "Timeout");
        var failure2 = new Failure<int>(1002, "Disk full");

        // Act
        var failures = new Failures<int>(new[] { failure1, failure2, failure1 });

        // Assert
        Assert.Equal(2, failures.Count);
        Assert.Equal(failure1, failures.Items[0]);
        Assert.Equal(failure2, failures.Items[1]);
    }

    [Fact]
    public void Failures_Generic_ReasonsProjection_ExtractsDistinctNonNullReasons()
    {
        // Arrange & Act
        var failures = new Failures<int>(
            new[] { new Failure<int>(1001, "Timeout"), new Failure<int>(1002), new Failure<int>(1003, "Timeout") }
        );

        // Assert
        Assert.Single(failures.Reasons);
        Assert.Contains("Timeout", failures.Reasons);
    }

    [Fact]
    public void Failures_Generic_ErrorCodesProjection_ExtractsDistinctCodesAcrossDuplicateReasons()
    {
        // Arrange & Act
        var failures = new Failures<int>(
            new[] { new Failure<int>(1001, "Email is required"), new Failure<int>(1002, "Email is required") }
        );

        // Assert
        Assert.Equal(2, failures.Count);
        Assert.Equal(2, failures.ErrorCodes.Count);
        Assert.Contains(1001, failures.ErrorCodes);
        Assert.Contains(1002, failures.ErrorCodes);
    }

    [Fact]
    public void Conflicts_DefaultValue_ToString_ReturnsBareTypeName()
    {
        // Arrange & Act & Assert
        Assert.Equal("Conflicts", default(Conflicts).ToString());
    }

    [Fact]
    public void Conflicts_NonEmpty_ToString_JoinsMessages()
    {
        // Arrange
        var conflicts = new Conflicts(
            new[] { new Conflict("State conflict", "STATE_ERROR"), new Conflict("Key already exists", "KEY_ERROR") }
        );

        // Act
        var result = conflicts.ToString();

        // Assert
        Assert.Equal("Conflicts: State conflict, Key already exists", result);
    }

    [Fact]
    public void ValidationErrors_DefaultValue_ToString_ReturnsBareTypeName()
    {
        // Arrange & Act & Assert
        Assert.Equal("ValidationErrors", default(ValidationErrors).ToString());
    }

    [Fact]
    public void ValidationErrors_NonEmpty_ToString_JoinsMessages()
    {
        // Arrange
        var errors = new ValidationErrors(Messages);

        // Act
        var result = errors.ToString();

        // Assert
        Assert.Equal("ValidationErrors: Email is required, Name is required", result);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_ToString_ReturnsBareTypeName()
    {
        // Arrange & Act & Assert
        Assert.Equal("ValidationErrors", default(ValidationErrors<int>).ToString());
    }

    [Fact]
    public void ValidationErrors_Generic_NonEmpty_ToString_JoinsMessages()
    {
        // Arrange
        var errors = new ValidationErrors<int>(
            new[] { new ValidationError<int>("Email is required", 1001), new ValidationError<int>("Name is required", 1002) }
        );

        // Act
        var result = errors.ToString();

        // Assert
        Assert.Equal("ValidationErrors: Email is required, Name is required", result);
    }

    [Fact]
    public void Failures_DefaultValue_ToString_ReturnsBareTypeName()
    {
        // Arrange & Act & Assert
        Assert.Equal("Failures", default(Failures).ToString());
    }

    [Fact]
    public void Failures_NonEmpty_ToString_JoinsReasons()
    {
        // Arrange
        var failures = new Failures(new[] { new Failure("Timeout"), new Failure("Disk full") });

        // Act
        var result = failures.ToString();

        // Assert
        Assert.Equal("Failures: Timeout, Disk full", result);
    }

    [Fact]
    public void Failures_NonEmpty_AllReasonsNull_ToString_ReturnsBareTypeName()
    {
        // Arrange
        // Count > 0 but Reasons is empty (nothing to display) - falls back to the bare type name
        // rather than emitting "Failures: " with nothing after the colon.
        var failures = new Failures(default(Failure));

        // Act
        var result = failures.ToString();

        // Assert
        Assert.Equal("Failures", result);
    }

    [Fact]
    public void Failures_Generic_DefaultValue_ToString_ReturnsBareTypeName()
    {
        // Arrange & Act & Assert
        Assert.Equal("Failures", default(Failures<int>).ToString());
    }

    [Fact]
    public void Failures_Generic_NonEmpty_ToString_JoinsReasons()
    {
        // Arrange
        var failures = new Failures<int>(new[] { new Failure<int>(1001, "Timeout"), new Failure<int>(1002, "Disk full") });

        // Act
        var result = failures.ToString();

        // Assert
        Assert.Equal("Failures: Timeout, Disk full", result);
    }

    [Fact]
    public void Failures_Generic_NonEmpty_AllReasonsNull_ToString_ReturnsBareTypeName()
    {
        // Arrange
        var failures = new Failures<int>(new[] { new Failure<int>(1001), new Failure<int>(1002) });

        // Act
        var result = failures.ToString();

        // Assert
        Assert.Equal("Failures", result);
    }
}
