# 08 - Spike: determine test-attribute awareness, then enforce the suffix on test-support methods

**What to build:** A concrete, verified answer to how the `Async` suffix rule gets enforced for test-support code (helpers, fixtures, factories, local functions) without false-positiving on `[Fact]`/`[Theory]` test methods, which are intentionally exempt - plus the enforcement itself, whichever mechanism the spike lands on.

**Blocked by:** 05 - Migrate tests/ fakes and doubles.

**Status:** ready-for-agent

- [ ] It's determined whether `VSTHRD200` already excludes `[Fact]`/`[Theory]`-attributed methods from its check, and this finding is recorded (e.g. as a comment on this ticket) with what was actually tested, not just read from documentation.
- [ ] If it does exclude them: `VSTHRD200` is enabled for test projects too, and verified by deliberately introducing a temporary un-suffixed async helper method (not a `[Fact]`/`[Theory]`) to confirm it gets flagged, then removing the temporary method once confirmed.
- [ ] If it does not exclude them: an alternative enforcement approach is chosen (e.g. analyzer stays off in test projects, rule enforced by review/the test-sweep skill) and the reasoning is recorded.
- [ ] Whichever branch applies, the resulting mechanism is unambiguous enough for ticket 09 to state the tests/ enforcement approach as settled fact, not as an open question.
