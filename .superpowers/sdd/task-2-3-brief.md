# Task 2+3: Split Abstractions.cs and Behaviors.cs into subdirectory files

**Goal:** Create 30 new files in subdirectories under `src/Interlocutor.Abstractions/`, then delete `Abstractions.cs` and `Behaviors.cs`.

**Critical constraints - MUST follow exactly:**
1. ONLY create the files listed below. Do NOT touch any other file.
2. ONLY delete `Abstractions.cs` and `Behaviors.cs`. Do NOT delete anything else.
3. Do NOT modify any existing file.
4. Each file's namespace must match the subdirectory name (e.g. files in `Core/` → `namespace Interlocutor.Abstractions.Core;`)
5. Do NOT add, remove, or change any code logic - pure file extraction

The build will NOT pass after this task (imports in other files won't resolve until later tasks). Do NOT try to build.

## Step 1: Create subdirectories

Create these directories under `src/Interlocutor.Abstractions/`:
- `Core/`
- `Commands/`
- `Queries/`
- `Notifications/`
- `Pipeline/`
- `Markers/`

## Step 2: Create 22 files from Abstractions.cs

### Core/ (namespace `Interlocutor.Abstractions.Core`)

**Core/ISender.cs:**
```csharp
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Core;

/// <summary>Send-only interface for request/response operations. Exposes <see cref="Send{TResponse}(IQuery{TResponse},CancellationToken)"/> for queries and commands.</summary>
public interface ISender
{
    /// <summary>Dispatches a query to its registered handler, returning the result through all pipeline behaviors.</summary>
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

    /// <summary>Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors.</summary>
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

    /// <summary>Dispatches a void command to its registered handler, returning the result through all pipeline behaviors.</summary>
    Task<Result> Send(ICommand command, CancellationToken ct = default);
}
```

**Core/IPublisher.cs:**
```csharp
using Interlocutor.Abstractions.Notifications;

namespace Interlocutor.Abstractions.Core;

/// <summary>Publish-only interface for fire-and-forget notifications. Exposes <see cref="Publish{TNotification}(TNotification,CancellationToken)"/>.</summary>
public interface IPublisher
{
    /// <summary>Publishes a notification to all registered <see cref="INotificationHandler{TNotification}"/> instances.</summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}
```

**Core/IMediator.cs:**
```csharp
namespace Interlocutor.Abstractions.Core;

/// <summary>Combined send-and-publish interface. Exposes both <see cref="ISender"/> and <see cref="IPublisher"/>.</summary>
public interface IMediator : ISender, IPublisher;
```

**Core/PublishStrategy.cs:**
```csharp
namespace Interlocutor.Abstractions.Core;

/// <summary>Controls how notifications are dispatched to multiple handlers.</summary>
public enum PublishStrategy
{
    /// <summary>Handlers execute in registration order. Stops on the first exception.</summary>
    Sequential,

    /// <summary>Handlers execute concurrently via Task.WhenAll. Exceptions are aggregated.</summary>
    Parallel,
}
```

**Core/IRequestContextAccessor.cs:**
```csharp
namespace Interlocutor.Abstractions.Core;

/// <summary>Scoped ambient context carrying correlation ID, user identity, and tenant information through the request pipeline.</summary>
/// <remarks>Null-safe - if no middleware populates this, <see cref="Context"/> is null. Consumers must handle gracefully.</remarks>
public interface IRequestContextAccessor
{
    /// <summary>Gets or sets the current request context. May be null if not populated.</summary>
    RequestContext? Context { get; set; }
}
```

**Core/RequestContext.cs:**
```csharp
namespace Interlocutor.Abstractions.Core;

/// <summary>Immutable snapshot of request-scoped ambient data.</summary>
public sealed record RequestContext
{
    /// <summary>Gets a unique identifier for correlating this request across services. Auto-generated if not set.</summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the authenticated user identifier, or null if anonymous.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets the tenant identifier for multi-tenant scenarios, or null if not scoped.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets an arbitrary key-value bag for extension data, or null if not set.</summary>
    public IReadOnlyDictionary<string, object>? Items { get; init; }
}
```

### Commands/ (namespace `Interlocutor.Abstractions.Commands`)

**Commands/ICommand.cs:**
```csharp
namespace Interlocutor.Abstractions.Commands;

public interface ICommand<TResponse>;

public interface ICommand;
```

**Commands/IEnumerableCommand.cs:**
```csharp
namespace Interlocutor.Abstractions.Commands;

public interface IEnumerableCommand<TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>;
```

**Commands/IBulkCommand.cs:**
```csharp
namespace Interlocutor.Abstractions.Commands;

public interface IBulkCommand<TItem, TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>
{
    IReadOnlyList<TItem> Items { get; }
}
```

**Commands/ICommandHandler.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Commands;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken ct);
}
```

**Commands/ResourceCommandHandler.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Commands;

public abstract class ResourceCommandHandler<TCommand, TEntity, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected abstract Task<TEntity?> LoadAsync(TCommand command, CancellationToken ct);

    protected abstract Failure? Authorize(TEntity entity, TCommand command);

    protected abstract Task<Result<TResponse>> HandleAsync(TCommand command, TEntity entity, CancellationToken ct);

    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await this.LoadAsync(command, ct);
        if (entity is null)
        {
            return new Result<TResponse>(new NotFound(typeof(TEntity).Name, this.DescribeKey(command)));
        }

        var authorizationResult = this.Authorize(entity, command);

        if (authorizationResult is not null)
        {
            return new Result<TResponse>(authorizationResult.Value);
        }

        return await this.HandleAsync(command, entity, ct);
    }

    protected virtual object DescribeKey(TCommand command) => command!;
}
```

### Queries/ (namespace `Interlocutor.Abstractions.Queries`)

**Queries/IQuery.cs:**
```csharp
namespace Interlocutor.Abstractions.Queries;

public interface IQuery<TResponse>;
```

**Queries/IEnumerableQuery.cs:**
```csharp
namespace Interlocutor.Abstractions.Queries;

public interface IEnumerableQuery<TItem> : IQuery<IReadOnlyList<TItem>>;
```

**Queries/IPagedQuery.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Queries;

public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    int Page { get; }

    int PageSize { get; }
}
```

**Queries/IQueryHandler.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Queries;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}
```

### Notifications/ (namespace `Interlocutor.Abstractions.Notifications`)

**Notifications/INotification.cs:**
```csharp
namespace Interlocutor.Abstractions.Notifications;

/// <summary>Marker interface for domain events or notifications. Has no return value - fire-and-forget.</summary>
public interface INotification;
```

**Notifications/INotificationHandler.cs:**
```csharp
namespace Interlocutor.Abstractions.Notifications;

/// <summary>Handles a single notification type. Multiple handlers may register for the same <typeparamref name="TNotification"/>.</summary>
/// <typeparam name="TNotification">The notification type this handler consumes.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Processes the notification.</summary>
    Task Handle(TNotification notification, CancellationToken ct);
}
```

### Pipeline/ (namespace `Interlocutor.Abstractions.Pipeline`)

**Pipeline/IPipelineBehavior.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Pipeline;

public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct);
}
```

### Markers/ (namespace `Interlocutor.Abstractions.Markers`)

**Markers/ITransactionalRequest.cs:**
```csharp
namespace Interlocutor.Abstractions.Markers;

public interface ITransactionalRequest;
```

**Markers/IExternalStoreRequest.cs:**
```csharp
namespace Interlocutor.Abstractions.Markers;

public interface IExternalStoreRequest;
```

**Markers/IAuditableRequest.cs:**
```csharp
namespace Interlocutor.Abstractions.Markers;

public interface IAuditableRequest
{
    string ActionName { get; }
}
```

**Markers/IRequiresPolicy.cs:**
```csharp
namespace Interlocutor.Abstractions.Markers;

public interface IRequiresPolicy
{
    string PolicyName { get; }
}
```

## Step 3: Create 8 files from Behaviors.cs (all in Pipeline/)

All files use `namespace Interlocutor.Abstractions.Pipeline`.

**Pipeline/LoggingBehavior.cs:**
```csharp
using System.Diagnostics;
using Interlocutor.CommonKernel;
using Microsoft.Extensions.Logging;

namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Pipeline behavior that logs every mediator request passing through the pipeline.</summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        var start = Stopwatch.GetTimestamp();
        var requestType = typeof(TRequest).Name;

        Result<TResponse> result;
        try
        {
            result = await next();
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            logger.LogError(ex, "Request {RequestType} failed with exception after {ElapsedMs}ms", requestType, elapsed.TotalMilliseconds);
            throw;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var outcome = result.Value switch
        {
            TResponse => "Success",
            ValidationFailed => "ValidationFailed",
            RuleViolation => "RuleViolation",
            NotFound => "NotFound",
            Forbidden => "Forbidden",
            Conflict => "Conflict",
            _ => "Unknown",
        };

        logger.LogInformation("Request {RequestType} completed with {Outcome} in {ElapsedMs}ms", requestType, outcome, elapsedMs);
        return result;
    }
}
```

**Pipeline/AuditEntry.cs:**
```csharp
namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Represents a single audit record written by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
public record AuditEntry(string ActionName, string RequestType, string Outcome, DateTime Timestamp, string? UserId = null);
```

**Pipeline/IAuditSink.cs:**
```csharp
namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Persists audit records produced by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
public interface IAuditSink
{
    /// <summary>Writes an audit entry to the backing store.</summary>
    Task RecordAsync(AuditEntry entry, CancellationToken ct);
}
```

**Pipeline/AuditingBehavior.cs:**
```csharp
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Pipeline behavior that records an audit trail for requests implementing <see cref="IAuditableRequest"/>.</summary>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink) : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not IAuditableRequest auditable) return await next();

        var result = await next();
        var outcome = result.Value switch
        {
            TResponse => "Success",
            ValidationFailed => "ValidationFailed",
            RuleViolation => "RuleViolation",
            NotFound => "NotFound",
            Forbidden => "Forbidden",
            Conflict => "Conflict",
            _ => "Unknown",
        };

        var entry = new AuditEntry(auditable.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow);
        await sink.RecordAsync(entry, ct);
        return result;
    }
}
```

**Pipeline/IPolicyEvaluator.cs:**
```csharp
namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Minimal authorization abstraction that avoids coupling to ASP.NET Core's <c>IAuthorizationService</c>.</summary>
public interface IPolicyEvaluator
{
    /// <summary>Evaluates whether the request is authorized for the given policy.</summary>
    Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct);
}
```

**Pipeline/AuthorizationBehavior.cs:**
```csharp
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Pipeline behavior that enforces declarative authorization policies on requests implementing <see cref="IRequiresPolicy"/>.</summary>
public class AuthorizationBehavior<TRequest, TResponse>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not IRequiresPolicy requiresPolicy) return await next();

        var authorized = await policyEvaluator.EvaluateAsync(requiresPolicy.PolicyName, request, ct);
        if (!authorized)
            return new Result<TResponse>(new Forbidden($"Policy '{requiresPolicy.PolicyName}' denied access to {typeof(TRequest).Name}"));

        return await next();
    }
}
```

**Pipeline/IPipelineStage.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Lightweight pre/post hook interface without creating a full <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</summary>
public interface IPipelineStage<TRequest, TResponse>
{
    /// <summary>Invoked before the request enters the inner pipeline.</summary>
    Task OnBeforeAsync(TRequest request, CancellationToken ct);

    /// <summary>Invoked after the request has been handled and the result is available.</summary>
    Task OnAfterAsync(TRequest request, Result<TResponse> result, CancellationToken ct);
}
```

**Pipeline/PipelineStageBehavior.cs:**
```csharp
using Interlocutor.CommonKernel;

namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
public class PipelineStageBehavior<TRequest, TResponse>(IEnumerable<IPipelineStage<TRequest, TResponse>> stages)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        foreach (var stage in stages) await stage.OnBeforeAsync(request, ct);

        Result<TResponse> result;
        try
        {
            result = await next();
        }
        catch
        {
            foreach (var stage in stages) await stage.OnAfterAsync(request, new Result<TResponse>(), ct);
            throw;
        }

        foreach (var stage in stages) await stage.OnAfterAsync(request, result, ct);

        return result;
    }
}
```

## Step 4: Delete old files

Delete:
- `src/Interlocutor.Abstractions/Abstractions.cs`
- `src/Interlocutor.Abstractions/Behaviors.cs`
