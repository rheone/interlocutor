namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.DependencyInjection.Autofac;

public class ContainerBuilderExtensionsTests
{
    [Fact]
    public void AddInterlocutor_RegistersMediator()
    {
        var builder = new ContainerBuilder();
        builder.AddInterlocutor(cfg => cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly));
        var container = builder.Build();

        var mediator = container.ResolveOptional<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void UseRecommendedChain_RegistersBehaviors()
    {
        var builder = new ContainerBuilder();

        builder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly).Pipeline.UseRecommendedChain()
        );
        var container = builder.Build();

        var mediator = container.ResolveOptional<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void OverrideOutcomeDescriber_RegistersOutcomeDescriber()
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(OutcomeClassifierBuilder.DefaultClassifier).As<IOutcomeClassifier>();

        builder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly)
                .Pipeline.OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>()
        );
        var container = builder.Build();

        var describer = container.ResolveOptional<IResponseOutcomeDescriber>();
        Assert.NotNull(describer);
    }
}
