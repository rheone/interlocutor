# 02 - Rename AspNetCore test fixture RateLimited to CustomRateLimited

## Labels

ready-for-agent

**What to build:** `tests/Interlocutor.AspNetCore.Tests/RateLimited.cs` currently declares a test-local `Interlocutor.AspNetCore.Tests.RateLimited` type used as a stand-in "consumer custom failure type" example, and several test files in that namespace `using Interlocutor.CommonKernel.ResultTypes;`. A forthcoming kernel `RateLimited` type (see ticket 05) will make the bare `RateLimited` symbol ambiguous in those files. Rename the test fixture to `CustomRateLimited` now, as a prefactor, so the collision never happens.

**Blocked by:** None - can start immediately

- [ ] `tests/Interlocutor.AspNetCore.Tests/RateLimited.cs` is renamed (file and type) to `CustomRateLimited.cs` / `CustomRateLimited`
- [ ] All references updated: `TeapotOutcomeStatusMapper.cs`, `DefaultOutcomeStatusMapperTests.cs`, `MinimalApiResultExtensionsTests.cs`, `ResultExtensionsTests.cs`, and any other file referencing the old name
- [ ] `dotnet build` and `dotnet test tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` pass with no behavior change
