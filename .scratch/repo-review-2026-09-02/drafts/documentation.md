# Documentation Lane Findings

## [HIGH] README.md Quick Start pattern-match example uses a non-existent `Outcome.SuccessCase<T>` type and switches on the wrong expression
- File: README.md:74-81
- Category: documentation
- Summary: The Quick Start's final code sample would not compile against the shipped API - it pattern-matches on a fictitious `Outcome.SuccessCase<T>` wrapper type and switches on `result` instead of `result.Value`.
- Detail: README.md shows:
  ```csharp
  var message = result switch
  {
      Outcome.SuccessCase<UserDto> success => $"Found: {success.Value.Name}",
      NotFound notfound => $"Not found: {notfound.Message}",
      _ => "Unexpected"
  };
  ```
  There is no `Outcome.SuccessCase<T>` type anywhere in `src/` (confirmed via `rg -n "SuccessCase" src/Interlocutor.CommonKernel`). `Outcome<TSuccessResult, TFailureResult>` (`src/Interlocutor.CommonKernel/Outcome.cs`) is a real C# union whose case values are the bare types themselves (`UserDto`, `NotFound`), not wrapped in a `SuccessCase<T>` shim. `documentation/messages-and-result-types.md`'s own "Pattern Matching" section (and every test in `tests/Interlocutor.CommonKernel.Tests/OutcomeOfTwoCasesTests.cs`, e.g. `Switch_OverSuccess_MatchesSuccessCase`) shows the correct, compiling form: `result.Value switch { UserDto user => ..., NotFound => ... }` - switching on the union's `.Value` property, with bare case types, not on the union instance itself and not on an `Outcome.SuccessCase<T>` wrapper. This is the first, most-visible code example a new reader sees and it would fail to compile as written.
- Suggested action: Rewrite the switch to match the pattern documented in `messages-and-result-types.md` and used throughout the test suite: `result.Value switch { UserDto user => $"Found: {user.Name}", NotFound notfound => $"Not found: {notfound.Message}" }` (and drop the `_ => "Unexpected"` arm, since the switch is already compiler-exhaustive over the two case types).

## [MEDIUM] pipeline-and-behaviors.md describes execution order using fictitious `OnIngress()`/`OnEgress()` methods that don't exist on `IPipelineBehavior`
- File: documentation/pipeline-and-behaviors.md:36-37
- Category: documentation
- Summary: The "Flat Ordered List" section narrates pipeline execution as calls to `Logging.OnIngress()`/`Transaction.OnEgress()` etc., but `IPipelineBehavior<TRequest, TResponse>` has no such members - only a single `Handle(request, next, ct)` method.
- Detail: The doc reads:
  ```
  1. **Inbound:** `Logging.OnIngress()` → `Auditing.OnIngress()` → `Validation.OnIngress()` → `Transaction.OnIngress()` → handler
  2. **Outbound:** handler result flows back out; behaviors run their egress in reverse order: `Transaction.OnEgress()` → `Validation.OnEgress()` → `Auditing.OnEgress()` → `Logging.OnEgress()`
  ```
  `rg -rn "OnIngress|OnEgress" src/` returns nothing - these methods don't exist anywhere in the codebase. `src/Interlocutor.Abstractions/Pipeline/IPipelineBehavior.cs` defines exactly one member: `Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)`. The same file's own later "Composition Diagram" section (a sequence diagram added by a prior doc-cleanup ticket, `.scratch/di-builder-and-discovery/issues/42-pipeline-composition-diagram-to-sequence.md`) and "Registering Custom Behaviors" section both correctly describe/show the real `Handle`/`next()` shape - this leftover prose from before that rewrite is the one inconsistent, inaccurate spot left in the file, presented in inline code formatting that reads as real method calls.
- Suggested action: Rewrite the two bullets to describe inbound/outbound flow in terms of the actual `Handle(request, next, ct)` contract (each behavior calling `next()` on the way in, then acting on the awaited result on the way out) instead of inventing `OnIngress`/`OnEgress` method names.

## [LOW] glossary.md still cites ADR-0008 directly, missed by the prior ADR-citation sweep
- File: documentation/glossary.md:201
- Category: documentation
- Summary: The `Interlocutor.Generator` glossary entry cites "(ADR-0008)" in prose, violating CLAUDE.md's "No internal design-record citations" house-style rule.
- Detail: Line 201 reads: `**Interlocutor.Generator** | The Roslyn incremental source generator behind compile-time handler discovery (ADR-0008). An analyzer reference pulled in by Abstractions; never ships in build output.` CLAUDE.md's House Style Content Rules explicitly forbid this pattern ("never cite or link to `maintainer_docs/adr/`... to justify a design choice... State the rationale itself, as fact"). A prior sweep (`.scratch/di-builder-and-discovery/issues/36-sweep-strip-internal-citations-and-fix-diagram.md`, status done) stripped ADR citations from `documentation/*.md` including two other glossary rows (`glossary.md:17`'s `IOperationOutcome` row and `glossary.md:81`'s `IMediator` row) but missed this one in the "Packages" section - it's the sole remaining `ADR-` reference anywhere under `documentation/` (verified via `rg -rn "ADR-" documentation/`).
- Suggested action: Delete the "(ADR-0008)" parenthetical; the sentence already states the rationale ("behind compile-time handler discovery") without needing the citation, matching how the sweep handled the other glossary rows.

## [LOW] maintainer_docs/README.md links to a PRD.md section heading that no longer exists
- File: maintainer_docs/README.md:63
- Category: documentation
- Summary: The "Historical: bootstrapreference/" note links to `PRD.md#15-comparison-with-bootstrap-prd`, but PRD.md's section 15 is now titled "Architecture decisions" with no "Comparison with Bootstrap PRD" heading.
- Detail: `maintainer_docs/README.md:63` reads: "...has been reconciled into [PRD.md §15](PRD.md#15-comparison-with-bootstrap-prd), which is the current source of truth." `maintainer_docs/PRD.md`'s actual section 15 heading (line 180) is `## 15. Architecture decisions`, which only briefly mentions `bootstrapreference/` in a sentence of prose, not under a "Comparison with Bootstrap PRD" heading anywhere in the file (`rg -in "bootstrap" maintainer_docs/PRD.md` returns only that one line). The anchor `#15-comparison-with-bootstrap-prd` resolves to nothing, so the link 404s on GitHub.
- Suggested action: Point the link at the real heading (`PRD.md#15-architecture-decisions`), or update the surrounding sentence if section 15's content has moved/been retitled since this line was written.
