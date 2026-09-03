# 06 - Give `ResourceCommandHandler` a `Forbidden` case

**What to build:** A denied resource-authorization check in `ResourceCommandHandler` surfaces as `Forbidden`, consistent with `AuthorizationBehavior`'s equivalent denial and with CLAUDE.md's result-type convention — not as unclassified `Failure`. Ships as a versioned breaking change following the repo's existing `Outcome`-arity deprecation path.

Full technical detail: [findings/03-resourcecommandhandler-failure-vs-forbidden.md](../findings/03-resourcecommandhandler-failure-vs-forbidden.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `ResourceCommandHandler<TCommand, TEntity, TSuccess>`'s fixed shape includes `Forbidden` (e.g. `Outcome<TSuccess, NotFound, Forbidden>`, or with `Failure` added back if a genuinely-unclassified case is still needed)
- [ ] `Authorize` returns `Forbidden?` instead of `Failure?`
- [ ] The change follows the repo's existing deprecation/versioning path for breaking `Outcome`-arity changes
- [ ] `documentation/handler-selection-and-dispatch.md`'s description of `ResourceCommandHandler`'s fixed shape is updated to match
