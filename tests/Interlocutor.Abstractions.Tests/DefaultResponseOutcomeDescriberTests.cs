namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="DefaultResponseOutcomeDescriber"/>.</summary>
public class DefaultResponseOutcomeDescriberTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_NullClassifier_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultResponseOutcomeDescriber(null!));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void Describe_DelegatesToTheInjectedClassifiersGetLabel()
    {
        // Arrange
        var describer = new DefaultResponseOutcomeDescriber(OutcomeClassifierBuilder.DefaultClassifier);

        // Act
        var label = describer.Describe(new NotFound("missing"));

        // Assert
        Assert.Equal("NotFound", label);
    }
}
