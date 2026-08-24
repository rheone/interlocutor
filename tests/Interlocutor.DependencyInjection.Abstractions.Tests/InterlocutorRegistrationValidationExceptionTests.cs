namespace Interlocutor.DependencyInjection.Abstractions.Tests;

// Auto Generated, verify expected behavior:
// InterlocutorRegistrationValidationException had no direct test coverage in this project - it is thrown by
// the container-specific translator packages (Interlocutor.DependencyInjection.Microsoft/.Autofac), not by
// anything in this project, but it is a public type owned by this assembly. Covers the Report assignment and
// the aggregated message BuildMessage produces from every RegistrationProblem, plus its own null-report guard.
public class InterlocutorRegistrationValidationExceptionTests
{
    [Fact]
    public void Constructor_AssignsReport()
    {
        // Arrange
        var report = new RegistrationReport(
            [],
            [],
            [],
            [new RegistrationProblem(RegistrationProblemKind.UnhandledRequestType, "SampleRequest has no handler")]
        );

        // Act
        var exception = new InterlocutorRegistrationValidationException(report);

        // Assert
        Assert.Same(report, exception.Report);
    }

    [Fact]
    public void Constructor_BuildsMessage_ListingEveryProblem()
    {
        // Arrange
        var report = new RegistrationReport(
            [],
            [],
            [],
            [
                new RegistrationProblem(RegistrationProblemKind.UnhandledRequestType, "SampleRequest has no handler"),
                new RegistrationProblem(RegistrationProblemKind.ConflictingLifetime, "SampleHandler has conflicting lifetimes"),
            ]
        );

        // Act
        var exception = new InterlocutorRegistrationValidationException(report);

        // Assert
        Assert.Contains("SampleRequest has no handler", exception.Message);
        Assert.Contains("SampleHandler has conflicting lifetimes", exception.Message);
        Assert.Contains(nameof(RegistrationProblemKind.UnhandledRequestType), exception.Message);
        Assert.Contains(nameof(RegistrationProblemKind.ConflictingLifetime), exception.Message);
    }

    [Fact]
    public void Constructor_WithNullReport_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => new InterlocutorRegistrationValidationException(null!));

        // Assert
        Assert.Equal("report", ex.ParamName);
    }
}
