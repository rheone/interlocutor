namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInterlocutor_RegistersMediatorAsScoped()
    {
        var services = new ServiceCollection();
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly));

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void UseRecommendedChain_RegistersBehaviorsInCanonicalOrder()
    {
        var services = new ServiceCollection();

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly).UseRecommendedChain()
        );

        var behaviorDescriptors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        // Should have at least LoggingBehavior, AuditingBehavior, AuthorizationBehavior
        Assert.NotEmpty(behaviorDescriptors);
    }

    [Fact]
    public void OverrideOutcomeDescriber_RegistersOutcomeDescriber()
    {
        var services = new ServiceCollection();

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly)
                .OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>()
        );

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IResponseOutcomeDescriber));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void OverrideOutcomeDescriber_AllowsCustomOutcomeDescriber()
    {
        var services = new ServiceCollection();
        services.AddSingleton(OutcomeClassifierBuilder.DefaultClassifier);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly)
                .OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>()
        );

        var provider = services.BuildServiceProvider();
        var describer = provider.GetRequiredService<IResponseOutcomeDescriber>();
        Assert.NotNull(describer);
        Assert.IsType<DefaultResponseOutcomeDescriber>(describer);
    }
}
