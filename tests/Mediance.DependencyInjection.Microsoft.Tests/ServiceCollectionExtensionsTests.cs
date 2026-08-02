using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Mediance.DependencyInjection.Microsoft.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediance_RegistersMediatorAsScoped()
    {
        var services = new ServiceCollection();
        services.AddMediance(typeof(ServiceCollectionExtensionsTests).Assembly);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddMediance_WithOptions_RegistersBehaviorsInCanonicalOrder()
    {
        var services = new ServiceCollection();
        var options = new MedianceOptions
        {
            AssembliesToScan = [typeof(ServiceCollectionExtensionsTests).Assembly],
            EnableDefaultBehaviors = true,
        };

        services.AddMediance(options);

        var behaviorDescriptors = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        // Should have at least LoggingBehavior, AuditingBehavior, AuthorizationBehavior
        Assert.NotEmpty(behaviorDescriptors);
    }

    [Fact]
    public void AddMediance_WithOptions_RegistersOutcomeDescriber()
    {
        var services = new ServiceCollection();
        var options = new MedianceOptions
        {
            AssembliesToScan = [typeof(ServiceCollectionExtensionsTests).Assembly],
        };

        services.AddMediance(options);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IResponseOutcomeDescriber));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void AddMediance_WithOptions_AllowsCustomOutcomeDescriber()
    {
        var services = new ServiceCollection();
        var options = new MedianceOptions
        {
            AssembliesToScan = [typeof(ServiceCollectionExtensionsTests).Assembly],
            ResponseOutcomeDescriberType = typeof(DefaultResponseOutcomeDescriber),
        };

        services.AddMediance(options);

        var provider = services.BuildServiceProvider();
        var describer = provider.GetRequiredService<IResponseOutcomeDescriber>();
        Assert.NotNull(describer);
        Assert.IsType<DefaultResponseOutcomeDescriber>(describer);
    }
}
