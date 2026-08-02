using Autofac;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;

namespace Mediance.DependencyInjection.Autofac.Tests;

public class ContainerBuilderExtensionsTests
{
    [Fact]
    public void AddMediance_RegistersMediator()
    {
        var builder = new ContainerBuilder();
        builder.AddMediance(typeof(ContainerBuilderExtensionsTests).Assembly);
        var container = builder.Build();

        var mediator = container.ResolveOptional<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddMediance_WithOptions_RegistersBehaviors()
    {
        var builder = new ContainerBuilder();
        var options = new MedianceOptions
        {
            AssembliesToScan = [typeof(ContainerBuilderExtensionsTests).Assembly],
            EnableDefaultBehaviors = true,
        };

        builder.AddMediance(options);
        var container = builder.Build();

        var mediator = container.ResolveOptional<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddMediance_WithOptions_RegistersOutcomeDescriber()
    {
        var builder = new ContainerBuilder();
        var options = new MedianceOptions
        {
            AssembliesToScan = [typeof(ContainerBuilderExtensionsTests).Assembly],
        };

        builder.AddMediance(options);
        var container = builder.Build();

        var describer = container.ResolveOptional<IResponseOutcomeDescriber>();
        Assert.NotNull(describer);
    }
}
