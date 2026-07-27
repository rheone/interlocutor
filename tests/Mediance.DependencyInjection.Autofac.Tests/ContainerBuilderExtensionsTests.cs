using Autofac;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
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

        var mediator = container.ResolveOptional<Abstractions.IMediator>();
        Assert.NotNull(mediator);
    }
}
