> **Status: ready-for-agent**

## Labels

ready-for-agent

## What to build

`/audit-naming` flagged a Tier-1 (Clash) finding: the identifier `Eject` names two unrelated APIs.

- **Pipeline sense** (keep as-is) - `Interlocutor.Abstractions.Pipeline.Eject.With<TResponse>` / `Eject.WithCase<TCase>`, a per-dispatch runtime short-circuit that ends the *current request* with a chosen outcome. This is the older, more heavily documented, publicly-facing escape hatch (~54 references across `src/Interlocutor.Abstractions`, its tests, and `pipeline-and-behaviors.md`/`extensibility.md`/`transactions-and-store-integration.md`). Not in scope for this ticket.
- **DI-builder sense** (rename target) - `InterlocutorBuilder.Eject(params Type[])` / `Eject<TImplementation>()` / `EjectedImplementationTypes`, a build-time operation that permanently removes implementation types from handler-discovery consideration. Rename this side to **`Exclude`** - it precisely names "don't register this implementation type" without borrowing the pipeline's runtime-control-flow verb.

Rename map (DI-builder sense only):

| Old | New |
|---|---|
| `InterlocutorBuilder.Eject(params Type[] implementationTypes)` | `Exclude(params Type[] implementationTypes)` |
| `InterlocutorBuilder.Eject<TImplementation>()` | `Exclude<TImplementation>()` |
| `InterlocutorBuilder.EjectedImplementationTypes` (public property) | `ExcludedImplementationTypes` |
| `InterlocutorBuilder._ejectedImplementationTypes` (private field) | `_excludedImplementationTypes` |

### Impact list (every call site / mention found via full-codebase search)

**Source:**
- `src/Interlocutor.DependencyInjection.Abstractions/InterlocutorBuilder.cs:105,176,372,375,728,731,738-739` - declaration, backing field, and both overloads.
- `src/Interlocutor.DependencyInjection.Abstractions/PublicAPI.Unshipped.txt:17-19` - update the three `PublicAPI` entries in place (project is unreleased - no `Shipped.txt` migration needed).

**Tests:**
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/AmbiguousClosureTests.cs:8,71,109,125,143,156,161,263` - XML-doc `<see cref>`, method calls, and property read.

**Documentation:**
- `documentation/api-reference/interlocutor-dependencyinjection-abstractions.md:45,58,59` - API reference table rows.
- `documentation/extensibility.md:354` - table row mentioning `Eject`.
- `documentation/builder-and-registration.md:125` - code example (`builder.Eject<LegacyInvoiceFormatter>();`).
- `documentation/glossary.md` - "DI Registration" section's `Eject` term row; reword to `Exclude` and drop the now-resolved naming ambiguity with the Pipeline section's own `Eject` entry.

**Not in scope (historical records - leave as-is):**
- `.scratch/di-builder-and-discovery/issues/55-ambiguous-handler-validation-and-eject.md` - closed (`Status: done`), documents history under the name in effect at the time.
- `.scratch/documentation-cleanup/issues/03-dispatch-and-registration.md` - same, historical.

## Acceptance criteria

- [ ] `InterlocutorBuilder.Eject`/`Eject<T>`/`EjectedImplementationTypes` renamed to `Exclude`/`Exclude<T>`/`ExcludedImplementationTypes`; private field renamed to match.
- [ ] `PublicAPI.Unshipped.txt` entries updated to the new names (no `Shipped.txt` change - project is unreleased).
- [ ] `AmbiguousClosureTests.cs` updated (calls, property reads, and the `<see cref="InterlocutorBuilder.Eject(Type[])"/>` doc comment).
- [ ] `documentation/api-reference/interlocutor-dependencyinjection-abstractions.md`, `documentation/extensibility.md`, and `documentation/builder-and-registration.md` updated to `Exclude`.
- [ ] `documentation/glossary.md`'s DI Registration `Eject` entry renamed to `Exclude` and reworded so it no longer describes a name shared with the Pipeline section's `Eject`.
- [ ] The Pipeline sense (`Interlocutor.Abstractions.Pipeline.Eject`, `PipelineEjectException`, `NoCommit.AndEject`) is left untouched.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution.

## Blocked by

None - can start immediately.
