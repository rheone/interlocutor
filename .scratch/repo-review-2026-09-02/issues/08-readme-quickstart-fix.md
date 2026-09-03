# 08 - Fix README.md Quick Start example

**What to build:** The Quick Start's pattern-match code sample compiles against the shipped API.

Full technical detail: [findings/06-readme-quickstart-broken-example.md](../findings/06-readme-quickstart-broken-example.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `Outcome.SuccessCase<UserDto>` is replaced with `UserDto` in the switch pattern
- [ ] The switch continues to be over `result` directly (not `result.Value`) and stays discard-free, matching `Outcome<TSuccessResult, TFailureResult>`'s documented exhaustive-switch behavior
- [ ] The example, run as written, actually compiles
