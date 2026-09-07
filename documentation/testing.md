# Testing Handlers & Pipelines

Strategies for testing code built on Interlocutor.

## Unit Testing Handlers

Handlers are plain classes with injected dependencies - test them directly without the mediator:

```csharp
public record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound>>;

public class GetUserHandler : IQueryHandler<GetUser, Outcome<UserDto, NotFound>>
{
    private readonly IUserRepository repository;

    public GetUserHandler(IUserRepository repository) => this.repository = repository;

    public async Task<Outcome<UserDto, NotFound>> Handle(
        GetUser query, CancellationToken ct)
    {
        var user = await this.repository.FindAsync(query.UserId, ct);
        return user == null
            ? new NotFound($"User {query.UserId} not found")
            : new UserDto(user.Id, user.Name);
    }
}

// Unit test
[Fact]
public async Task GetUserHandler_WithExistingUser_ReturnsUserDto()
{
    // Arrange
    var repository = new Mock<IUserRepository>();
    repository
        .Setup(r => r.FindAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new User { Id = 1, Name = "Alice" });

    var handler = new GetUserHandler(repository.Object);

    // Act
    var result = await handler.Handle(new GetUser(1), CancellationToken.None);

    // Assert
    Assert.True(result.TryGetValue(out UserDto _));
    var user = Assert.IsType<UserDto>(result.Value);  // .Value is object - narrow it before reading members
    Assert.Equal("Alice", user.Name);
}

[Fact]
public async Task GetUserHandler_WithNoUser_ReturnsNotFound()
{
    // Arrange
    var repository = new Mock<IUserRepository>();
    repository
        .Setup(r => r.FindAsync(999, It.IsAny<CancellationToken>()))
        .ReturnsAsync((User?)null);

    var handler = new GetUserHandler(repository.Object);

    // Act
    var result = await handler.Handle(new GetUser(999), CancellationToken.None);

    // Assert
    Assert.False(result.TryGetValue(out UserDto _));
    var notFound = Assert.IsType<NotFound>(result.Value);
    Assert.Contains("not found", notFound.Message);
}
```

## Integration Testing Pipeline Composition

> [!WARNING]
> The examples below register EF Core's `UnitOfWorkBehavior` under Microsoft.Extensions.DependencyInjection via `TestUnitOfWorkBehavior<,>`, a subclass closing `TContext` over the test's own `DbContext`, rather than `builder.Pipeline.AddUnitOfWorkBehavior()` directly. That's deliberate, not a style choice: `AddUnitOfWorkBehavior()` currently throws at container-build time for EF Core under Microsoft DI (it works fine under Autofac). See [Transactions & Store Integration](transactions-and-store-integration.md#entityframework-core-integration) for the full explanation and the same pattern used by the runnable sample.
>
> ```csharp
> internal sealed class TestUnitOfWorkBehavior<TRequest, TResponse>(
>     Lazy<AppDbContext> context,
>     IOutcomeClassifier classifier,
>     IResponseOutcomeDescriber? describer = null
> ) : UnitOfWorkBehavior<TRequest, TResponse, AppDbContext>(context, classifier, describer);
> ```

Test that behaviors run in the expected order and handle short-circuiting correctly:

```csharp
[Fact]
public async Task PipelineOrder_ValidationBeforeTransaction()
{
    // Arrange: create a service provider with a composed pipeline
    var services = new ServiceCollection();
    services.AddInterlocutor(builder =>
    {
        builder.ScanAssembly(typeof(Program).Assembly);
        builder.Pipeline.AddLoggingBehavior();
        builder.Pipeline.AddValidationBehavior();
        builder.Pipeline.AddBehavior(typeof(TestUnitOfWorkBehavior<,>));  // see warning above
    });

    services.AddDbContext<TestDbContext>(
        options => options.UseInMemoryDatabase("test"),
        ServiceLifetime.Scoped
    );

    var provider = services.BuildServiceProvider();

    // Act: send a command with invalid data
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var result = await mediator.Send(new CreateOrder("", new List<OrderItem>()));

    // Assert: validation fails before the handler runs, so no transaction starts
    var validation = Assert.IsType<ValidationError>(result.Value);

    // Database should be unchanged (transaction never started)
    var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    Assert.Empty(context.Orders);
}

[Fact]
public async Task ShortCircuit_AuthorizationStopsTransaction()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddInterlocutor(builder =>
    {
        builder.ScanAssembly(typeof(Program).Assembly);
        builder.Pipeline.AddAuthorizationBehavior();
        builder.Pipeline.AddBehavior(typeof(TestUnitOfWorkBehavior<,>));  // see warning above
    });

    services.AddDbContext<TestDbContext>(
        options => options.UseInMemoryDatabase("test"),
        ServiceLifetime.Scoped
    );

    // Mock the policy evaluator to deny access
    services.AddSingleton<IPolicyEvaluator>(new DenyAllPolicyEvaluator());

    var provider = services.BuildServiceProvider();

    // Act
    using var scope = provider.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var result = await mediator.Send(
        new DeleteAllOrders { PolicyName = "DenyAll" }
    );

    // Assert: authorization short-circuits, transaction never starts
    var forbidden = Assert.IsType<Forbidden>(result.Value);

    // No side effects because transaction never started
}
```

## Testing Behaviors

Test a behavior in isolation by creating a mock handler chain:

```csharp
public class LoggingBehaviorTests
{
    [Fact]
    public async Task LoggingBehavior_LogsRequestAndResponse()
    {
        // Arrange
        var logger = new Mock<ILogger<LoggingBehavior<GetUser, Outcome<UserDto, NotFound>>>>();
        var behavior = new LoggingBehavior<GetUser, Outcome<UserDto, NotFound>>(logger.Object);

        var query = new GetUser(1);
        var response = new Outcome<UserDto, NotFound>(new UserDto(1, "Alice"));

        Func<Task<Outcome<UserDto, NotFound>>> next = () => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);

        // Verify logging was called
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GetUser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }
}
```

## DI Startup Validation

`AddInterlocutor` doesn't validate anything on its own - a missing handler stays undetected until the first dispatch that needs it. Test the opt-in checks explicitly instead of assuming either one runs by default:

```csharp
[Fact]
public void AddInterlocutor_WithValidateOnBuild_ThrowsForMissingHandler()
{
    // Arrange: don't register a GetUserHandler anywhere in the scanned assembly
    var services = new ServiceCollection();

    // Act & Assert: ValidateOnBuild() opts into checking the accumulated registration
    // list - without it, this would build successfully and only fail on first dispatch.
    var ex = Assert.Throws<InterlocutorRegistrationValidationException>(() =>
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(GetUser).Assembly);
            builder.ValidateOnBuild();
        })
    );

    Assert.Contains(ex.Report.Problems, p => p.Description.Contains("GetUser"));
}

[Fact]
public void ValidateHandlerRegistrations_ThrowsForMissingHandler()
{
    // Arrange: same missing-handler scenario, but caught after the container is built instead
    var services = new ServiceCollection();
    services.AddInterlocutor(builder => builder.ScanAssembly(typeof(GetUser).Assembly));
    var provider = services.BuildServiceProvider();

    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(
        () => provider.ValidateHandlerRegistrations(typeof(GetUser).Assembly)
    );
    Assert.Contains("GetUser", ex.Message);
}
```

## Testing with an In-Memory Database

For integration tests with real transactions, use an in-memory database:

```csharp
[Fact]
public async Task CreateOrder_DuplicateId_NeverInsertsTheDuplicate()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddInterlocutor(builder =>
    {
        builder.ScanAssembly(typeof(Tests).Assembly);
        builder.Pipeline.AddBehavior(typeof(TestUnitOfWorkBehavior<,>));  // see warning above
    });

    services.AddDbContext<AppDbContext>(
        options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
        ServiceLifetime.Scoped
    );

    var provider = services.BuildServiceProvider();

    // Act 1: valid order
    using var scope1 = provider.CreateScope();
    var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
    var result1 = await mediator1.Send(new CreateOrder(1, Items()), CancellationToken.None);

    var db1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
    Assert.Single(db1.Orders);  // Order was created and committed

    // Act 2: invalid order (duplicate unique constraint)
    using var scope2 = provider.CreateScope();
    var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

    // This command will fail with a Conflict outcome
    var handler = new CreateOrderHandler(scope2.ServiceProvider.GetRequiredService<Lazy<AppDbContext>>());
    var result2 = await handler.Handle(
        new CreateOrder(1, Items()),  // Same ID, violates uniqueness
        CancellationToken.None
    );

    // Assert
    var conflict = Assert.IsType<Conflict>(result2.Value);

    // No duplicate order in the database - the handler's own uniqueness check prevented the insert
    // before it ever staged one. Returning Conflict does not, on its own, roll anything back - see
    // Transactions & Store Integration § Commit/Rollback Semantics.
    var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
    Assert.Single(db2.Orders);  // Still just one order
}
```

## Testing Pipeline Stages

```csharp
[Fact]
public async Task CachingStage_HitReturnsFromCache()
{
    // Arrange
    var cache = new Dictionary<string, UserDto>();
    var stage = new CachingStage(cache);

    var query = new GetUser(1);
    var cachedUser = new UserDto(1, "Alice");
    cache[query.ToString()] = cachedUser;

    Func<Task<Outcome<UserDto, NotFound>>> next = () =>
        Task.FromResult(new Outcome<UserDto, NotFound>(new UserDto(1, "Bob")));  // Different

    // Act
    var result = await stage.OnBeforeAsync(query, CancellationToken.None);

    // Assert
    Assert.True(result.ShortCircuited);
    Assert.Equal(cachedUser, result.Response.Value);  // Cache hit, returned "Alice"
}
```

## Testing Notifications

Test notification handlers independently:

```csharp
[Fact]
public async Task OrderShippedEmailHandler_SendsEmail()
{
    // Arrange
    var emailService = new Mock<IEmailService>();
    var handler = new OrderShippedEmailHandler(emailService.Object);

    var notification = new OrderShipped(123, "TRACK-456");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    emailService.Verify(
        e => e.SendAsync(
            It.Is<string>(to => to.Contains("shipped")),
            It.Is<string>(msg => msg.Contains("TRACK-456")),
            It.IsAny<CancellationToken>()
        ),
        Times.Once
    );
}
```

## Best Practices

1. **Unit test handlers directly** - mock their dependencies, bypass the mediator. Fast and focused.
2. **Integration test pipeline composition** - use a real container, verify order and short-circuiting.
3. **Test behaviors independently** - mock the `next()` delegate, verify the behavior's before/after logic.
4. **Use in-memory databases** - for realistic transaction testing without a real database.
5. **Verify startup validation** - catch missing handlers at build time in your CI, not at runtime.
6. **Keep tests focused** - one scenario per test, one assertion per behavior test.
