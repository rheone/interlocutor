# ASP.NET Core Integration & HTTP Mapping

Mapping Interlocutor outcomes to HTTP responses via `Interlocutor.AspNetCore`.

> [!CAUTION]
> This is the **only documentation file** in this project permitted to reference HTTP status codes. Other docs should link here instead of mentioning HTTP anywhere. See house conventions in the documentation index.

## Installation & Registration

Add the NuGet package:

```bash
dotnet add package Interlocutor.AspNetCore
```

No additional registration needed — `Interlocutor.AspNetCore` provides extension methods used at the edge (controller/endpoint).

## ToActionResult() Extension

Map an `IOperationOutcome` to an ASP.NET Core `IActionResult`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender mediator;

    public OrdersController(ISender mediator) => this.mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrder(request.CustomerId, request.Items);
        var result = await this.mediator.Send(command, ct);

        // Map the outcome to HTTP — built-in mapping handles all case types
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct)
    {
        var result = await this.mediator.Send(new DeleteOrder(id), ct);

        // Same extension method — maps the outcome type-safely
        return result.ToActionResult();
    }
}
```

## Minimal API

Works the same in minimal APIs:

```csharp
app.MapPost("/api/orders", async (CreateOrderRequest request, ISender mediator, CancellationToken ct) =>
{
    var command = new CreateOrder(request.CustomerId, request.Items);
    var result = await this.mediator.Send(command, ct);
    return result.ToActionResult();  // Same mapping
});

app.MapDelete("/api/orders/{id}", async (int id, ISender mediator, CancellationToken ct) =>
{
    var result = await this.mediator.Send(new DeleteOrder(id), ct);
    return result.ToActionResult();
});
```

## Status Code Mapping

Each outcome case type maps to an HTTP status code:

| Outcome Case | HTTP Status | Body | Details |
|--------------|-------------|------|---------|
| Success (payload) | 200 OK | The success payload (JSON) | For `OperationOutcome<T>`, T is serialized. |
| Success (no payload) | 200 OK | Empty or omitted | For `OperationOutcome` (unit), no body. |
| ValidationError | 400 Bad Request | `ValidationProblemDetails` (RFC 7807) | Errors dictionary with field names as keys. |
| NotFound | 404 Not Found | Empty | No response body. |
| Forbidden | 403 Forbidden | Empty | Reason is logged server-side, not sent to client. |
| Conflict | 409 Conflict | `ProblemDetails` (RFC 7807) | Code and message included. |
| Failure | 500 Internal Server Error | `ProblemDetails` (RFC 7807) | Generic failure mapped as server error. |

## Custom HTTP Mapping

If you need different status codes, implement `IOperationOutcomeMapper` or override the default behavior:

```csharp
// Custom mapper for specific outcome types
public class CustomMapper : IOperationOutcomeMapper
{
    public IActionResult Map<TResponse>(TResponse outcome)
        where TResponse : IOperationOutcome
    {
        // Inspect outcome.IsSuccess, outcome.Failure, and map however you want
        
        if (outcome.IsSuccess)
        {
            return new OkObjectResult(/* ... */);
        }
        
        var failure = outcome.Failure;
        
        // Map specific failure types
        if (failure is CustomConflict conflict)
        {
            return new ConflictObjectResult(new { conflict.Code, conflict.Message });
        }
        
        // Fall back to default mapping for other cases
        return DefaultMapper.Map(outcome);
    }
}

// Register your custom mapper
services.AddSingleton<IOperationOutcomeMapper, CustomMapper>();
```

## Example Responses

### Success (Typed Command)

```csharp
// Command
public record CreateUser(string Name, string Email)
    : ICommand<OperationOutcome<int, ValidationError>>;

// Handler returns Success with new user ID
var result = await mediator.Send(new CreateUser("Alice", "alice@example.com"));

// Response: 200 OK
// Body: 42  (the user ID, as plain JSON)
```

### ValidationError

```csharp
// Command with invalid data
var result = await mediator.Send(new CreateUser("", "not-an-email"));

// Response: 400 Bad Request
// Body:
// {
//   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
//   "title": "One or more validation errors occurred.",
//   "status": 400,
//   "errors": {
//     "Name": ["'Name' must not be empty."],
//     "Email": ["'Email' is not a valid email address."]
//   }
// }
```

### NotFound

```csharp
// Query that finds nothing
var result = await mediator.Send(new GetUser(999));

// Response: 404 Not Found
// Body: (empty)
```

### Forbidden

```csharp
// Command rejected by authorization
var result = await mediator.Send(new DeleteOrder(42));  // Current user not authorized

// Response: 403 Forbidden
// Body: (empty, reason logged server-side)
```

### Conflict

```csharp
// Update with version mismatch
var result = await mediator.Send(new UpdateProduct(1, "New Name", 10));  // Old version

// Response: 409 Conflict
// Body:
// {
//   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
//   "title": "Conflict",
//   "status": 409,
//   "detail": "Concurrency version mismatch",
//   "instance": "/api/products/1"
// }
```

## Problem Details (RFC 7807)

Non-2xx responses follow RFC 7807 `ProblemDetails` format by default. ValidationError uses `ValidationProblemDetails` (extending ProblemDetails with an `errors` dictionary). This is a standard format that API clients can rely on for error parsing.

## Error Handling at the Edge

Exceptions thrown in handlers or behaviors (not converted to outcomes) are handled by ASP.NET Core's exception middleware, not by `ToActionResult()`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender mediator;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrder(request.CustomerId, request.Items);
        
        try
        {
            var result = await this.mediator.Send(command, ct);
            return result.ToActionResult();  // Maps outcomes to HTTP
        }
        catch (Exception ex)
        {
            // Unexpected exceptions (bugs) reach here and are handled by ASP.NET Core middleware
            // Configure middleware to return 500 + ProblemDetails
            throw;
        }
    }
}
```

## CORS & Content Negotiation

`ToActionResult()` respects ASP.NET Core's content negotiation. If the client requests JSON, you get JSON; if XML, you get XML (if configured). The outcome mapper respects this.

## Testing with IActionResult

Unit test your action results:

```csharp
[Fact]
public async Task CreateOrder_Valid_Returns200()
{
    var mediator = new Mock<ISender>();
    mediator.Setup(m => m.Send(It.IsAny<IQuery<IOperationOutcome>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new OperationOutcome<int>(42));  // Success with ID 42

    var controller = new OrdersController(mediator.Object);
    
    var result = await controller.CreateOrder(new CreateOrderRequest { /* ... */ }, default);
    
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.Equal(42, okResult.Value);
}

[Fact]
public async Task DeleteOrder_NotFound_Returns404()
{
    var mediator = new Mock<ISender>();
    mediator.Setup(m => m.Send(It.IsAny<ICommand<IOperationOutcome>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new OperationOutcome<Success, NotFound>(new NotFound()));

    var controller = new OrdersController(mediator.Object);
    
    var result = await controller.DeleteOrder(999, default);
    
    Assert.IsType<NotFoundResult>(result);
}
```
