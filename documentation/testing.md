# Testing Handlers & Pipelines

Strategies for testing code built on Interlocutor. See `docs/PRD.md` §16 for the testing strategy overview.

## Unit Testing Handlers

Handlers are plain classes with injected dependencies — test them directly without the mediator:

```csharp
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<UserDto, NotFound>>
{
    private readonly IUserRepository repository;

    public GetUserHandler(IUserRepository repository) => this.repository = repository;

    public async Task<OperationOutcome<UserDto, NotFound>> Handle(
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
    var success = Assert.IsType<OperationOutcome<UserDto, NotFound>>(result);
    Assert.True(success.IsSuccess);
    var user = success.Value;
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
    Assert.False(result.IsSuccess);
    var notFound = Assert.IsType<NotFound>(result.Value);
    Assert.Contains("not found", notFound.Message);
}
```

## Integration Testing Pipeline Composition

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
        builder.AddBehavior(typeof(LoggingBehavior<,>));
        builder.AddBehavior(typeof(ValidationBehavior<,>));
        builder.AddBehavior(typeof(TransactionBehavior<,>));
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
    Assert.False(result.IsSuccess);
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
        builder.AddBehavior(typeof(AuthorizationBehavior<,>));
        builder.AddBehavior(typeof(TransactionBehavior<,>));
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
    Assert.False(result.IsSuccess);
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
        var logger = new Mock<ILogger<LoggingBehavior<GetUser, OperationOutcome<UserDto, NotFound>>>>();
        var behavior = new LoggingBehavior<GetUser, OperationOutcome<UserDto, NotFound>>(logger.Object);

        var query = new GetUser(1);
        var response = new OperationOutcome<UserDto, NotFound>(new UserDto(1, "Alice"));

        Func<Task<OperationOutcome<UserDto, NotFound>>> next = () => Task.FromResult(response);

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

Verify that all handlers are registered at startup:

```csharp
[Fact]
public void AddInterlocutor_WithMissingHandler_ThrowsOnStartup()
{
    // Arrange: don't register handlers
    var services = new ServiceCollection();
    
    // Act & Assert: building the container should throw because no handler for GetUser
    var ex = Assert.Throws<InvalidOperationException>(() =>
    {
        services.AddInterlocutor(builder =>
        {
            builder.ScanAssembly(typeof(Program).Assembly);  // But no GetUserHandler registered
        });
        
        var provider = services.BuildServiceProvider();  // Should throw here
    });
    
    Assert.Contains("GetUser", ex.Message);
}
```

## Testing with an In-Memory Database

For integration tests with real transactions, use an in-memory database:

```csharp
[Fact]
public async Task CreateOrder_WithTransaction_RollsBackOnFailure()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddInterlocutor(builder =>
    {
        builder.ScanAssembly(typeof(Tests).Assembly);
        builder.AddBehavior(typeof(TransactionBehavior<,>));
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
    Assert.False(result2.IsSuccess);
    var conflict = Assert.IsType<Conflict>(result2.Value);
    
    // Transaction rolled back — no duplicate order in database
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

    Func<Task<OperationOutcome<UserDto, NotFound>>> next = () =>
        Task.FromResult(new OperationOutcome<UserDto, NotFound>(new UserDto(1, "Bob")));  // Different

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

1. **Unit test handlers directly** — mock their dependencies, bypass the mediator. Fast and focused.
2. **Integration test pipeline composition** — use a real container, verify order and short-circuiting.
3. **Test behaviors independently** — mock the `next()` delegate, verify the behavior's before/after logic.
4. **Use in-memory databases** — for realistic transaction testing without a real database.
5. **Verify startup validation** — catch missing handlers at build time in your CI, not at runtime.
6. **Keep tests focused** — one scenario per test, one assertion per behavior test.
