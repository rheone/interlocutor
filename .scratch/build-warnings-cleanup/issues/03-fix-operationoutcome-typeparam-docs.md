# 03 - Fix RCS1263 stale typeparam docs in OperationOutcome.cs

**What to build:** `OperationOutcome.cs` in `Interlocutor.CommonKernel` reports 20 RCS1263 warnings ("type parameter could not be found") - its XML doc `<typeparam>` tags reference generic parameter names (e.g. `TSuccessResult`, `TFailureResult`, `TCase2`, `TCase3`) that don't match the type's actual declared generic parameters, most likely left behind after a rename. Correct every `<typeparam>` tag across the `OperationOutcome<...>` arities in that file so each documented name matches the real declared type parameter, and the doc text still accurately describes what that parameter represents.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `dotnet build` reports zero RCS1263 warnings for `src/Interlocutor.CommonKernel/OperationOutcome.cs`
- [x] Every `<typeparam name="...">` in the file matches an actual generic parameter on that declaration
- [x] No generic parameter names themselves were changed - only the doc tags were corrected to match the existing code
- [x] `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` still passes (166/166)

## Comments

Investigation found every `<typeparam>` tag in the file already matches its declaration's actual generic parameter name exactly (e.g. `OperationOutcome<TSuccessResult, TFailureResult>` documents `TSuccessResult` and `TFailureResult` - no stale name anywhere). RCS1263 still fires on all of them because Roslynator's analyzer does not resolve type parameter lists declared on the preview C# `union` syntax this file uses (`public readonly union OperationOutcome<...>(...)`) - a tooling limitation with the preview feature, not a doc bug to rename around. Fixed with a file-scoped `#pragma warning disable/restore RCS1263`, documented inline, rather than renaming parameters that were already correct.
