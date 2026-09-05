namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="OutcomeClassifierBuilder"/> and the <see cref="IOutcomeClassifier"/> it builds.</summary>
public class OutcomeClassifierBuilderTests
{
    // Auto Generated, verify expected behavior:
    [Theory]
    [InlineData(typeof(Success), "Success")]
    [InlineData(typeof(NotFound), "NotFound")]
    [InlineData(typeof(Forbidden), "Forbidden")]
    [InlineData(typeof(Conflict), "Conflict")]
    [InlineData(typeof(Failure), "Failure")]
    [InlineData(typeof(Cancelled), "Cancelled")]
    [InlineData(typeof(Timeout), "Timeout")]
    [InlineData(typeof(RateLimited), "RateLimited")]
    public void DefaultClassifier_KernelCaseTypes_ReturnsEachTypesRegisteredLabel(Type caseType, string expectedLabel)
    {
        // Arrange
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        var value = Activator.CreateInstance(caseType);

        // Act
        var label = classifier.GetLabel(value);

        // Assert
        Assert.Equal(expectedLabel, label);
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
    public void GetLabel_GenericValidationErrorVariant_ReturnsItsPredicateLabel()
    {
        // Arrange - covers the seeded predicate that matches any type whose name starts with
        // "ValidationError", including generic variants the builder cannot enumerate by closed type.
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;

        // Act
        var label = classifier.GetLabel(new ValidationErrors<string>([("required", "code")]));

        // Assert
        Assert.Equal("ValidationError", label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterType_Generic_ReturnsGivenLabel()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder().RegisterType<CustomCase>("CustomLabel").Build();
        var value = new CustomCase("n/a");

        // Act & Assert
        Assert.Equal("CustomLabel", classifier.GetLabel(value));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterType_ByRuntimeType_ReturnsGivenLabel()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder().RegisterType(typeof(CustomCase), "CustomLabel").Build();
        var value = new CustomCase("boom");

        // Act & Assert
        Assert.Equal("CustomLabel", classifier.GetLabel(value));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterType_ByRuntimeType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.RegisterType(null!, "label"));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterPredicate_MatchingResponse_ReturnsRegisteredLabel()
    {
        // Arrange
        var classifier = new OutcomeClassifierBuilder()
            .RegisterPredicate(response => response is string s && s.StartsWith("err-", StringComparison.Ordinal), "Custom")
            .Build();

        // Act & Assert
        Assert.Equal("Custom", classifier.GetLabel("err-boom"));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterPredicate_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.RegisterPredicate(null!, "label"));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterPredicate_NullLabel_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.RegisterPredicate(_ => true, null!));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void RegisterType_ReturnsTheSameBuilderForChaining()
    {
        // Arrange
        var builder = new OutcomeClassifierBuilder();

        // Act
        var result = builder.RegisterType<CustomCase>("label");

        // Assert
        Assert.Same(builder, result);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_CompilerGeneratedUnion_UnwrapsToTheActiveCaseBeforeLabeling()
    {
        // Arrange - Outcome<TSuccess, TFailure> implements IUnion; the classifier must unwrap it to
        // the active case value (a Failure here) rather than labeling the union wrapper's own type.
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        Outcome<string, Failure> response = new Failure("boom");

        // Act
        var label = classifier.GetLabel(response);

        // Assert
        Assert.Equal("Failure", label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_UnregisteredCustomTypeInUnion_FallsBackToRuntimeTypeName()
    {
        // Arrange - a custom case payload type nobody registered still labels via the runtime-type-name
        // fallback, same as for a non-union response.
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        Outcome<CustomCase, NotFound> response = new CustomCase("unregistered payload type");

        // Act
        var label = classifier.GetLabel(response);

        // Assert
        Assert.Equal(nameof(CustomCase), label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_NullPayloadSuccess_UsesTheActiveCaseTypeNameInsteadOfNone()
    {
        // Arrange
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        Outcome<string?, NotFound> response = (string?)null;

        // Act
        var label = classifier.GetLabel(response);

        // Assert
        Assert.Equal(nameof(String), label);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetLabel_GenuinelyUnsetUnion_ReturnsNone()
    {
        // Arrange - neither case type of Outcome<Success, Failure> can ever be null, so a default (never
        // constructed) union has no active case type to fall back to either, and labels as "None" - the
        // same as any other genuinely null response.
        var classifier = OutcomeClassifierBuilder.DefaultClassifier;
        var response = default(Outcome<Success, Failure>);

        // Act
        var label = classifier.GetLabel(response);

        // Assert
        Assert.Equal("None", label);
    }

    private sealed record CustomCase(string Reason);
}
