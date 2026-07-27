using Autofac;

namespace Mediance.DependencyInjection.Autofac.Tests;

public class ContainerBuilderExtensionsTests
{
    [Fact]
    public void AddMediance_RegistersMediator()
    {
        var builder = new ContainerBuilder();
        builder.AddMediance(typeof(ContainerBuilderExtensionsTests).Assembly);
        var container = builder.Build();

        var mediator = container.ResolveOptional<Abstractions.IMediator>();
        Assert.NotNull(mediator);
    }
}
