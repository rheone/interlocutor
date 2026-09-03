Status: needs-triage
Severity: MEDIUM
Category: architecture
Source: antagonist lane's independent gap-hunt (no lane examined build configuration)

# `global.json`'s `rollForward: latestMajor` on a preview-only SDK is a build-stability risk

## Summary

The solution pins `"version": "11.0.100-preview.6.26359.118"` but sets `"rollForward": "latestMajor"`, meaning any machine (a future CI runner, a contributor's later `dotnet` install) with a *newer major* SDK installed will silently build with that SDK instead. This is a real risk specifically because this repo's core type (`Outcome`) depends on the still-preview `union` declaration syntax, whose availability and exact codegen are exactly the kind of thing that could change across a major SDK bump while still preview.

## Evidence

This environment already has two SDK majors installed side by side (`10.0.400` and `11.0.100-preview.6.26359.118`, via `dotnet --list-sdks`) — proof that "more than one major SDK present on the machine" is not hypothetical, only that today's higher major (11) happens to be the pinned one so nothing has silently rolled forward yet. `Interlocutor.Generator.csproj`'s own comment already flags that even the *current* `Microsoft.CodeAnalysis.CSharp` package's `UnionDeclarationSyntax` is marked `[Experimental]`/`RSEXPERIMENTAL006` — the language team itself considers this surface unstable.

This interacts with [16-csharpier-union-parse-gap.md](16-csharpier-union-parse-gap.md): both are examples of the preview-syntax dependency creating tooling/build fragility the repo hasn't fully hardened against.

## Suggested action

Consider `"rollForward": "latestFeature"` or `"latestPatch"` instead, so the pinned major/minor (and therefore the preview language surface) can't silently change out from under the build. If `latestMajor` is deliberate (e.g. to make on-ramping to GA .NET 11 easy later), leave a comment in `global.json` explaining the choice given the preview-feature dependency, since the tradeoff isn't obvious from the file alone.
