namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests that aggregate result types (ValidationErrors, Failures) handle default values safely.
/// When a struct is zero-initialized, its backing array is default, not empty. Accessors must
/// guard against this and return safe inert values.
/// </summary>
public class AggregateResultTypesTests
{
    [Fact]
    public void ValidationErrors_DefaultValue_CountIsZero()
    {
        var defaultValue = default(ValidationErrors);
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void ValidationErrors_DefaultValue_ErrorsIsEmpty()
    {
        var defaultValue = default(ValidationErrors);
        Assert.Empty(defaultValue.Errors);
    }

    [Fact]
    public void ValidationErrors_DefaultValue_MessagesIsEmpty()
    {
        var defaultValue = default(ValidationErrors);
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_CountIsZero()
    {
        var defaultValue = default(ValidationErrors<int>);
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_ErrorsIsEmpty()
    {
        var defaultValue = default(ValidationErrors<int>);
        Assert.Empty(defaultValue.Errors);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_MessagesIsEmpty()
    {
        var defaultValue = default(ValidationErrors<int>);
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void ValidationErrors_Generic_DefaultValue_ErrorCodesIsEmpty()
    {
        var defaultValue = default(ValidationErrors<int>);
        Assert.Empty(defaultValue.ErrorCodes);
    }

    [Fact]
    public void Failures_DefaultValue_CountIsZero()
    {
        var defaultValue = default(Failures);
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void Failures_DefaultValue_ItemsIsEmpty()
    {
        var defaultValue = default(Failures);
        Assert.Empty(defaultValue.Items);
    }

    [Fact]
    public void ValidationErrors_ExplicitEmpty_StillThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ValidationErrors(Array.Empty<ValidationError>()));
        Assert.Contains("At least one validation error is required", ex.Message);
    }

    [Fact]
    public void ValidationErrors_Generic_ExplicitEmpty_StillThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ValidationErrors<int>(Array.Empty<ValidationError<int>>()));
        Assert.Contains("At least one validation error is required", ex.Message);
    }

    [Fact]
    public void Failures_ExplicitEmpty_StillThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Failures(Array.Empty<Failure>()));
        Assert.Contains("At least one failure is required", ex.Message);
    }

    [Fact]
    public void Conflicts_DefaultValue_CountIsZero()
    {
        var defaultValue = default(Conflicts);
        Assert.Equal(0, defaultValue.Count);
    }

    [Fact]
    public void Conflicts_DefaultValue_ItemsIsEmpty()
    {
        var defaultValue = default(Conflicts);
        Assert.Empty(defaultValue.Items);
    }

    [Fact]
    public void Conflicts_DefaultValue_MessagesIsEmpty()
    {
        var defaultValue = default(Conflicts);
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void Conflicts_ExplicitEmpty_StillThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Conflicts(Array.Empty<Conflict>()));
        Assert.Contains("At least one conflict is required", ex.Message);
    }

    [Fact]
    public void Conflicts_SingleItem_IsAccessible()
    {
        var conflict = new Conflict("Resource already exists", "DUPLICATE_KEY");
        var conflicts = new Conflicts(conflict);

        Assert.Equal(1, conflicts.Count);
        Assert.Single(conflicts.Items);
        Assert.Equal(conflict, conflicts.Items[0]);
    }

    [Fact]
    public void Conflicts_MultipleItems_DeduplicatedOrderPreserved()
    {
        var conflict1 = new Conflict("Concurrency conflict", "CONFLICT_001");
        var conflict2 = new Conflict("Duplicate key", "CONFLICT_002");

        var conflicts = new Conflicts(new[] { conflict1, conflict2, conflict1 });

        Assert.Equal(2, conflicts.Count);
        Assert.Equal(conflict1, conflicts.Items[0]);
        Assert.Equal(conflict2, conflicts.Items[1]);
    }

    [Fact]
    public void Conflicts_MessagesProjection_ExtractsDistinctMessages()
    {
        var conflict1 = new Conflict("State conflict", "STATE_ERROR");
        var conflict2 = new Conflict("Key already exists", "KEY_ERROR");

        var conflicts = new Conflicts(new[] { conflict1, conflict2 });

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
        // Verify that each aggregate type exposes the unified accessor pattern.
        // This test documents the unified naming convention: all plurals should have a consistent
        // primary collection accessor.

        var validationErrors = new ValidationErrors("Email is required");
        var failures = new Failures(new Failure("Operation failed"));
        var conflicts = new Conflicts(new Conflict("State conflict", "CONFLICT"));

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
        var failure = new Failure("Operation failed");
        var failures = new Failures(failure);

        Assert.Equal(1, failures.Count);
        Assert.Single(failures.Items);
        Assert.Equal(failure, failures.Items[0]);
    }

    [Fact]
    public void Failures_MultipleItems_DeduplicatedOrderPreserved()
    {
        var failure1 = new Failure("Timeout");
        var failure2 = new Failure("Disk full");

        var failures = new Failures(new[] { failure1, failure2, failure1 });

        Assert.Equal(2, failures.Count);
        Assert.Equal(failure1, failures.Items[0]);
        Assert.Equal(failure2, failures.Items[1]);
    }

    [Fact]
    public void Failures_NullEnumerable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Failures(null!));
    }

    [Fact]
    public void Conflicts_NullEnumerable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Conflicts(null!));
    }

    [Fact]
    public void ValidationErrors_NullEnumerable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ValidationErrors((IEnumerable<ValidationError>)null!));
    }

    [Fact]
    public void ValidationErrors_Generic_MultipleItems_DeduplicatedOrderPreserved()
    {
        var error1 = new ValidationError<int>("Email is required", 1001);
        var error2 = new ValidationError<int>("Name is required", 1002);

        var errors = new ValidationErrors<int>(new[] { error1, error2, error1 });

        Assert.Equal(2, errors.Count);
        Assert.Equal(error1, errors.Items[0]);
        Assert.Equal(error2, errors.Items[1]);
    }

    [Fact]
    public void ValidationErrors_Generic_UniqueErrorCodeCount_CountsDistinctCodesAcrossDuplicateMessages()
    {
        var errors = new ValidationErrors<int>(
            new[] { new ValidationError<int>("Email is required", 1001), new ValidationError<int>("Email is required", 1002) }
        );

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
        Assert.Throws<ArgumentNullException>(() => new ValidationErrors<int>((IEnumerable<ValidationError<int>>)null!));
    }
}
