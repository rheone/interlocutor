namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

#pragma warning disable SA1402

public record TestCommand(int EntityId) : ICommand<Outcome<string, NotFound, Forbidden>>;

public class TestEntity
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

public class TestResourceCommandHandler : ResourceCommandHandler<TestCommand, TestEntity, Outcome<string, NotFound, Forbidden>>
{
    private readonly TestEntity? _loadedEntity;
    private readonly Forbidden? _authorizationDenial;
    private readonly Outcome<string, NotFound, Forbidden> _handleResult;

    public TestResourceCommandHandler(
        TestEntity? loadedEntity,
        Forbidden? authorizationDenial,
        Outcome<string, NotFound, Forbidden> handleResult
    )
    {
        this._loadedEntity = loadedEntity;
        this._authorizationDenial = authorizationDenial;
        this._handleResult = handleResult;
    }

    protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
        Task.FromResult(this._loadedEntity);

    protected override Forbidden? Authorize(TestEntity entity, TestCommand command) => this._authorizationDenial;

    protected override Task<Outcome<string, NotFound, Forbidden>> HandleAsync(
        TestCommand command,
        TestEntity entity,
        CancellationToken ct
    ) => Task.FromResult(this._handleResult);
}

/// <summary>
/// PROTOTYPE (spike/eject-withcase-outside-pipeline, <c>.scratch/resource-command-handler-case-fidelity/issues/01-...</c>):
/// tests <see cref="ResourceCommandHandler{TCommand, TEntity, TResponse}"/> now that <c>NotFound</c>/<c>Forbidden</c>
/// are ejected via <see cref="Eject.WithCase{TCase}(TCase)"/> instead of constructed directly.
/// </summary>
/// <remarks>
/// A call directly against <see cref="ResourceCommandHandler{TCommand, TEntity, TResponse}.HandleAsync(TCommand, CancellationToken)"/>
/// - bypassing the mediator entirely, as the tests below do - now throws <see cref="PipelineEjectCaseException"/>
/// for the not-found/forbidden paths rather than returning a value: this class never constructs
/// <c>TResponse</c> itself any more, only the dispatch machinery that resolves it as an
/// <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> does. See
/// <see cref="ResourceCommandHandlerEjectCaseEndToEndTests"/> for proof that the same short-circuit results and
/// success path are observed unchanged by a caller going through a real <c>Mediator</c> dispatch, which is how
/// every actual consumer (including this base class's subclasses) is reached in practice.
/// </remarks>
public class ResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingEntity_ReturnsHandlerResult()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var expectedResult = new Outcome<string, NotFound, Forbidden>("Success");
        var handler = new TestResourceCommandHandler(entity, null, expectedResult);

        // Act - the success path never ejects, so a direct call still returns normally.
        var result = await handler.HandleAsync(new TestCommand(1), CancellationToken.None);

        // Assert
        Assert.True(result.TryGetValue(out string _));
        Assert.Equal("Success", (string)result.Value);
    }

    [Fact]
    public async Task Handle_WithMissingEntity_EjectsNotFoundCase()
    {
        // Arrange
        var handler = new TestResourceCommandHandler(null, null, new Outcome<string, NotFound, Forbidden>("unused"));

        // Act - this class no longer constructs TResponse itself for a missing entity: it throws the
        // type-erased eject signal and leaves construction to the dispatch machinery's catch site.
        var ex = await Assert.ThrowsAsync<PipelineEjectCaseException>(() =>
            handler.HandleAsync(new TestCommand(999), CancellationToken.None)
        );

        // Assert
        var notFound = Assert.IsType<NotFound>(ex.CaseValue);
        Assert.Contains("TestEntity", notFound.Message);
        Assert.Contains("not found", notFound.Message);
    }

    [Fact]
    public async Task Handle_WithAuthorizationDenial_EjectsForbiddenCase()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var denial = new Forbidden("Access denied");
        var handler = new TestResourceCommandHandler(entity, denial, new Outcome<string, NotFound, Forbidden>("unused"));

        // Act
        var ex = await Assert.ThrowsAsync<PipelineEjectCaseException>(() =>
            handler.HandleAsync(new TestCommand(1), CancellationToken.None)
        );

        // Assert
        var forbidden = Assert.IsType<Forbidden>(ex.CaseValue);
        Assert.Equal("Access denied", forbidden.Reason);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task Handle_WithMissingEntity_AndOverriddenDescribeKey_UsesOverrideInEjectedNotFoundMessage()
    {
        // Arrange - DescribeKey's default implementation returns the command itself; a subclass overriding
        // it should have that override reflected in the ejected NotFound's message instead of the raw command.
        var handler = new DescribeKeyOverridingHandler();

        // Act
        var ex = await Assert.ThrowsAsync<PipelineEjectCaseException>(() =>
            handler.HandleAsync(new TestCommand(42), CancellationToken.None)
        );

        // Assert
        var notFound = Assert.IsType<NotFound>(ex.CaseValue);
        Assert.Contains("entity-42", notFound.Message);
    }

    private sealed class DescribeKeyOverridingHandler
        : ResourceCommandHandler<TestCommand, TestEntity, Outcome<string, NotFound, Forbidden>>
    {
        protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
            Task.FromResult<TestEntity?>(null);

        protected override Forbidden? Authorize(TestEntity entity, TestCommand command) => null;

        protected override Task<Outcome<string, NotFound, Forbidden>> HandleAsync(
            TestCommand command,
            TestEntity entity,
            CancellationToken ct
        ) => throw new InvalidOperationException("Should not be called when the entity is missing.");

        protected override object DescribeKey(TestCommand command) => $"entity-{command.EntityId}";
    }
}
