## Labels

ready-for-agent

## What to build

Implement `AuditingBehavior<TRequest, TResponse>` — a pipeline behavior that records an audit trail for requests implementing `IAuditableRequest`.

The `IAuditableRequest` marker interface already exists in `Mediance.Abstractions` with a load-bearing `ActionName` property. An `AuditingBehavior` should:

1. Check if `TRequest` implements `IAuditableRequest`
2. If yes, resolve an `IAuditSink` from DI and write an audit record with:
   - Action name (`request.ActionName`)
   - Request type
   - Timestamp
   - Outcome (success or which `Failure` variant)
   - Current user (resolved from `IRequestContextAccessor` or similar — may be null)
3. If the request doesn't implement `IAuditableRequest`, pass through
4. The `IAuditSink` should be defined in `Mediance.Abstractions` as a minimal interface:
   ```csharp
   public interface IAuditSink
   {
       Task RecordAsync(AuditEntry entry, CancellationToken ct);
   }
   ```
   with an `AuditEntry` record class for the payload.

Package placement: add related types to `Mediance.Abstractions` (IAuditSink, AuditEntry). The behavior itself may live in Abstractions or be opt-in — decide based on whether you want auditing always available.

## Acceptance criteria

- [ ] `AuditingBehavior<TRequest, TResponse>` implements `IPipelineBehavior<TRequest, TResponse>`
- [ ] Writes audit record via `IAuditSink` for `IAuditableRequest` requests
- [ ] Passes through for non-auditable requests
- [ ] Works for both typed commands/queries and void `ICommand` requests (depends on Slice 08)
- [ ] All existing tests still pass

## Blocked by

- 08-fix-void-command-dispatch (so void commands also receive auditing)
