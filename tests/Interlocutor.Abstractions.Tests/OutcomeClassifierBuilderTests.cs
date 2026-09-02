namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="OutcomeClassifierBuilder"/> and the <see cref="IOutcomeClassifier"/> it builds.</summary>
public class OutcomeClassifierBuilderTests
{
    // Auto Generated, verify expected behavior:
    [Theory]
    [InlineData(typeof(Success), false)]
    [InlineData(typeof(NotFound), true)]
    [InlineData(typeof(Forbidden), true)]
    [InlineData(typeof(Conflict), true)]
    [InlineData(typeof(Failure), true)]
    [InlineData(typeof(Cancelled), true)]
    [InlineData(typeof(Timeout), true)]
    [InlineData(typeof(RateLimited), true)]
    public void DefaultClassifier_KernelCaseTypes_ClassifiesEachAsExpected(Type caseType, bool expectedIsFailure)
    {
        // Arrange
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        var value = Activator.CreateInstance(caseType);

        // Act
        var isFailure = classifier.IsFailure(value);

        // Assert
        Assert.Equal(expectedIsFailure, isFailure);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void DefaultClassifier_IsASingletonInstance()
    {
        // Arrange & Act & Assert - lazily built once behind a lock, shared across every caller that doesn't
        // supply its own custom classifier.
        Assert.Same(OutcomeClassifierBuilder.DefaultClassifier, OutcomeClassifierBuilder.DefaultClassifier);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_NullResponse_ReturnsNone()
    {
        // Arrange
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;

        // Act
        var label = classifier.GetLabel(null);

        // Assert
        Assert.Equal("None", label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_RegisteredType_ReturnsItsRegisteredLabel()
    {
        // Arrange
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;

        // Act
        var label = classifier.GetLabel(new NotFound("missing"));

        // Assert
        Assert.Equal("NotFound", label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_UnregisteredType_FallsBackToRuntimeTypeName()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder().Build();

        // Act
        var label = classifier.GetLabel("unregistered string value");

        // Assert
        Assert.Equal("String", label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void IsFailure_UnregisteredType_FailsClosed()
    {
        // Arrange - fail-closed policy: consumers who forget to register a custom type see immediate loud
        // rollbacks rather than silently committed writes.
        var classifier = new OutcomeClassifierBuilder().Build();

        // Act
        var isFailure = classifier.IsFailure("unregistered string value");

        // Assert
        Assert.True(isFailure);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void IsFailure_GenericValidationErrorVariant_ClassifiedAsFailureViaPredicate()
    {
        // Arrange - covers the seeded predicate that matches any type whose name starts with
        // "ValidationError", including generic variants the builder cannot enumerate by closed type.
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;

        // Act
        var isFailure = classifier.IsFailure(new ValidationErrors<string>([("required", "code")]));

        // Assert
        Assert.True(isFailure);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterSuccessType_CustomType_ClassifiedAsSuccessWithGivenLabel()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder().RegisterSuccessType<CustomFailure>("CustomOk").Build();
        var value = new CustomFailure("n/a");

        // Act & Assert
        Assert.False(classifier.IsFailure(value));
        Assert.Equal("CustomOk", classifier.GetLabel(value));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterFailureType_Generic_ClassifiedAsFailureWithGivenLabel()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder().RegisterFailureType<CustomFailure>("CustomFail").Build();
        var value = new CustomFailure("boom");

        // Act & Assert
        Assert.True(classifier.IsFailure(value));
        Assert.Equal("CustomFail", classifier.GetLabel(value));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterFailureType_ByRuntimeType_ClassifiedAsFailureWithGivenLabel()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder().RegisterFailureType(typeof(CustomFailure), "CustomFail").Build();
        var value = new CustomFailure("boom");

        // Act & Assert
        Assert.True(classifier.IsFailure(value));
        Assert.Equal("CustomFail", classifier.GetLabel(value));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterFailureType_ByRuntimeType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.RegisterFailureType(null!, "label"));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterPredicate_MatchingResponse_ClassifiedPerThePredicate()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder()
            .RegisterPredicate(
                response => response is string s && s.StartsWith("err-", StringComparison.Ordinal),
                "Custom",
                isFailure: true
            )
            .Build();

        // Act & Assert
        Assert.True(classifier.IsFailure("err-boom"));
        Assert.Equal("Custom", classifier.GetLabel("err-boom"));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterPredicate_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.RegisterPredicate(null!, "label", isFailure: true));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterPredicate_NullLabel_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.RegisterPredicate(_ => true, null!, isFailure: true));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterSuccessType_ReturnsTheSameBuilderForChaining()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act
        var result = builder.RegisterSuccessType<CustomFailure>("label");

        // Assert
        Assert.Same(builder, result);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void IsFailure_CompilerGeneratedUnion_UnwrapsToTheActiveCaseBeforeClassifying()
    {
        // Arrange - Outcome<TSuccess, TFailure> implements IUnion; the classifier must unwrap it to
        // the active case value (a Failure here) rather than classifying the union wrapper's own type.
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        Outcome<string, Failure> response = new Failure("boom");

        // Act
        var isFailure = classifier.IsFailure(response);

        // Assert
        Assert.True(isFailure);
    }

    private sealed record CustomFailure(string Reason);
}
