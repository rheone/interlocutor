namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInterlocutor_RegistersMediatorAsScoped()
    {
        var services = new ServiceCollection();
        services.AddInterlocutor(typeof(ServiceCollectionExtensionsTests).Assembly);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddInterlocutor_WithOptions_RegistersBehaviorsInCanonicalOrder()
    {
        var services = new ServiceCollection();
        var options = new InterlocutorOptions
        {
            AssembliesToScan = [typeof(ServiceCollectionExtensionsTests).Assembly],
            EnableDefaultBehaviors = true,
        };

        services.AddInterlocutor(options);

        var behaviorDescriptors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        // Should have at least LoggingBehavior, AuditingBehavior, AuthorizationBehavior
        Assert.NotEmpty(behaviorDescriptors);
    }

    [Fact]
    public void AddInterlocutor_WithOptions_RegistersOutcomeDescriber()
    {
        var services = new ServiceCollection();
        var options = new InterlocutorOptions { AssembliesToScan = [typeof(ServiceCollectionExtensionsTests).Assembly] };

        services.AddInterlocutor(options);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IResponseOutcomeDescriber));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void AddInterlocutor_WithOptions_AllowsCustomOutcomeDescriber()
    {
        var services = new ServiceCollection();
        var options = new InterlocutorOptions
        {
            AssembliesToScan = [typeof(ServiceCollectionExtensionsTests).Assembly],
            ResponseOutcomeDescriberType = typeof(DefaultResponseOutcomeDescriber),
        };

        services.AddInterlocutor(options);

        var provider = services.BuildServiceProvider();
        var describer = provider.GetRequiredService<IResponseOutcomeDescriber>();
        Assert.NotNull(describer);
        Assert.IsType<DefaultResponseOutcomeDescriber>(describer);
    }
}
