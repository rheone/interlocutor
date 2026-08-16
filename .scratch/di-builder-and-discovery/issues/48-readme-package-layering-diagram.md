## Labels

ready-for-agent

## What to build

`README.md`'s "Library Overview" section has a table listing each package and its purpose, but nothing showing *how the packages depend on each other* - a consumer deciding which packages to install has to infer the dependency direction from reading each row's prose individually.

Add a flowchart directly under (or beside) the existing table showing package dependency layering: `Interlocutor.CommonKernel` (zero dependencies) ← `Interlocutor.Abstractions` ← `{Interlocutor.AspNetCore, Interlocutor.DependencyInjection.Microsoft, Interlocutor.DependencyInjection.Autofac, Interlocutor.EntityFrameworkCore, Interlocutor.NHibernate, Interlocutor.FluentValidation}`. This mirrors the package-layering description already in `CLAUDE.md` and is meant to complement the existing table (which says *what* each package does) rather than replace it (the diagram says *how they relate*).

Keep it lean - this is a single static dependency graph, not an architecture deep-dive; README was recently deliberately trimmed to a lean front door (tickets 37-38), so this diagram should read in a few seconds, not require study.

**Verify by rendering** in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] Flowchart shows `CommonKernel` as the zero-dependency base, `Abstractions` depending only on it, and the six leaf packages each depending on `Abstractions`
- [ ] The existing package table is kept, not replaced
- [ ] Diagram renders correctly via `node tools/validate-mermaid.mjs` and is confirmed legible in both GitHub light and dark rendering
- [ ] Any manual coloring uses `classDef` with explicit `fill` and `color`
- [ ] Diagram accurately reflects the dependency rules in `CLAUDE.md`'s package-layering section (verify against `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` if any doubt)

## Blocked by

None - can start immediately
