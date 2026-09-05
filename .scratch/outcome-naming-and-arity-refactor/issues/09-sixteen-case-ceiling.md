# 09 - Enforce a 16-case hard ceiling on attributed unions

**What to build:** Today `OutcomeMemberGenerator` only enforces a floor - `INTERLOC011` errors on an attributed union with fewer than two case types - there is no ceiling, even though one already exists in practice: `Match<TResult>`/`Switch`'s generated signatures are built from the BCL's `System.Func<T1..T16, TResult>`/`System.Action<T1..T16>` delegate families, which stop at 16 input type parameters. An attributed union past 16 case types would fail deep inside the generated code with a confusing error (no `Func<>`/`Action<>` overload wide enough to bind to), rather than a clear message at the declaration site.

Add a new build error, `INTERLOC013`, raised by `OutcomeMemberGenerator` when an attributed `partial union` declares more than 16 case types. State the actual reason in the diagnostic's message/description - the ceiling comes from the BCL's `Func`/`Action` family topping out at 16 type parameters, not an arbitrary library restriction - so a reader understands why 16 and not some other number.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `OutcomeMemberGenerator` reports `INTERLOC013` (build error) for any attributed union with more than 16 case types, mirroring `INTERLOC011`'s shape in `InterlocutorDiagnostics.cs`.
- [ ] The diagnostic's message/description names the actual reason (`Func<T1..T16,TResult>`/`Action<T1..T16>`'s 16-parameter ceiling), not just "too many cases."
- [ ] New tests cover a 16-case attributed union succeeding and a 17-case attributed union producing `INTERLOC013`.
- [ ] `documentation/messages-and-result-types.md`'s `## Choosing Arities` section and/or `documentation/extensibility.md`'s custom-arity walkthrough notes the hard 16-case ceiling and why it exists.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
