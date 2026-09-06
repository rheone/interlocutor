namespace Interlocutor.Abstractions.Tests;

using FluentValidation;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Tests for <see cref="InterlocutorStartupValidation.ValidatePipelineCaseFidelity"/>.</summary>
/// <remarks>
/// This test assembly already declares several deliberately mismatched marker/case pairs elsewhere (see
/// <c>AuthorizationBehaviorTests.MisshapenProtectedQuery</c> and its siblings in
/// <c>CancellationBehaviorTests</c>/<c>RateLimitedBehaviorTests</c>/<c>TimeoutBehaviorTests</c>), so scanning
/// this whole assembly always throws - exactly like <see cref="InterlocutorStartupValidationTests"/>'s own
/// <c>ValidateHandlerRegistrations_HandlerRegisteredForRequestType_DoesNotReportThatTypeAsMissing_Test"</c>
/// already works around for <see cref="InterlocutorStartupValidation.ValidateHandlerRegistrations"/>. The
/// "passes silently" tests below assert the narrower claim that idiom uses: whichever mismatches are
/// reported, a specific correctly-shaped type here is never one of them.
/// </remarks>
public class InterlocutorStartupValidationCaseFidelityTests
{
    [Fact]
    public void ValidatePipelineCaseFidelity_RequiresPolicyRequestWithForbiddenCase_DoesNotReportThatTypeAsMismatched_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050 // reflection-based scan/construction is explicitly opt-in for this startup-only check
        var ex = Record.Exception(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMatchedPolicyCommand).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(
            nameof(CaseFidelityMatchedPolicyCommand),
            invalidOperationException.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_RequiresPolicyRequestWithoutForbiddenCase_ThrowsNamingTheRequestType_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMismatchedPolicyCommand).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Contains(nameof(CaseFidelityMismatchedPolicyCommand), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IRequiresPolicyRequest), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Forbidden), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_CancellableRequestWithCancelledCase_DoesNotReportThatTypeAsMismatched_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Record.Exception(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMatchedCancellableQuery).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(
            nameof(CaseFidelityMatchedCancellableQuery),
            invalidOperationException.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_CancellableRequestWithoutCancelledCase_ThrowsNamingTheRequestType_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMismatchedCancellableQuery).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Contains(nameof(CaseFidelityMismatchedCancellableQuery), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ICancellableRequest), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Cancelled), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_RateLimitedRequestWithRateLimitedCase_DoesNotReportThatTypeAsMismatched_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Record.Exception(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMatchedRateLimitedCommand).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(
            nameof(CaseFidelityMatchedRateLimitedCommand),
            invalidOperationException.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_RateLimitedRequestWithoutRateLimitedCase_ThrowsNamingTheRequestType_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMismatchedRateLimitedCommand).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Contains(nameof(CaseFidelityMismatchedRateLimitedCommand), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IRateLimitedRequest), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(RateLimited), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_TimeoutRequestWithTimeoutCase_DoesNotReportThatTypeAsMismatched_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Record.Exception(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMatchedTimeoutQuery).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(
            nameof(CaseFidelityMatchedTimeoutQuery),
            invalidOperationException.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_TimeoutRequestWithoutTimeoutCase_ThrowsNamingTheRequestType_Test()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityMismatchedTimeoutQuery).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Contains(nameof(CaseFidelityMismatchedTimeoutQuery), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ITimeoutRequest), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Timeout), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_ValidatorRegisteredWithoutValidationErrorsCase_ThrowsNamingTheRequestType_Test()
    {
        // Arrange - a real FluentValidation.IValidator<CaseFidelityValidatedCommandNoCase> is actually
        // registered, but the response type declares neither ValidationErrors nor FluentValidationErrors -
        // the one fact only reachable here, never at INTERLOC015's compile time (ADR-0023).
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CaseFidelityValidatedCommandNoCase>, CaseFidelityValidatedCommandNoCaseValidator>();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityValidatedCommandNoCase).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        Assert.Contains(nameof(CaseFidelityValidatedCommandNoCase), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ValidationErrors), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_ValidatorRegisteredWithValidationErrorsCase_DoesNotReportThatTypeAsMismatched_Test()
    {
        // Arrange - same shape as the previous test, but the response type does declare ValidationErrors.
        var services = new ServiceCollection();
        services.AddSingleton<
            IValidator<CaseFidelityValidatedCommandWithCase>,
            CaseFidelityValidatedCommandWithCaseValidator
        >();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Record.Exception(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityValidatedCommandWithCase).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(
            nameof(CaseFidelityValidatedCommandWithCase),
            invalidOperationException.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ValidatePipelineCaseFidelity_NoValidatorRegisteredForRequestWithoutValidationErrorsCase_DoesNotReportThatTypeAsMismatched_Test()
    {
        // Arrange - CaseFidelityValidatedCommandNoCase's own response has no ValidationErrors case, but with
        // no IValidator<> registered at all for it here, the FluentValidation-shaped check must not fire -
        // whether a validator is registered is a DI-registration fact, not a static property of the type.
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
#pragma warning disable IL2026, IL3050
        var ex = Record.Exception(() =>
            provider.ValidatePipelineCaseFidelity(typeof(CaseFidelityValidatedCommandNoCase).Assembly)
        );
#pragma warning restore IL2026, IL3050

        // Assert
        var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
        Assert.DoesNotContain(
            nameof(CaseFidelityValidatedCommandNoCase),
            invalidOperationException.Message,
            StringComparison.Ordinal
        );
    }

    public sealed record CaseFidelityMatchedPolicyCommand(string PolicyName)
        : ICommand<Outcome<string, Forbidden>>,
            IRequiresPolicyRequest;

    public sealed record CaseFidelityMismatchedPolicyCommand(string PolicyName)
        : ICommand<Outcome<string, NotFound>>,
            IRequiresPolicyRequest;

    public sealed record CaseFidelityMatchedCancellableQuery(string Name)
        : IQuery<Outcome<string, Cancelled>>,
            ICancellableRequest;

    public sealed record CaseFidelityMismatchedCancellableQuery(string Name)
        : IQuery<Outcome<string, NotFound>>,
            ICancellableRequest;

    public sealed record CaseFidelityMatchedRateLimitedCommand(string LimitKey)
        : ICommand<Outcome<string, RateLimited>>,
            IRateLimitedRequest;

    public sealed record CaseFidelityMismatchedRateLimitedCommand(string LimitKey)
        : ICommand<Outcome<string, NotFound>>,
            IRateLimitedRequest;

    public sealed record CaseFidelityMatchedTimeoutQuery(TimeSpan Budget) : IQuery<Outcome<string, Timeout>>, ITimeoutRequest;

    public sealed record CaseFidelityMismatchedTimeoutQuery(TimeSpan Budget)
        : IQuery<Outcome<string, NotFound>>,
            ITimeoutRequest;

    public sealed record CaseFidelityValidatedCommandNoCase(string Name) : ICommand<Outcome<string, NotFound>>;

    public sealed class CaseFidelityValidatedCommandNoCaseValidator : AbstractValidator<CaseFidelityValidatedCommandNoCase>
    {
        public CaseFidelityValidatedCommandNoCaseValidator() => this.RuleFor(x => x.Name).NotEmpty();
    }

    public sealed record CaseFidelityValidatedCommandWithCase(string Name) : ICommand<Outcome<string, ValidationErrors>>;

    public sealed class CaseFidelityValidatedCommandWithCaseValidator : AbstractValidator<CaseFidelityValidatedCommandWithCase>
    {
        public CaseFidelityValidatedCommandWithCaseValidator() => this.RuleFor(x => x.Name).NotEmpty();
    }
}
