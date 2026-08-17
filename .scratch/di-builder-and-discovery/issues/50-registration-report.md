## Labels

ready-for-agent

## What to build

There is currently no single place to see what `InterlocutorBuilder` actually accumulated, or why - debugging "why isn't X registered" or "why did X get registered twice" means reading every builder call site by hand. Build the shared reporting model tickets 51 and 52 both build on.

- A structured, container-neutral report model recording, for the current builder configuration: every accumulated registration entry together with its provenance (explicit call, scan discovery - and, once ticket 51 lands, matched-by-rule), anything skipped and why (most importantly, the existing silent optional-package probing in `TryGetType` - `Interlocutor.FluentValidation`/`Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` support that wasn't wired because the package isn't referenced - which stays silent as *behavior* but becomes visible as a *report entry*), and a `Problems` collection for validation failures (populated by ticket 52; empty until then).
- `InterlocutorBuilder.DescribeRegistrations()` returns this model, buildable and inspectable at any time regardless of whether `ValidateOnBuild()` (ticket 52) is ever called.
- A human-readable formatter over the structured model (e.g. a `ToString()` or dedicated formatting method) suitable for logging or console output.
- Both `Interlocutor.DependencyInjection.Microsoft` and `Interlocutor.DependencyInjection.Autofac` optionally pipe the formatted report through `ILogger` at `AddInterlocutor()` time, if a logger is available in the service collection/container being configured - additive, never required to use the report.
- This is a single shared model, not two: ticket 52's aggregate validation exception carries this same object rather than building its own separate structure.

## Acceptance criteria

- [ ] `DescribeRegistrations()` returns a structured report reflecting every entry accumulated so far, with per-entry provenance
- [ ] The report includes entries for optional-package soft-dependency probes that found nothing, without those probes starting to throw
- [ ] A human-readable formatter renders the structured report as text
- [ ] Both DI packages can optionally log the formatted report at `AddInterlocutor()` time when a logger is available, without requiring one
- [ ] The report model has a `Problems` collection, empty until ticket 52 populates it, so ticket 52 doesn't need a second model
- [ ] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

49 - Handler lifetime and AddHandler (the provenance categories this ticket reports on need to already exist)
