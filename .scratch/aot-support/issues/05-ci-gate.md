# 05 - CI gate that holds the promise

Type: task
Status: resolved
Blocked by: 03

## Question

Stand up the continuous integration that makes the committed support level un-regressable.

Ticket 17 of `di-builder-and-discovery` put it plainly: without a gate, an ahead-of-time claim becomes a
false claim within a few commits. This effort has already demonstrated the sharper version of that problem -
a **clean analyzer build is not a passing gate**, because the library built warning-free while throwing on
every dispatch.

So the gate has to include whatever level of execution the promise implies:

- Analyzer-clean build across the shipping libraries - necessary, insufficient on its own.
- Whatever publish mode the promise names, actually performed.
- And, if the promise covers runtime behavior, the sample or an equivalent harness **run** and exercised,
  not merely compiled.

Constraints to design around: this repo has **no CI workflows at all** today (only
`.github/dependabot.yml`), so this is greenfield. The dev container has gcc but no clang, so anything
requiring the native ILCompiler toolchain must be shown to work on the chosen runner image rather than
assumed. The container-compatibility promise from ticket 03 may mean the gate has to run more than once,
once per container.

Take the CI recommendation from **Native AOT feasibility** as input rather than re-researching it.

## Answer

Written: `.github/workflows/ci.yml` (this repo's first CI workflow - previously only `dependabot.yml`
existed), `scripts/smoke-test-trimmed-sample.sh`.

Two jobs. `build-and-test` is the baseline this repo had none of. `trim-compatibility` holds the promise:

1. `dotnet build -warnaserror:<the IL2xxx trim codes>` across the whole solution - fails fast on a
   per-project analyzer regression, before the slower steps below run. Deliberately excludes IL3xxx (AOT)
   codes, since this library does not claim AOT compatibility.
2. Publishes the sample with `dotnet publish -p:PublishTrimmed=true -r linux-x64 --self-contained`.
3. **Runs the published binary and smoke-tests every dispatch path** - create, both open-generic paths,
   notification fan-out, the short-circuiting stage, delete - asserting HTTP status codes and that the log
   contains no unhandled exception.

Step 3 is the one that matters. Every step of this exact workflow was run locally against a genuine publish
output before being written into the workflow file, not assumed to work from the shape of the YAML.

**Doing this surfaced a second instance of the exact failure category ADR-0016 already describes, from this
same effort.** The claim that a trimmed publish was "verified running end to end" was based on testing a
plain `dotnet build`/`dotnet run` output - which never invokes the trimmer - not an actual
`dotnet publish -p:PublishTrimmed=true` output. Running the real thing crashed on the first request:
EF Core's model validation reflectively constructs a value comparer per property type, and trimming had
removed it. Fixed by reinstating the EF Core compiled model (`dotnet ef dbcontext optimize`) in the sample -
this time verified against the actual published, executed binary, not inferred. `docs/adr/0016-trim-compatible-not-aot-compatible.md`
and `documentation/trim-and-aot-support.md` are both corrected; see the resolution note added to ticket 06
above.

Also fixed along the way, found only by a whole-solution build after wiring `InterlocutorRegistration`'s
`ImplementationType` requirement through to every caller: `InterlocutorRegistration`'s own constructor
parameter needed the same `DynamicallyAccessedMembers(All)` its property already carried (a real ILC-only
gap, invisible to the per-project analyzer - see ADR-0016's own postscript on this), which cascaded through
`HandlerDiscovery`, `InterlocutorBuilder`, and every `InterlocutorPipelineBuilder` `Add*`/`Override*` method;
`Interlocutor.EntityFrameworkCore.TransactionBehavior`'s `TContext` parameter needed
`PublicParameterlessConstructor` for its own `Lazy<TContext>`; and the sample's `GetByIdHandler<TEntity>`
needed the DAM set `DbContext.Set<TEntity>()` itself requires. All three are now annotated correctly rather
than merely not-yet-triggered.

**Runner image:** `ubuntu-24.04`, matching the research's confirmation that it ships the toolchain a future
level-(c) native gate would need, so this workflow does not need to change runner when Option B eventually
ships - only gain a third job.

The publish-time `-p:PublishTrimmed=true` cannot be passed directly to `dotnet publish` here: MSBuild applies
it while restoring too, which fails restoring `Interlocutor.Generator` (netstandard2.0, analyzer-only -
trimming requires .NET Core 3.0+) even though it is never part of the trimmed output. Fixed by restoring
first, then publishing with `--no-restore`, and (defensively, for `dotnet build`'s own global-property
propagation) adding `UndefineProperties` to every `ProjectReference` onto `Interlocutor.Generator`.
