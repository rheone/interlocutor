# 10 - Configurable "likely doing too much" warning for large arities

**What to build:** Add a new analyzer warning, `INTERLOC014`, firing on any attributed union whose case-type count exceeds a configurable threshold (default 6 - matching the shipped family's own ceiling and the existing `## Choosing Arities` guidance in `documentation/messages-and-result-types.md`). The message names the offending type and states, in effect, that a type/handler needing this many cases is usually a sign it's doing too much - nudging toward splitting the operation rather than growing the union further. The threshold is configurable via an `.editorconfig`-style analyzer option (e.g. `interlocutor_max_recommended_outcome_arity`), consistent with this repo's existing `dotnet_diagnostic.INTERLOCxxx.severity` configuration convention. As a normal Roslyn diagnostic it's already dismissible per-declaration via `#pragma warning disable INTERLOC014` / `#pragma warning restore INTERLOC014`, or suppressed repo-wide via severity configuration - no new suppression mechanism needed.

Update `documentation/extensibility.md`'s existing seven-case `PaymentOutcome` custom-arity union sample (under "### Custom-arity outcome unions: `GenerateOutcomeMembersAttribute`" - distinct from the earlier hand-rolled `PaymentOutcome` class under "## Custom `IOutcome` implementations") to wrap its declaration in the disable/restore pragma pair, since it would otherwise trip this warning by default - demonstrating the dismissal is itself part of the worked example.

**Blocked by:** Ticket 09 (same diagnostics-file area - sequencing avoids collision, not a hard technical dependency).

**Status:** ready-for-agent

- [ ] `OutcomeMemberGenerator` reports `INTERLOC014` (warning, enabled by default) for any attributed union whose case count exceeds the configured threshold (default 6).
- [ ] The threshold is readable from an `.editorconfig` analyzer option; changing it changes the point at which the warning fires, verified by a test.
- [ ] The diagnostic message names the union type and states that this many cases usually signals the type/handler is doing too much.
- [ ] `#pragma warning disable INTERLOC014` / `restore` around a declaration suppresses it; verified by a test.
- [ ] `documentation/extensibility.md`'s custom-arity `PaymentOutcome` sample gains the disable/restore pragma pair around its declaration and a short note on why.
- [ ] `documentation/messages-and-result-types.md`'s `## Choosing Arities` section cross-references the new warning instead of only describing the convention in prose.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
