Status: needs-triage
Severity: LOW
Category: convention
Source: tests-conventions.md lane, confirmed by antagonist lane

# csharpier can't parse the `partial union` files, and CI never runs a format check at all

## Summary

`dotnet csharpier check .` (the formatter CLAUDE.md names) fails to parse `Outcome.cs` and `PaymentOutcome.cs` with CS1001/CS1018/CS1002/CS1022 syntax errors, because its bundled parser doesn't recognize the C# 14 preview `partial union`/`union` declaration syntax these files use. Separately, `.github/workflows/ci.yml` doesn't invoke csharpier at all, so formatting drift in the other ~497 files isn't caught by CI either — only by manual developer discipline.

## Evidence

Reproduced directly: `dotnet csharpier check src/Interlocutor.CommonKernel/Outcome.cs` fails at lines 112 and 143, the highest-arity `union` declarations. This is a csharpier parser limitation with preview union syntax, not real formatting drift in those files. `.github/workflows/ci.yml` has no csharpier/format-check step in either job.

## Suggested action

Track a csharpier version bump once union-type parsing support lands upstream. In the meantime, consider a CI step running `dotnet csharpier check .` (accepting it will need to skip or tolerate failure on the union-syntax files until upstream support exists) so formatting drift in the other files stops depending solely on manual discipline.
