# 08 - Spike: determine test-attribute awareness, then enforce the suffix on test-support methods

**What to build:** A concrete, verified answer to how the `Async` suffix rule gets enforced for test-support code (helpers, fixtures, factories, local functions) without false-positiving on `[Fact]`/`[Theory]` test methods, which are intentionally exempt - plus the enforcement itself, whichever mechanism the spike lands on.

**Blocked by:** 05 - Migrate tests/ fakes and doubles.

**Status:** ready-for-agent

- [x] It's determined whether `VSTHRD200` already excludes `[Fact]`/`[Theory]`-attributed methods from its check, and this finding is recorded (e.g. as a comment on this ticket) with what was actually tested, not just read from documentation.
- [x] If it does exclude them: `VSTHRD200` is enabled for test projects too, and verified by deliberately introducing a temporary un-suffixed async helper method (not a `[Fact]`/`[Theory]`) to confirm it gets flagged, then removing the temporary method once confirmed.
- [ ] If it does not exclude them: an alternative enforcement approach is chosen (e.g. analyzer stays off in test projects, rule enforced by review/the test-sweep skill) and the reasoning is recorded.
- [x] Whichever branch applies, the resulting mechanism is unambiguous enough for ticket 09 to state the tests/ enforcement approach as settled fact, not as an open question.

**Status:** done - implemented in `<pending commit>`.

## Comments

Spike finding, verified experimentally (not just read from docs): with `VSTHRD200` raised to `Warning`, a temporary unsuffixed async `[Fact]` method drew no `VSTHRD200` warning, while a temporary unsuffixed async helper method and a temporary unsuffixed async local function (both non-`[Fact]`/`[Theory]`) both did get flagged. So `VSTHRD200` already excludes `[Fact]`/`[Theory]`-attributed methods with no configuration needed - only test-support code (helpers, fixtures, factories, local functions) needed enforcing.

Branch taken: the "does exclude them" branch. `VSTHRD200` is now `Warning` for `tests/` via its own scoped `.editorconfig` section (mirroring the `src/`/`TutorialProject` section from ticket 07, with its own rationale comment recording this spike). A solution-wide sweep found and renamed every pre-existing unsuffixed async test-support helper/local function the newly-enforced rule legitimately caught (e.g. `TimeoutBehaviorTests`' `Next`→`NextAsync`, `GeneratorTestHarness.RunAnalyzer`→`RunAnalyzerAsync`, `AmbiguousClosureTests`' `IClosureTestExportStrategy.Export`→`ExportAsync`, `DeleteUserThenFailHandler.Handle`→`HandleAsync`), with all call sites updated. A clean solution build now shows zero `VSTHRD200` diagnostics anywhere, including `tests/`.

This settles the tests/ enforcement approach as fact for ticket 09: `VSTHRD200` is enforced (`Warning`) uniformly across `src/`, `TutorialProject`, and `tests/` alike, with no test-attribute carve-out required.
