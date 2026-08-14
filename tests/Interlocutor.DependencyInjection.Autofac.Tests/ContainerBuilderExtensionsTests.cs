namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.DependencyInjection.Autofac;

public class ContainerBuilderExtensionsTests
{
    [Fact]
    public void AddInterlocutor_RegistersMediator()
    {
        var builder = new ContainerBuilder();
        builder.AddInterlocutor(typeof(ContainerBuilderExtensionsTests).Assembly);
        var container = builder.Build();

        var mediator = container.ResolveOptional<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddInterlocutor_WithOptions_RegistersBehaviors()
    {
        var builder = new ContainerBuilder();
        var options = new InterlocutorOptions
        {
            AssembliesToScan = [typeof(ContainerBuilderExtensionsTests).Assembly],
            EnableDefaultBehaviors = true,
        };

        builder.AddInterlocutor(options);
        var container = builder.Build();

        var mediator = container.ResolveOptional<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddInterlocutor_WithOptions_RegistersOutcomeDescriber()
    {
        var builder = new ContainerBuilder();
        var options = new InterlocutorOptions { AssembliesToScan = [typeof(ContainerBuilderExtensionsTests).Assembly] };

        builder.AddInterlocutor(options);
        var container = builder.Build();

        var describer = container.ResolveOptional<IResponseOutcomeDescriber>();
        Assert.NotNull(describer);
    }
}
