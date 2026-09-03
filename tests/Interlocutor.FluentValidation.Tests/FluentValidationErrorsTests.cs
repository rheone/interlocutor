namespace Interlocutor.FluentValidation.Tests;

using System.Diagnostics;
using System.Reflection;
using global::FluentValidation;
using global::FluentValidation.Results;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, default-value behavior, fidelity, and <see cref="FluentValidationErrors.ToString"/> of
/// <see cref="FluentValidationErrors"/>.
/// </summary>
public class FluentValidationErrorsTests
{
    [Fact]
    public void Constructor_NullFailures_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FluentValidationErrors(null!));
    }

    [Fact]
    public void Constructor_EmptyFailures_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FluentValidationErrors([]));
    }

    [Fact]
    public void Constructor_PreservesEveryFieldOfEveryFailure()
    {
        // Arrange
        var customState = new object();
        var failure = new ValidationFailure("Email", "'Email' is not a valid email address.")
        {
            ErrorCode = "EmailValidator",
            Severity = Severity.Warning,
            CustomState = customState,
            AttemptedValue = "not-an-email",
        };

        // Act
        var errors = new FluentValidationErrors([failure]);

        // Assert - every field FluentValidation attaches to a ValidationFailure round-trips unchanged.
        var roundTripped = Assert.Single(errors.Failures);
        Assert.Equal("Email", roundTripped.PropertyName);
        Assert.Equal("'Email' is not a valid email address.", roundTripped.ErrorMessage);
        Assert.Equal("EmailValidator", roundTripped.ErrorCode);
        Assert.Equal(Severity.Warning, roundTripped.Severity);
        Assert.Same(customState, roundTripped.CustomState);
        Assert.Equal("not-an-email", roundTripped.AttemptedValue);
    }

    [Fact]
    public void Messages_ProjectsErrorMessageFromEveryFailure()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Age", "Age must be non-negative"),
        };

        // Act
        var errors = new FluentValidationErrors(failures);

        // Assert
        Assert.Equal(["Name is required", "Age must be non-negative"], errors.Messages);
    }

    [Fact]
    public void Messages_DoesNotDeduplicate()
    {
        // Arrange - unlike ValidationErrors, FluentValidationErrors wraps every ValidationFailure verbatim,
        // even when two failures happen to carry the same message.
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Name", "Name is required"),
        };

        // Act
        var errors = new FluentValidationErrors(failures);

        // Assert
        Assert.Equal(2, errors.Failures.Count);
        Assert.Equal(["Name is required", "Name is required"], errors.Messages);
    }

    [Fact]
    public void DefaultValue_FailuresIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(FluentValidationErrors);

        // Assert
        Assert.Empty(defaultValue.Failures);
        Assert.Empty(defaultValue.Messages);
        Assert.Empty(defaultValue.FieldMessages);
    }

    [Fact]
    public void SatisfiesIValidationFailureCase()
    {
        // Arrange
        var errors = new FluentValidationErrors([new ValidationFailure("Name", "Name is required")]);

        // Act
        IValidationFailureCase failureCase = errors;

        // Assert
        Assert.Equal(errors.Messages, failureCase.Messages);
    }

    [Fact]
    public void SatisfiesIFieldValidationFailureCase()
    {
        // Arrange
        var errors = new FluentValidationErrors([new ValidationFailure("Name", "Name is required")]);

        // Act
        IFieldValidationFailureCase failureCase = errors;

        // Assert
        Assert.Equal(errors.FieldMessages, failureCase.FieldMessages);
    }

    [Fact]
    public void FieldMessages_PairsPropertyNameWithMessage_InFailureOrder()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Age", "Age must be non-negative"),
        };

        // Act
        var errors = new FluentValidationErrors(failures);

        // Assert
        Assert.Equal(
            [
                new FieldValidationMessage("Name", "Name is required"),
                new FieldValidationMessage("Age", "Age must be non-negative"),
            ],
            errors.FieldMessages
        );
    }

    [Fact]
    public void FieldMessages_EmptyPropertyName_NormalizedToNull()
    {
        // Arrange - FluentValidation populates PropertyName as "" (not null) for a rule with no target
        // property (e.g. RuleFor(x => x)); FieldValidationMessage.PropertyName documents null as that meaning.
        var failure = new ValidationFailure(string.Empty, "Start date must be before end date.");

        // Act
        var errors = new FluentValidationErrors([failure]);

        // Assert
        var fieldMessage = Assert.Single(errors.FieldMessages);
        Assert.Null(fieldMessage.PropertyName);
        Assert.Equal("Start date must be before end date.", fieldMessage.Message);
    }

    [Fact]
    public void ToString_DefaultValue_ReturnsTypeNameOnly()
    {
        // Arrange & Act
        var result = default(FluentValidationErrors).ToString();

        // Assert
        Assert.Equal("FluentValidationErrors", result);
    }

    [Fact]
    public void ToString_WithFailures_IncludesMessages()
    {
        // Arrange
        var errors = new FluentValidationErrors([
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Age", "Age must be non-negative"),
        ]);

        // Act
        var result = errors.ToString();

        // Assert
        Assert.Equal("FluentValidationErrors: Name is required, Age must be non-negative", result);
    }

    [Fact]
    public void Equals_SameFailuresDifferentBackingInstances_AreEqualWithMatchingHashCodes()
    {
        // Arrange - two distinct ValidationFailure instances (not the same reference) with the same content,
        // in two distinct backing lists (see finding 02: the previous compiler-synthesized equality compared
        // the backing list by reference and always failed here).
        var left = new FluentValidationErrors([
            new ValidationFailure("Email", "'Email' is not a valid email address.") { ErrorCode = "EmailValidator" },
        ]);
        var right = new FluentValidationErrors([
            new ValidationFailure("Email", "'Email' is not a valid email address.") { ErrorCode = "EmailValidator" },
        ]);

        // Act & Assert
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_SameFailuresDifferentOrder_AreEqualWithMatchingHashCodes()
    {
        // Arrange - order-independent, the same guarantee the other five aggregate result types have.
        var a = new ValidationFailure("Email", "'Email' is not a valid email address.");
        var b = new ValidationFailure("Name", "Name is required");
        var collectedAThenB = new FluentValidationErrors([a, b]);
        var collectedBThenA = new FluentValidationErrors([b, a]);

        // Act & Assert
        Assert.Equal(collectedAThenB, collectedBThenA);
        Assert.True(collectedAThenB == collectedBThenA);
        Assert.Equal(collectedAThenB.GetHashCode(), collectedBThenA.GetHashCode());
    }

    [Fact]
    public void Equals_DuplicateFailureCounts_AreNotCollapsedLikeASet()
    {
        // Arrange - verbatim, non-deduplicating semantics (unlike the other five aggregate types) must
        // survive the equality fix: [A, A, B] and [A, B] are unequal because A appears a different number
        // of times, not just "the same distinct items".
        var failureA = new ValidationFailure("Email", "'Email' is not a valid email address.");
        var failureB = new ValidationFailure("Name", "Name is required");
        var withDuplicate = new FluentValidationErrors([failureA, failureA, failureB]);
        var withoutDuplicate = new FluentValidationErrors([failureA, failureB]);

        // Act & Assert
        Assert.NotEqual(withDuplicate, withoutDuplicate);
    }

    [Fact]
    public void Equals_DifferentFailures_AreUnequal()
    {
        // Arrange
        var left = new FluentValidationErrors([new ValidationFailure("Email", "'Email' is not a valid email address.")]);
        var right = new FluentValidationErrors([new ValidationFailure("Name", "Name is required")]);

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void Equals_DefaultVsDefault_AreEqual()
    {
        // Arrange & Act & Assert
        Assert.Equal(default(FluentValidationErrors), default(FluentValidationErrors));
    }

    [Fact]
    public void Equals_DefaultVsExplicit_AreUnequal()
    {
        // Arrange
        var explicitValue = new FluentValidationErrors([
            new ValidationFailure("Email", "'Email' is not a valid email address."),
        ]);

        // Act & Assert
        Assert.NotEqual(default, explicitValue);
    }

    [Fact]
    public void BackingField_IsNeverBrowsable()
    {
        // Arrange - hidden from the debugger the same way the other five aggregate result types' composed
        // DistinctItemSet<TItem> field(s) are (ticket 06), even though this field isn't DistinctItemSet-typed.
        var field = typeof(FluentValidationErrors).GetField("_failures", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var attribute = field?.GetCustomAttribute<DebuggerBrowsableAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(DebuggerBrowsableState.Never, attribute.State);
    }
}
