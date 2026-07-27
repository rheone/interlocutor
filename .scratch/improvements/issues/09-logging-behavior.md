## Labels

ready-for-agent

## What to build

Implement `LoggingBehavior<TRequest, TResponse>` — a pipeline behavior that logs every mediator request passing through the pipeline.

For each request, log:
- Request type name
- Handler type name (inferred from resolved handler)
- Duration (start time before `next()`, stop time after)
- Outcome (success or which `Failure` variant)

The behavior should be generic over `TRequest` and `TResponse` so it works with both queries and commands. It should use `Microsoft.Extensions.Logging.ILogger<T>`.

Package placement: add to `Mediance.Abstractions` or create a new `Mediance.Logging` package — pick based on whether you want logging to be always-present vs opt-in. The default composition in the design docs shows it as one of several opt-in behaviors.

## Acceptance criteria

- [ ] `LoggingBehavior<TRequest, TResponse>` implements `IPipelineBehavior<TRequest, TResponse>`
- [ ] Logs request type, handler type, duration, and outcome
- [ ] Works for both `ICommand<TResponse>` and `IQuery<T>` requests
- [ ] Works for void `ICommand` requests (depends on Slice 08 fix)
- [ ] Does not interfere with request handling or change return values
- [ ] All existing tests still pass

## Blocked by

- 08-fix-void-command-dispatch (so void commands also receive logging)
