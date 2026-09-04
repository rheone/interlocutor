# 08 - Fix README.md Quick Start example

**What to build:** The Quick Start's pattern-match code sample compiles against the shipped API.

Full technical detail: [findings/06-readme-quickstart-broken-example.md](../findings/06-readme-quickstart-broken-example.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `Outcome.SuccessCase<UserDto>` is replaced with `UserDto` in the switch pattern
- [x] The switch continues to be over `result` directly (not `result.Value`) and stays discard-free, matching `Outcome<TSuccessResult, TFailureResult>`'s documented exhaustive-switch behavior
- [x] The example, run as written, actually compiles

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. README.md's Quick Start now switches directly over `result` with `UserDto success`/`NotFound notfound` arms (the never-existent `Outcome.SuccessCase<UserDto>` is gone), discard-free. No source changes were needed this session.
