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
    [Fact]
    public void AddInterlocutor_RegistersMediator()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.AddInterlocutor(cfg => cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly));

        // Act
        var container = builder.Build();
        var mediator = container.ResolveOptional<IMediator>();

        // Assert
        Assert.NotNull(mediator);
    }

    [Fact]
    public void UseRecommendedChain_RegistersBehaviors()
    {
        // Arrange
        var builder = new ContainerBuilder();

        builder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly).Pipeline.UseRecommendedChain()
        );

        // Act
        var container = builder.Build();
        var mediator = container.ResolveOptional<IMediator>();

        // Assert
        Assert.NotNull(mediator);
    }

    [Fact]
    public void OverrideOutcomeDescriber_RegistersOutcomeDescriber()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.RegisterInstance(OutcomeClassifierBuilder.DefaultClassifier).As<IOutcomeClassifier>();

        builder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly)
                .Pipeline.OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>()
        );

        // Act
        var container = builder.Build();
        var describer = container.ResolveOptional<IResponseOutcomeDescriber>();

        // Assert
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

        // Arrange
        var builder = new ContainerBuilder();

        builder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(ContainerBuilderExtensionsTests).Assembly)
                .AddTypeMatch(t => t == typeof(ClosedMatchedBehavior), TypeMatchTarget.Behavior)
        );

        // Act
        var container = builder.Build();
        var behavior = container.Resolve<IPipelineBehavior<MatchedRequest, Outcome>>();

        // Assert
        Assert.IsType<ClosedMatchedBehavior>(behavior);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void AddInterlocutor_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => builder.AddInterlocutor(null!));

        // Assert
        Assert.Equal("configure", exception.ParamName);
    }

    /// <summary>
    /// A closed pipeline behavior, matched by <see cref="AddTypeMatch_ClosedBehaviorMatchedAgainstOpenService_BuildsAndResolves"/>
    /// against the open <see cref="IPipelineBehavior{TRequest,TResponse}"/> service type - the shape ticket 01
    /// covers: a closed implementation registered against an open Autofac service.
    /// </summary>
    private sealed class ClosedMatchedBehavior : IPipelineBehavior<MatchedRequest, Outcome>
    {
        public Task<Outcome> Handle(MatchedRequest request, Func<Task<Outcome>> next, CancellationToken ct) =>
            this.HandleAsync(request, next, ct);

        public Task<Outcome> HandleAsync(MatchedRequest request, Func<Task<Outcome>> next, CancellationToken ct) => next();
    }

    // Deliberately empty: a type-identity marker for this test's generic-constraint shape, not a data holder.
#pragma warning disable S2094
    private sealed record MatchedRequest;
#pragma warning restore S2094
}
