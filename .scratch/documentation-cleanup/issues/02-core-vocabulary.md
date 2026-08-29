> **Status: done**

## Labels

ready-for-agent

## What to build

Pass over `documentation/glossary.md`, `documentation/index.md`, and `documentation/messages-and-result-types.md`:

- Remove stale language (`glossary.md`'s "no longer uses" framing, `messages-and-result-types.md`'s docs/ citation - already resolved by the unit-01 rename but confirm the sentence still reads cleanly once the guide it points at eventually moves in unit 07).
- Rebalance discriminated-union / `OperationOutcome` emphasis - `messages-and-result-types.md` currently mentions `OperationOutcome` ~60 times in 451 lines; keep the explanation of *why* the union-based result model exists and how to pick an arity, but stop treating exhaustiveness-checking prose as the file's main event once the vocabulary is established.
- Add inline `> [!WARNING]` alerts at first mention of `Cancelled`, `RateLimited`, and `Timeout` (all three `[Obsolete("May be overkill to include")]` in `Interlocutor.CommonKernel`) wherever `messages-and-result-types.md` lists the failure-case types.
- Add glossary-first-mention links from `messages-and-result-types.md` into `glossary.md` per the new CLAUDE.md House Style bullet, skipping terms the file already explains inline.
- Verify every claim in all three files against the actual `src/Interlocutor.CommonKernel` and `src/Interlocutor.Abstractions` types - fix anything that doesn't match.

## Acceptance criteria

- [x] No "old/previous/legacy" language remains in the three files
- [x] All three `[Obsolete]` result-case types carry an inline warning alert at first mention (`messages-and-result-types.md`'s is canonical; `glossary.md` cross-links to it)
- [x] `OperationOutcome`/discriminated-union prose reads as one useful tool among several, not the headline
- [x] First mentions of glossary terms link to `glossary.md`, without over-linking repeat mentions or in-context explanations
- [x] Every code example in all three files compiles against the current shipped API

## Findings beyond the original scope

Verifying the case-type code samples against `src/Interlocutor.CommonKernel` turned up real bugs, now fixed:

- `ValidationError` was shown with a `Code` parameter that doesn't exist on the base type (the error-code variant is the separate `ValidationError<TErrorCode>`), and the "aggregated" sample showed a positional-record shape `ValidationErrors(IReadOnlyList<ValidationError> Items)` that doesn't match the real type's constructors.
- `Conflict` was shown as `Conflict(string Code, string? Message = null)` - reversed parameter order and wrongly optional; the real type is `Conflict(string Message, string Code)`, both mandatory.
- `Success`, `NotFound`, and `Forbidden` were declared as `record` (reference type) in code samples; all three are actually `readonly record struct`.
- The `Forbidden` dispatch example asserted `Assert.IsAssignableFrom<Failure>(result.Value)`, which can't be true - `Forbidden` and `Failure` are unrelated types. Fixed to assert `Forbidden`.
- The "Marker Interfaces" table omitted `ICancellable`/`IHasTimeout`/`IRateLimited` despite the file's own preceding section covering the outcomes those markers drive - added the missing rows.
- `glossary.md`'s `InterlocutorPipelineBuilder` entry didn't mention `OverridePipelineFor` (shipped in the just-landed ADR-0019) - added.

