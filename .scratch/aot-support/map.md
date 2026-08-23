# Map: Trimming and Native AOT support level

Label: `wayfinder:map`

## Destination

A locked decision on what Interlocutor promises for trimming and Native AOT - which of the realistic
options the library commits to, and the container-compatibility promise that comes with it (Microsoft DI
plus other common DI tooling) - captured as an ADR, with `documentation/trim-and-aot-support.md` and a CI
gate that hold that promise honestly.

Reaching the destination means nobody has to decide anything else before implementation starts. It does
**not** mean the implementation is done.

## Notes

**Domain.** .NET 11 preview mediator/CQRS library. Responses are C# union `readonly record struct`s
(`OperationOutcome<TSuccess, ...>`, see [ADR-0001](../../docs/adr/0001-use-csharp-15-unions-for-result-types.md)).
Pipeline behaviors are registered as **open** generics (`IPipelineBehavior<TRequest, TResponse>`) and closed
by the container at dispatch time ([ADR-0002](../../docs/adr/0002-pipeline-behavior-onion-model.md),
[ADR-0009](../../docs/adr/0009-no-default-pipeline.md)).

**Skills every session should consult.** `/grilling` for the decision tickets; `/research` for the research
tickets; `/tdd` once implementation starts.

**Standing preferences for this effort.**

- Compatibility with Microsoft DI **and** other common DI tooling is a stated goal, not a nice-to-have.
  A decision that only works on one container is not a resolution.
- Analyzer-clean is not the bar. The blocker that started this effort produced **zero** IL2xxx/IL3xxx
  warnings and still threw at every dispatch. Any claim must be verified by running code, not by a clean build.
- Honesty over marketing: a narrower promise we actually hold beats a broad one we don't.

**The trigger.** With `PublishAot=true`, `Microsoft.Extensions.DependencyInjection` refuses to close
`IPipelineBehavior<TRequest, TResponse>` when `TResponse` is a value type - which `OperationOutcome<...>`
always is. Observed from `CallSiteFactory.VerifyOpenGenericAotCompatibility`; every dispatch in the sample
app throws `InvalidOperationException`.

**Standing claim under test.** [ADR-0008](../../docs/adr/0008-compile-time-handler-discovery.md) already
asserts that compile-time handler discovery "enables build-time diagnostics and AOT/trim compatibility".
This effort tests that claim. A likely outcome is a new ADR that supersedes or qualifies it.

**Working-tree state.** The `improvements` branch carries uncommitted work: `IsAotCompatible` on all nine
shipping libraries, trim/AOT annotations throughout (including the runtime-scanning fallback, as ticket 17
of `di-builder-and-discovery` asked), and the sample's EF Core compiled model, hand-written schema DDL, and
`JsonSerializerContext`. It builds warning-free and all tests pass. Whether the sample keeps `PublishAot=true`
depends on **Commit to a support level and container promise**.

**Related existing tickets.** `di-builder-and-discovery/17-trim-and-aot-hardening.md` is the ticket this
effort grew out of; `33-doc-trim-and-aot-support.md` is its blocked documentation counterpart.

## Decisions so far

<!-- one line per resolved ticket: gist plus link -->

- [CI gate that holds the promise](issues/05-ci-gate.md) - written as `.github/workflows/ci.yml` (this repo's
  first CI workflow) plus `scripts/smoke-test-trimmed-sample.sh`. Builds with the trim-analyzer codes as
  errors, publishes the sample trimmed, then **runs the published binary** and smoke-tests every dispatch
  path - a build-only gate proved nothing twice already in this same effort. Building this gate caught a
  second real instance of exactly that failure category: the "verified end to end" claim on the map's own
  Option A decision had only ever tested a plain build/run, never an actual trimmed publish; the real one
  crashed on the first request from EF Core's own reflective model validation. Fixed (EF Core compiled model
  reinstated in the sample) and this time verified against the executed binary. ADR-0016 and
  `documentation/trim-and-aot-support.md` both corrected accordingly.
- [Commit to a support level and container promise](issues/03-commit-to-support-level.md) - stay on Option
  A (trim-compatible, "on the fly") as the shipped level; defer Option B (generated closed-registration
  catalog) as a scoped future effort. If B is ever built, it must coexist with A as an opt-in mode, not
  replace it - registering both open and closed descriptors for the same service type re-triggers the AOT
  guard closed registration exists to avoid, so a real hybrid switches per compilation rather than adding.
  Full reasoning in [ADR-0016](../../docs/adr/0016-trim-compatible-not-aot-compatible.md).
- [Write the ADR and reconcile ADR-0008's standing AOT claim](issues/04-write-the-adr.md) - written as
  [ADR-0016](../../docs/adr/0016-trim-compatible-not-aot-compatible.md), a narrower correction rather than a
  full supersession - ADR-0008's actual decision (compile-time discovery) is untouched and still Accepted;
  only its AOT claim was wrong. ADR-0008 now carries a pointer note, following the same precedent already
  used for ADR-0003. Indexed in `docs/README.md` under a new "Deployment: trimming & Native AOT" theme.
- [What `documentation/trim-and-aot-support.md` promises consumers](issues/06-consumer-documentation-promise.md) -
  written. The promise: trim-compatible and verified end to end; Native AOT explicitly does not work, and
  the failure mode (clean build, crashes at runtime, no build-time diagnostic today) is stated up front. The
  consumer burden anticipated when this ticket was opened - a compiled EF Core model, hand-written schema
  DDL - turned out to belong to Native AOT specifically and was reverted once the decision landed on trim
  only; what remains is smaller (a `JsonSerializerContext` for minimal APIs, resolving any scanning-fallback
  warning).
- [Prototype: does closed-generic behavior registration survive real dispatch?](issues/02-closed-registration-prototype.md) -
  yes, the generator's existing constraint machinery (`HandlerTypeAnalyzer.BuildConstraints`,
  `GenericConstraintSnapshotBuilder`, `GenericConstraintChecker.Satisfies`) generalizes to behaviors
  unmodified - proven by invoking the real methods via reflection against the sample's compiled types, not a
  reimplementation. `SampleTransactionBehavior<,>` correctly includes only `ITransactionalRequest`-satisfying
  requests; the unconstrained `LoggingBehavior<,>` includes everything. The stage host needs no separate
  treatment - it declares no constraint at all, the same shape already proven. All four original concerns
  (fan-out, ordering, constraint filtering, stage host) are now settled between this ticket and the research.
- [Native AOT feasibility: the constraint, the containers, and the realistic options](issues/01-aot-feasibility-research.md) -
  the wall is real and every container shares it; there are **two** blockers, not one (the mediator's own
  `Activator.CreateInstance` fails before DI is even reached); closed registrations demonstrably fix the DI
  half; a reference-type outcome is impossible because a C# `union` is always a struct. Recommends shipping
  trim-compatibility now and pursuing generated closed composition as a scoped follow-up.

### Shipped as a correctness fix alongside that resolution

Not a decision on the route - the removal of a false claim, which was wrong under every option and so did not
wait for **Commit to a support level and container promise**:

- Every shipping package is now `IsTrimmable`, deliberately **not** `IsAotCompatible`, each carrying the
  reason inline.
- Six false suppression justifications corrected or removed. Three turned out to be pure dead weight - nothing
  needed them once the false AOT claim was gone. The one genuine remaining suppression
  (`ResultExtensions.InvokeMap`, IL2060) now states plainly that it holds when trimming and **not** when
  compiling ahead of time.
- The sample is `PublishTrimmed`, not `PublishAot`, and is **verified running**: create, both open-generic
  paths, notification fan-out, the short-circuiting stage's conflict response, and delete all behave
  correctly, with no exceptions in the log. Publishing it ahead-of-time was previously a warning-free build
  that threw on every request.
- `documentation/trim-and-aot-support.md` no longer claims compile-time discovery "enables AOT/trim"; it
  states the trim guarantee and the ahead-of-time limitation, with the structural reason.
- Full suite green throughout.

## Not yet specified

- **If Option B is picked up later:** the effect on generator incrementality and caching, which interacts
  with `di-builder-and-discovery` ticket 53 (generator pipeline full incrementality); the actual coexistence
  wiring (behavior discovery, the cross product, catalog emission, both translators, `Mediator` rewiring);
  and the container-level configuration surface a consumer would use to opt into it. None of this is sharp
  enough to ticket while B remains deferred rather than committed.
- Whether the notification fan-out path has the same value-type problem. Notification handlers return `Task`
  rather than an `OperationOutcome`, so probably not - but this is reasoning, not evidence, and it has not
  been checked. Note that `INotification` carries no `class` constraint, so a struct notification is legal
  and would face the same instantiation problem.
- The combinatorial size of a (behavior x request) catalog - an estimated risk, not a measured one.
- Whether closed-generic registration measurably changes dispatch cost, and whether this library wants a
  benchmark suite at all.

## Out of scope

- **Option C, a reference-type `OperationOutcome`.** Rejected in [ADR-0016](../../docs/adr/0016-trim-compatible-not-aot-compatible.md):
  the C# `union` feature this design depends on has no reference-type form in the pinned SDK, so this would
  mean abandoning unions for a hand-written hierarchy, reversing
  [ADR-0001](../../docs/adr/0001-use-csharp-15-unions-for-result-types.md) and losing compiler-verified
  exhaustiveness, to solve a problem the Option A/B hybrid solves without touching the type system. Worth
  revisiting only if a reference-type union form ships in a future C# version - unconfirmed whether one is
  even planned.
- The AOT readiness of Entity Framework Core and NHibernate themselves. Upstream; we can document what a
  consumer must do (compiled models, migration bundles) but cannot fix it here.
- Blazor WebAssembly, iOS, and other AOT-mandatory platform targets. Different runtime constraints, and no
  evidence anyone wants Interlocutor there yet.
- Publishing the sample application as a shipped artifact.
