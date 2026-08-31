namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

public record StartupValidationQuery(int Value) : IQuery<Outcome<int, Failure>>;

public class StartupValidationQueryHandler : IQueryHandler<StartupValidationQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> Handle(StartupValidationQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(query.Value));
}

public record StartupValidationCommand(string Name) : ICommand<Outcome<string, Failure>>;

/// <summary>Tests for <see cref="InterlocutorStartupValidation"/>.</summary>
public class InterlocutorStartupValidationTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void ValidateHandlerRegistrations_AssemblyWithNoConcreteRequestTypes_ReturnsTheSameProviderWithoutThrowing_Test()
    {
        // Arrange - Interlocutor.Abstractions itself declares no concrete IQuery<>/ICommand<> implementations
        // (it's the abstractions library, not application code), so scanning it finds nothing to validate.
        // Scanning this test assembly instead would pick up every other test's request records regardless of
        // what this test itself registers, making a genuine "nothing missing" happy path impossible to isolate.
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050 // reflection-based scan is explicitly opt-in for this startup-only check
        var result = provider.ValidateHandlerRegistrations(typeof(IMediator).Assembly);
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Same(provider, result);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void ValidateHandlerRegistrations_MissingHandler_ThrowsInvalidOperationExceptionNamingTheRequestType_Test()
    {
        // Arrange - no handler registered for StartupValidationCommand.
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidateHandlerRegistrations(typeof(StartupValidationCommand).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Contains(nameof(StartupValidationCommand), ex.Message);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void ValidateHandlerRegistrations_HandlerRegisteredForRequestType_DoesNotReportThatTypeAsMissing_Test()
    {
        // Arrange - the test assembly also declares many other request types with no handler registered
        // here, so scanning it may still throw for those; what this test asserts is narrower: whichever
        // request types are reported missing, StartupValidationQuery - whose handler is registered below -
        // must never be one of them.
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<StartupValidationQuery, Outcome<int, Failure>>, StartupValidationQueryHandler>();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Record.Exception(() => provider.ValidateHandlerRegistrations(typeof(StartupValidationQuery).Assembly));
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(nameof(StartupValidationQuery), invalidOperationException.Message);
    }
}
