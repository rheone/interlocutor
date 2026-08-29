Status: needs-triage

## Labels

needs-triage

## What to build

`.github/workflows/ci.yml`'s `trim-compatibility` job (gating the ADR-0016 trim-compatible support level) restores, publishes, and smoke-tests `samples/Interlocutor.Sample.WebApi` - the project ticket 01 of the tutorial-project effort deleted:

```yaml
- name: Restore for the trimmed publish
  run: dotnet restore samples/Interlocutor.Sample.WebApi -r linux-x64

- name: Publish the sample trimmed
  run: >-
    dotnet publish samples/Interlocutor.Sample.WebApi -c Release -r linux-x64
    --self-contained --no-restore -p:PublishTrimmed=true
    -o publish/trimmed-sample

- name: Run the trimmed publish and smoke-test every dispatch path
  run: |
    chmod +x publish/trimmed-sample/Interlocutor.Sample.WebApi
    ./scripts/smoke-test-trimmed-sample.sh publish/trimmed-sample/Interlocutor.Sample.WebApi
```

Every push/PR now fails the `Restore for the trimmed publish` step with a missing-path error - this is real, currently-broken CI, not a documentation staleness issue. `scripts/smoke-test-trimmed-sample.sh` (still present) is now dead code with nothing left to call it.

This was missed by ticket 01's own "confirm first that nothing outside `samples/` and the walkthrough doc references these two projects... stop and flag it rather than silently deleting through it" acceptance criterion - the workflow file is under `.github/`, a hidden directory a plain-text reference sweep can miss depending on tooling. Found while completing ticket 12 (documentation sync) in this same effort; **deliberately left unfixed here** rather than patched inline, because the right fix isn't mechanical:

`TutorialProject/TodoTasks.Api` was never built with the old sample's trim/AOT story in mind (no source-generated `JsonSerializerContext` enumerating every request/response body type, no EF Core compiled model, and its NHibernate + Autofac + FluentValidation stack was never verified trim-compatible the way the deleted sample's EF Core + Microsoft.Extensions.DependencyInjection stack was). Silently repointing this job at `TutorialProject/TodoTasks.Api` would either fail the job for real (if the stack isn't actually trim-compatible) or, worse, pass without having verified anything meaningful, misrepresenting what ADR-0016 actually promises. That's a judgment call for whoever owns the trim-compatibility commitment, not something to guess at while doing an unrelated documentation-sync ticket.

## Acceptance criteria

- [ ] Decide, and record the decision: either (a) retrofit `TutorialProject/TodoTasks.Api` with what its own trim-compat smoke test would need (a `JsonSerializerContext`, verification the NHibernate/Autofac/FluentValidation stack is actually trim-safe) and repoint the job at it, or (b) remove the `trim-compatibility` job's publish/smoke-test steps (and `scripts/smoke-test-trimmed-sample.sh`) entirely if this repo no longer has a runnable trim-compat proof, updating `documentation/trim-and-aot-support.md`/ADR-0016 to match whatever level of assurance actually remains
- [ ] CI is green again on a real push/PR, not just `dotnet build`/`dotnet test` locally
- [ ] No dead script or workflow step left behind referencing a path that doesn't exist

## Blocked by

None - can start immediately, but requires a maintainer decision on scope (option a vs. b above) before implementation
