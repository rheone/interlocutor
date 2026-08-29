namespace Interlocutor.DependencyInjection.Abstractions.Tests;

// Auto Generated, verify expected behavior:
// RegistrationReport is exercised indirectly through InterlocutorBuilderTests (DescribeRegistrations,
// DescribeRegistrations_Format_ProducesNonEmptyReadableText) but had no dedicated test file, leaving its own
// constructor null-guards, ToString()/Format() delegation, empty-section omission, and open-generic type
// display untested in isolation from the builder.
public class RegistrationReportTests
{
    // T1/T2 are deliberately left unconstrained by any member - this interface exists purely to exercise
    // RegistrationReport's open-generic type-display formatting, not to define a real contract.
#pragma warning disable S2326
    private interface ISampleOpenGenericService<T1, T2>;
#pragma warning restore S2326

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
    public void Constructor_WithNoTypeLoadFailuresArgument_DefaultsToEmpty()
    {
        // Act
        var report = new RegistrationReport([], [], [], []);

        // Assert
        Assert.Empty(report.TypeLoadFailures);
    }

    [Fact]
    public void Format_IncludesTypeLoadFailures_WhenPresent()
    {
        // Arrange
        var report = new RegistrationReport(
            [],
            [],
            [],
            [],
            [new TypeLoadFailure("SomeAssembly", "Could not load type 'Foo'.")]
        );

        // Act
        var text = report.Format();

        // Assert
        Assert.Contains("Type load failures (1):", text);
        Assert.Contains("SomeAssembly", text);
        Assert.Contains("Could not load type 'Foo'.", text);
    }

    [Fact]
    public void Format_WithNoTypeLoadFailures_OmitsThatSection()
    {
        // Arrange
        var report = new RegistrationReport([], [], [], []);

        // Act
        var text = report.Format();

        // Assert
        Assert.DoesNotContain("Type load failures", text);
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

    // Deliberately empty: type-identity markers for this test's registration-report formatting, not data holders.
#pragma warning disable S2094
    private sealed class SampleService;

    private sealed class SampleImplementation;
#pragma warning restore S2094

    private sealed class SampleOpenGenericImplementation<T1, T2> : ISampleOpenGenericService<T1, T2>;
}
