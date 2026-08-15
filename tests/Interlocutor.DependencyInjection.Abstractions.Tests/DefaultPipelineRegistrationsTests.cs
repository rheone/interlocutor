namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;

public class DefaultPipelineRegistrationsTests
{
    [Fact]
    public void Behaviors_IncludesLoggingAuditingAndAuthorization_AsScopedBehaviorRegistrations()
    {
        var registrations = DefaultPipelineRegistrations.Behaviors();

        Assert.Contains(
            registrations,
            r =>
                r.ServiceType == typeof(IPipelineBehavior<,>)
                && r.ImplementationType == typeof(LoggingBehavior<,>)
                && r.Lifetime == RegistrationLifetime.Scoped
                && r.Kind == RegistrationKind.Behavior
        );
        Assert.Contains(
            registrations,
            r =>
                r.ServiceType == typeof(IPipelineBehavior<,>)
                && r.ImplementationType == typeof(AuditingBehavior<,>)
                && r.Kind == RegistrationKind.Behavior
        );
        Assert.Contains(
            registrations,
            r =>
                r.ServiceType == typeof(IPipelineBehavior<,>)
                && r.ImplementationType == typeof(AuthorizationBehavior<,>)
                && r.Kind == RegistrationKind.Behavior
        );
    }

    [Fact]
    public void Behaviors_PreservesCanonicalOrder_LoggingThenAuditingThenAuthorization()
    {
        var registrations = DefaultPipelineRegistrations
            .Behaviors()
            .Select(r => r.ImplementationType)
            .Where(t =>
                t == typeof(LoggingBehavior<,>) || t == typeof(AuditingBehavior<,>) || t == typeof(AuthorizationBehavior<,>)
            )
            .ToList();

        Assert.Equal(
            [typeof(LoggingBehavior<,>), typeof(AuditingBehavior<,>), typeof(AuthorizationBehavior<,>)],
            registrations
        );
    }

    [Fact]
    public void Dependencies_IncludesDefaultsForAuditSinkPolicyEvaluatorAndRequestContextAccessor()
    {
        var registrations = DefaultPipelineRegistrations.Dependencies();

        Assert.Contains(
            registrations,
            r =>
                r.ServiceType == typeof(IAuditSink)
                && r.ImplementationType == typeof(LoggingAuditSink)
                && r.Lifetime == RegistrationLifetime.Scoped
                && r.Kind == RegistrationKind.Default
        );
        Assert.Contains(
            registrations,
            r =>
                r.ServiceType == typeof(IPolicyEvaluator)
                && r.ImplementationType == typeof(DefaultPolicyEvaluator)
                && r.Kind == RegistrationKind.Default
        );
        Assert.Contains(
            registrations,
            r =>
                r.ServiceType == typeof(IRequestContextAccessor)
                && r.ImplementationType == typeof(AmbientRequestContextAccessor)
                && r.Kind == RegistrationKind.Default
        );
    }
}
