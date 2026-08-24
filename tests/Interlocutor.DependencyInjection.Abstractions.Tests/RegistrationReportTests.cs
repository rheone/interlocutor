namespace Interlocutor.DependencyInjection.Abstractions.Tests;

// Auto Generated, verify expected behavior:
// RegistrationReport is exercised indirectly through InterlocutorBuilderTests (DescribeRegistrations,
// DescribeRegistrations_Format_ProducesNonEmptyReadableText) but had no dedicated test file, leaving its own
// constructor null-guards, ToString()/Format() delegation, empty-section omission, and open-generic type
// display untested in isolation from the builder.
public class RegistrationReportTests
{
    [Fact]
    public void Constructor_WithNullRegistrations_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => new RegistrationReport(null!, [], [], []));

        // Assert
        Assert.Equal("registrations", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullStageFactories_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => new RegistrationReport([], null!, [], []));

        // Assert
        Assert.Equal("stageFactories", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSkipped_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => new RegistrationReport([], [], null!, []));

        // Assert
        Assert.Equal("skipped", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullProblems_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => new RegistrationReport([], [], [], null!));

        // Assert
        Assert.Equal("problems", ex.ParamName);
    }

    [Fact]
    public void ToString_DelegatesToFormat()
    {
        // Arrange
        var report = new RegistrationReport(
            [
                new InterlocutorRegistration(
                    typeof(SampleService),
                    typeof(SampleImplementation),
                    RegistrationLifetime.Scoped,
                    RegistrationKind.Handler
                ),
            ],
            [],
            [],
            []
        );

        // Act & Assert
        Assert.Equal(report.Format(), report.ToString());
    }

    [Fact]
    public void Format_WithNoStageFactoriesSkippedOrProblems_OmitsThoseSections()
    {
        // Arrange
        var report = new RegistrationReport(
            [
                new InterlocutorRegistration(
                    typeof(SampleService),
                    typeof(SampleImplementation),
                    RegistrationLifetime.Scoped,
                    RegistrationKind.Handler
                ),
            ],
            [],
            [],
            []
        );

        // Act
        var text = report.Format();

        // Assert
        Assert.Contains("Registrations (1):", text);
        Assert.DoesNotContain("Delegate-backed stages", text);
        Assert.DoesNotContain("Skipped", text);
        Assert.DoesNotContain("Problems", text);
    }

    [Fact]
    public void Format_RendersOpenGenericServiceAndImplementationTypes_WithArityMarker()
    {
        // Arrange
        var report = new RegistrationReport(
            [
                new InterlocutorRegistration(
                    typeof(ISampleOpenGenericService<,>),
                    typeof(SampleOpenGenericImplementation<,>),
                    RegistrationLifetime.Transient,
                    RegistrationKind.Behavior
                ),
            ],
            [],
            [],
            []
        );

        // Act
        var text = report.Format();

        // Assert
        Assert.Contains("ISampleOpenGenericService<,>", text);
        Assert.Contains("SampleOpenGenericImplementation<,>", text);
    }

    [Fact]
    public void Format_IncludesMatchedByRule_WhenPresent()
    {
        // Arrange
        var report = new RegistrationReport(
            [
                new InterlocutorRegistration(
                    typeof(SampleService),
                    typeof(SampleImplementation),
                    RegistrationLifetime.Scoped,
                    RegistrationKind.Handler,
                    RegistrationProvenance.Matched,
                    "my-rule"
                ),
            ],
            [],
            [],
            []
        );

        // Act
        var text = report.Format();

        // Assert
        Assert.Contains("Matched by my-rule", text);
    }

    private interface ISampleOpenGenericService<T1, T2>;

    private sealed class SampleService;

    private sealed class SampleImplementation;

    private sealed class SampleOpenGenericImplementation<T1, T2> : ISampleOpenGenericService<T1, T2>;
}
