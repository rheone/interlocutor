namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.DependencyInjection.Abstractions;
using Interlocutor.DependencyInjection.Autofac;

public class ContainerBuilderExtensionsTests
{
    /// <summary>
    /// A closed pipeline behavior, matched by <see cref="AddTypeMatch_ClosedBehaviorMatchedAgainstOpenService_BuildsAndResolves"/>
    /// against the open <see cref="IPipelineBehavior{TRequest,TResponse}"/> service type - the shape ticket 01
    /// covers: a closed implementation registered against an open Autofac service.
    /// </summary>
    private sealed class ClosedMatchedBehavior : IPipelineBehavior<MatchedRequest, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(MatchedRequest request, Func<Task<OperationOutcome>> next, CancellationToken ct) =>
            next();
    }

    private sealed record MatchedRequest;

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

    [Fact]
    public void AddTypeMatch_ClosedBehaviorMatchedAgainstOpenService_BuildsAndResolves()
    {
        // Ticket 01: a type-match rule targeting TypeMatchTarget.Behavior always records the open
        // IPipelineBehavior<,> service type paired with the closed matched implementation type - the same
        // open-service/closed-implementation shape AddBehavior(Type, RegistrationLifetime) produces. Before
        // this fix, Autofac's ContainerBuilderExtensions.Apply called .As(openServiceType) directly for this
        // pairing, which throws ArgumentException at Build() time.
        var builder = new ContainerBuilder();

        builder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly)
                .AddTypeMatch(t => t == typeof(ClosedMatchedBehavior), TypeMatchTarget.Behavior)
        );

        var container = builder.Build();

        var behavior = container.Resolve<IPipelineBehavior<MatchedRequest, OperationOutcome>>();
        Assert.IsType<ClosedMatchedBehavior>(behavior);
    }
}
