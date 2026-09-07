namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// PROTOTYPE (spike/eject-withcase-outside-pipeline, <c>.scratch/resource-command-handler-case-fidelity/issues/01-...</c>):
/// proves, through a real <see cref="Mediator"/> dispatch with <b>no pipeline behaviors registered at all</b>,
/// that an eject thrown from inside <see cref="ResourceCommandHandler{TCommand, TEntity, TResponse}"/> - a
/// template-method base class, not a pipeline behavior - is still caught and converted back into an ordinary
/// response.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately registers zero <c>IPipelineBehavior&lt;,&gt;</c> entries, unlike
/// <see cref="AuthorizationBehaviorEjectCaseEndToEndTests"/> (which needs <c>AuthorizationBehavior</c> in the
/// chain to have something to eject from). That absence is the point: it isolates the one remaining place an
/// eject thrown from <see cref="ResourceCommandHandler{TCommand, TEntity, TResponse}.HandleAsync(TCommand, CancellationToken)"/>
/// could possibly be caught - <c>CommandHandlerWrapper</c> resolving the subclass as
/// <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> and handing its <c>HandleAsync</c> method to
/// <c>PipelineChainExecutor.ExecuteAsync</c> as the innermost delegate, which already guards for
/// <see cref="PipelineEjectCaseException"/> before any behavior ever runs. If that guard were the only thing
/// standing between this class and a leaked exception, this test would fail with an unhandled
/// <see cref="PipelineEjectCaseException"/> instead of returning a constructed response - it doesn't, which is
/// the empirical proof this spike exists to produce.
/// </para>
/// </remarks>
public class ResourceCommandHandlerEjectCaseEndToEndTests
{
    [Fact]
    public async Task SendAsync_MissingEntity_ReturnsNotFoundConstructedByRealDispatchMachinery_WithNoPipelineBehaviorsRegistered()
    {
        // Arrange - note the absence of any AddScoped(typeof(IPipelineBehavior<,>), ...) registration.
        var services = new ServiceCollection();
        services.AddSingleton(DefaultOutcomeCaseRegistry.Instance);
        services.AddScoped<
            ICommandHandler<ResourceCommand, Outcome<string, NotFound, Forbidden>>,
            ResourceCommandHandlerUnderTest
        >(_ => new ResourceCommandHandlerUnderTest(loadedEntity: null, authorizationDenial: null));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = new Mediator(scope.ServiceProvider);

        // Act
        var result = await mediator.SendAsync(new ResourceCommand(1), TestContext.Current.CancellationToken);

        // Assert - the ejected NotFound reached this caller as an ordinary response, not a leaked exception.
        var notFound = Assert.IsType<NotFound>(result.Value);
        Assert.Contains("ResourceEntity", notFound.Message);
    }

    [Fact]
    public async Task SendAsync_AuthorizationDenied_ReturnsForbiddenConstructedByRealDispatchMachinery_WithNoPipelineBehaviorsRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(DefaultOutcomeCaseRegistry.Instance);
        var entity = new ResourceEntity { Id = 1 };
        services.AddScoped<
            ICommandHandler<ResourceCommand, Outcome<string, NotFound, Forbidden>>,
            ResourceCommandHandlerUnderTest
        >(_ => new ResourceCommandHandlerUnderTest(entity, new Forbidden("no access")));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = new Mediator(scope.ServiceProvider);

        // Act
        var result = await mediator.SendAsync(new ResourceCommand(1), TestContext.Current.CancellationToken);

        // Assert
        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Equal("no access", forbidden.Reason);
    }

    [Fact]
    public async Task SendAsync_EntityFoundAndAuthorized_ReturnsHandlerSuccess_WithNoPipelineBehaviorsRegistered()
    {
        // Arrange - the success path never ejects, so this proves the migration didn't disturb it either.
        var services = new ServiceCollection();
        services.AddSingleton(DefaultOutcomeCaseRegistry.Instance);
        var entity = new ResourceEntity { Id = 1 };
        services.AddScoped<
            ICommandHandler<ResourceCommand, Outcome<string, NotFound, Forbidden>>,
            ResourceCommandHandlerUnderTest
        >(_ => new ResourceCommandHandlerUnderTest(entity, authorizationDenial: null));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = new Mediator(scope.ServiceProvider);

        // Act
        var result = await mediator.SendAsync(new ResourceCommand(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.TryGetValue(out string success));
        Assert.Equal("resource-1", success);
    }

    private sealed record ResourceCommand(int Id) : ICommand<Outcome<string, NotFound, Forbidden>>;

    private sealed class ResourceEntity
    {
        public int Id { get; init; }
    }

    private sealed class ResourceCommandHandlerUnderTest(ResourceEntity? loadedEntity, Forbidden? authorizationDenial)
        : ResourceCommandHandler<ResourceCommand, ResourceEntity, Outcome<string, NotFound, Forbidden>>,
            ICommandHandler<ResourceCommand, Outcome<string, NotFound, Forbidden>>
    {
        protected override Task<ResourceEntity?> LoadAsync(ResourceCommand command, CancellationToken ct) =>
            Task.FromResult(loadedEntity);

        protected override Forbidden? Authorize(ResourceEntity entity, ResourceCommand command) => authorizationDenial;

        protected override Task<Outcome<string, NotFound, Forbidden>> HandleAsync(
            ResourceCommand command,
            ResourceEntity entity,
            CancellationToken ct
        ) => Task.FromResult<Outcome<string, NotFound, Forbidden>>($"resource-{entity.Id}");
    }
}
