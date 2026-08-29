> **Status: done**

## Labels

ready-for-agent

## What to build

`documentation/pipeline-and-behaviors.md`'s `AuthorizationBehavior` section says "Requires: nothing (you register your own `IPolicyEvaluator` implementation)" but never mentions what happens if you don't: `DefaultPolicyEvaluator` is try-added and **always returns `true`, granting every policy check**. Its own XML doc is explicit that this is dev/test-only and must be replaced before production. That's a silent-allow-all default with zero documentation coverage today.

Add an inline `> [!WARNING]` at the point `AuthorizationBehavior` is introduced (or immediately after its "Requires" line) stating: if no `IPolicyEvaluator` is registered, `DefaultPolicyEvaluator` is used and grants every policy check unconditionally - suitable for development/testing only, must be overridden with a real evaluator before production.

## Acceptance criteria

- [x] `DefaultPolicyEvaluator`'s allow-all behavior is stated explicitly, with a warning alert, in `pipeline-and-behaviors.md`
- [x] The warning is discoverable from the `AuthorizationBehavior` section without a forward reference the reader has to chase

## Findings beyond the original scope

Found the same invalid code while adding the warning: `ICommand<Success>` appears three times across `pipeline-and-behaviors.md` (×2: `AuthorizationBehavior`, `AuditingBehavior`) and `transactions-and-store-integration.md` (×1). `Success` doesn't implement `IOperationOutcome` - only the `OperationOutcome`/`OperationOutcome<...>` unions do - so `ICommand<TResponse>`'s `where TResponse : IOperationOutcome` constraint rejects it; none of the three would compile. Fixed all three to `ICommand<OperationOutcome>`. Also fixed `AuditingBehavior`'s "Requires: `IAuditSink` implementation (you register your own)" line, which was wrong the same way `AuthorizationBehavior`'s was - it falls back to a try-added `LoggingAuditSink` default (benign - just logs - unlike `DefaultPolicyEvaluator`), not a hard requirement.
