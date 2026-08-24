# 04 - Fix ordering, dispose-safety, and comparison warnings in the remaining test/sample projects

**What to build:** Clean up a scattered set of small warnings outside `Interlocutor.Abstractions` and `Interlocutor.CommonKernel` (already covered by tickets 02 and 03):

- SA1201 (elements should appear in the correct order) - `tests/Interlocutor.AspNetCore.Tests` (1), `tests/Interlocutor.DependencyInjection.Abstractions.Tests` (1), `tests/Interlocutor.DependencyInjection.Autofac.Tests` (2)
- SA1137 (elements should have the same indentation) - `samples/Interlocutor.Sample.Handlers` (1), `src/Interlocutor.DependencyInjection.Abstractions` (1)
- CS0252 (possible unintended reference comparison - left side needs cast) - `tests/Interlocutor.Abstractions.Tests` (1). Read the surrounding assertion carefully: this may mean the test isn't actually verifying value equality where it claims to - fix the comparison to express what the test actually intends, not just add a cast to silence the warning.
- CA2000 (dispose object before losing scope) - `src/Interlocutor.DependencyInjection.Autofac` (1), `tests/Interlocutor.DependencyInjection.Microsoft.Tests` (1). Confirm whether the undisposed object is a real resource leak (e.g. a container/scope not disposed on an early-return or exception path) before choosing the fix - wrap in `using`/`try`-`finally` or restructure ownership as appropriate, don't just suppress.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero SA1201, SA1137, CS0252, and CA2000 warnings for the projects listed above
- [ ] The CS0252 fix reflects the comparison the test actually intends (verified by reading the assertion's context, not a blind cast)
- [ ] The CA2000 fixes dispose the flagged objects on every code path, including early returns and exceptions
- [ ] `dotnet test` passes for every affected test project
