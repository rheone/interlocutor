namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions.Pipeline.Diagnostics;

/// <summary>
/// Covers <c>IPipelineExplainer</c> registration under Autofac - opt-in only when
/// <c>InterlocutorPipelineBuilder.AddPipelineExplainer</c> is called, using the plain POCO
/// <see cref="PipelineExplainerOptions"/> the builder was called with directly - Autofac has no
/// <c>IOptions</c>-based precedence layer the way Microsoft DI does.
/// </summary>
public class PipelineExplainerRegistrationTests
{
    [Fact]
    public void AddPipelineExplainer_NotCalled_IPipelineExplainerIsNotRegistered()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(cfg => cfg.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly));

        // Act
        var container = containerBuilder.Build();

        // Assert
        Assert.Null(container.ResolveOptional<IPipelineExplainer>());
    }

    [Fact]
    public void AddPipelineExplainer_Called_RegistersIPipelineExplainerUsingThePocoOptionsDirectly()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(cfg =>
        {
            cfg.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            cfg.Pipeline.AddPipelineExplainer(new PipelineExplainerOptions { Enabled = true });
        });
        var container = containerBuilder.Build();
        var explainer = container.Resolve<IPipelineExplainer>();

        // Act - Enabled from the POCO passed to AddPipelineExplainer means this must not throw.
        var explanation = explainer.Explain(typeof(object), typeof(object), container.Resolve<IServiceProvider>());

        // Assert
        Assert.Empty(explanation.ResolvedBehaviorTypes);
    }

    [Fact]
    public void AddPipelineExplainer_DisabledByDefault_ExplainThrows()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(cfg =>
        {
            cfg.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            cfg.Pipeline.AddPipelineExplainer();
        });
        var container = containerBuilder.Build();
        var explainer = container.Resolve<IPipelineExplainer>();
        var services = container.Resolve<IServiceProvider>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => explainer.Explain(typeof(object), typeof(object), services));
    }
}
