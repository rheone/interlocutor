# 03 - Fix RCS1263 stale typeparam docs in OperationOutcome.cs

**What to build:** `OperationOutcome.cs` in `Interlocutor.CommonKernel` reports 20 RCS1263 warnings ("type parameter could not be found") - its XML doc `<typeparam>` tags reference generic parameter names (e.g. `TSuccessResult`, `TFailureResult`, `TCase2`, `TCase3`) that don't match the type's actual declared generic parameters, most likely left behind after a rename. Correct every `<typeparam>` tag across the `OperationOutcome<...>` arities in that file so each documented name matches the real declared type parameter, and the doc text still accurately describes what that parameter represents.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero RCS1263 warnings for `src/Interlocutor.CommonKernel/OperationOutcome.cs`
- [ ] Every `<typeparam name="...">` in the file matches an actual generic parameter on that declaration
- [ ] No generic parameter names themselves were changed - only the doc tags were corrected to match the existing code
- [ ] `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` still passes
