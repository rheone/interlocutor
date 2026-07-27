using System.Diagnostics;
using Mediance.CommonKernel;
using Microsoft.Extensions.Logging;

namespace Mediance.Abstractions;

/// <summary>Pipeline behavior that logs every mediator request passing through the pipeline.</summary>
/// <remarks>Logs request type name, duration, and outcome (success or failure variant). Uses <see cref="ILogger{T}"/>.</remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type wrapped in <see cref="Result{TResponse}"/>.</typeparam>
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
            logger.LogError(
                ex,
                "Request {RequestType} failed with exception after {ElapsedMs}ms",
                requestType,
                elapsed.TotalMilliseconds
            );
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

        logger.LogInformation(
            "Request {RequestType} completed with {Outcome} in {ElapsedMs}ms",
            requestType,
            outcome,
            elapsedMs
        );

        return result;
    }
}

/// <summary>Represents a single audit record written by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
/// <param name="ActionName">The auditable action name from <see cref="IAuditableRequest.ActionName"/>.</param>
/// <param name="RequestType">The CLR type name of the request.</param>
/// <param name="Outcome">The result outcome: Success or one of the 5 Failure variant names.</param>
/// <param name="Timestamp">When the audited operation completed (UTC).</param>
/// <param name="UserId">Optional user identifier from the ambient context.</param>
public record AuditEntry(string ActionName, string RequestType, string Outcome, DateTime Timestamp, string? UserId = null);

/// <summary>Persists audit records produced by <see cref="AuditingBehavior{TRequest,TResponse}"/>.</summary>
public interface IAuditSink
{
    /// <summary>Writes an audit entry to the backing store.</summary>
    Task RecordAsync(AuditEntry entry, CancellationToken ct);
}

/// <summary>Pipeline behavior that records an audit trail for requests implementing <see cref="IAuditableRequest"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type wrapped in <see cref="Result{TResponse}"/>.</typeparam>
public class AuditingBehavior<TRequest, TResponse>(IAuditSink sink) : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not IAuditableRequest auditable)
        {
            return await next();
        }

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

/// <summary>Minimal authorization abstraction that avoids coupling to ASP.NET Core's <c>IAuthorizationService</c>.</summary>
/// <remarks>Consumers create a bridge adapter to delegate to ASP.NET Core or a custom authorization system.</remarks>
public interface IPolicyEvaluator
{
    /// <summary>Evaluates whether the request is authorized for the given policy.</summary>
    /// <param name="policyName">The policy name from <see cref="IRequiresPolicy.PolicyName"/>.</param>
    /// <param name="request">The mediator request being evaluated.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if authorized; false to short-circuit with <see cref="Forbidden"/>.</returns>
    Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct);
}

/// <summary>Pipeline behavior that enforces declarative authorization policies on requests implementing <see cref="IRequiresPolicy"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type wrapped in <see cref="Result{TResponse}"/>.</typeparam>
public class AuthorizationBehavior<TRequest, TResponse>(IPolicyEvaluator policyEvaluator)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not IRequiresPolicy requiresPolicy)
        {
            return await next();
        }

        var authorized = await policyEvaluator.EvaluateAsync(requiresPolicy.PolicyName, request, ct);

        if (!authorized)
        {
            return new Result<TResponse>(
                new Forbidden($"Policy '{requiresPolicy.PolicyName}' denied access to {typeof(TRequest).Name}")
            );
        }

        return await next();
    }
}

/// <summary>Lightweight pre/post hook interface. Consumers implement <see cref="OnBeforeAsync"/> and/or <see cref="OnAfterAsync"/> without creating a full <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type wrapped in <see cref="Result{TResponse}"/>.</typeparam>
public interface IPipelineStage<TRequest, TResponse>
{
    /// <summary>Invoked before the request enters the inner pipeline.</summary>
    Task OnBeforeAsync(TRequest request, CancellationToken ct);

    /// <summary>Invoked after the request has been handled and the result is available.</summary>
    Task OnAfterAsync(TRequest request, Result<TResponse> result, CancellationToken ct);
}

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type wrapped in <see cref="Result{TResponse}"/>.</typeparam>
public class PipelineStageBehavior<TRequest, TResponse>(IEnumerable<IPipelineStage<TRequest, TResponse>> stages)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        foreach (var stage in stages)
        {
            await stage.OnBeforeAsync(request, ct);
        }

        Result<TResponse> result;
        try
        {
            result = await next();
        }
        catch
        {
            foreach (var stage in stages)
            {
                await stage.OnAfterAsync(request, new Result<TResponse>(), ct);
            }
            throw;
        }

        foreach (var stage in stages)
        {
            await stage.OnAfterAsync(request, result, ct);
        }

        return result;
    }
}
