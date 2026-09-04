# 06 - Give `ResourceCommandHandler` a `Forbidden` case

**What to build:** A denied resource-authorization check in `ResourceCommandHandler` surfaces as `Forbidden`, consistent with `AuthorizationBehavior`'s equivalent denial and with CLAUDE.md's result-type convention — not as unclassified `Failure`. Ships as a versioned breaking change following the repo's existing `Outcome`-arity deprecation path.

Full technical detail: [findings/03-resourcecommandhandler-failure-vs-forbidden.md](../findings/03-resourcecommandhandler-failure-vs-forbidden.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `ResourceCommandHandler<TCommand, TEntity, TSuccess>`'s fixed shape includes `Forbidden` (e.g. `Outcome<TSuccess, NotFound, Forbidden>`, or with `Failure` added back if a genuinely-unclassified case is still needed)
- [x] `Authorize` returns `Forbidden?` instead of `Failure?`
- [x] The change follows the repo's existing deprecation/versioning path for breaking `Outcome`-arity changes
- [x] `documentation/handler-selection-and-dispatch.md`'s description of `ResourceCommandHandler`'s fixed shape is updated to match

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. ResourceCommandHandler<TCommand, TEntity, TSuccess>'s fixed shape is now Outcome<TSuccess, NotFound, Forbidden> (was ...Failure); Authorize returns Forbidden?. Landed directly as a breaking change per this repo's pre-1.0 no-shim precedent. documentation/handler-selection-and-dispatch.md reflects the new shape. No source changes were needed this session.
