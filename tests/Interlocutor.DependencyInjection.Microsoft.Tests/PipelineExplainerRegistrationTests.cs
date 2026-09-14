namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Options;
using Interlocutor.Abstractions.Pipeline.Diagnostics;

/// <summary>
/// Covers <c>IPipelineExplainer</c> registration under Microsoft DI - opt-in only when
/// <c>InterlocutorPipelineBuilder.AddPipelineExplainer</c> is called, and the Microsoft-DI-specific
/// precedence where an application's own <see cref="IOptions{TOptions}"/>/<see cref="IOptionsMonitor{TOptions}"/>
/// registration for <see cref="PipelineExplainerOptions"/> wins over the builder-supplied value.
/// </summary>
public class PipelineExplainerRegistrationTests
{
    [Fact]
    public void AddPipelineExplainer_NotCalled_IPipelineExplainerIsNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly));
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.Null(provider.GetService<IPipelineExplainer>());
    }

    [Fact]
    public void AddPipelineExplainer_Called_RegistersIPipelineExplainer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            builder.Pipeline.AddPipelineExplainer();
        });
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IPipelineExplainer>());
    }

    [Fact]
    public void AddPipelineExplainer_DisabledByDefault_ExplainThrows()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            builder.Pipeline.AddPipelineExplainer();
        });
        var provider = services.BuildServiceProvider();
        var explainer = provider.GetRequiredService<IPipelineExplainer>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => explainer.Explain(typeof(object), typeof(object), provider));
    }

    [Fact]
    public void AddPipelineExplainer_NoConsumerOptionsRegistered_UsesBuilderSuppliedOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            builder.Pipeline.AddPipelineExplainer(new PipelineExplainerOptions { Enabled = true });
        });
        var provider = services.BuildServiceProvider();
        var explainer = provider.GetRequiredService<IPipelineExplainer>();

        // Act - Enabled from the builder-supplied options means this must not throw.
        var explanation = explainer.Explain(typeof(object), typeof(object), provider);

        // Assert
        Assert.Empty(explanation.ResolvedBehaviorTypes);
    }

    [Fact]
    public void AddPipelineExplainer_ConsumerConfiguresOptions_ConsumerConfigurationWinsOverBuilderSuppliedValue()
    {
        // Arrange - the application configures PipelineExplainerOptions itself (Enabled = true), while the
        // builder call passes an explicitly disabled instance. The consumer's own registration must win.
        var services = new ServiceCollection();
        services.Configure<PipelineExplainerOptions>(o => o.Enabled = true);
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            builder.Pipeline.AddPipelineExplainer(new PipelineExplainerOptions { Enabled = false });
        });
        var provider = services.BuildServiceProvider();
        var explainer = provider.GetRequiredService<IPipelineExplainer>();

        // Act - would throw if the disabled builder-supplied options had won instead.
        var explanation = explainer.Explain(typeof(object), typeof(object), provider);

        // Assert
        Assert.Empty(explanation.ResolvedBehaviorTypes);
    }

    [Fact]
    public void AddPipelineExplainer_ConsumerConfiguresDisabledOptions_ConsumerConfigurationWinsOverEnabledBuilderSuppliedValue()
    {
        // Arrange - the reverse precedence direction: the consumer's own configuration disables the
        // explainer even though the builder call passed an enabled instance.
        var services = new ServiceCollection();
        services.Configure<PipelineExplainerOptions>(o => o.Enabled = false);
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(PipelineExplainerRegistrationTests).Assembly);
            builder.Pipeline.AddPipelineExplainer(new PipelineExplainerOptions { Enabled = true });
        });
        var provider = services.BuildServiceProvider();
        var explainer = provider.GetRequiredService<IPipelineExplainer>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => explainer.Explain(typeof(object), typeof(object), provider));
    }
}
