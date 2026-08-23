# 01 - Native AOT feasibility: the constraint, the containers, and the realistic options

Type: research
Status: resolved
Blocked by: none

## Question

What is the real constraint behind the value-type open-generic failure, and what are the genuinely
available options for supporting trimming and Native AOT in this library?

Sub-questions, all to be answered against primary sources (dotnet/runtime and dotnet/extensions source,
Microsoft Learn, container docs and source, comparable libraries' actual code):

1. **The constraint.** What does `CallSiteFactory.VerifyOpenGenericAotCompatibility` actually do? Is this a
   hard CLR/NativeAOT limitation - value-type generic instantiations are not shared the way reference-type
   ones are, and there is no JIT to synthesize them - or a conservative Microsoft DI guard satisfiable
   another way? Which feature switch drives it: `VerifyOpenGenericServiceTrimmability`,
   `IsDynamicCodeSupported`, or both?
2. **The containers.** Does the same wall hit Autofac, Lamar, DryIoc, and SimpleInjector under a genuinely
   native-compiled binary - a runtime limitation every container shares - or is it specific to Microsoft
   DI's guard? Which of them document AOT support at all?
3. **Does closed registration lift it?** If the existing generator emitted a closed registration per
   concrete `(TRequest, TResponse)` pair, does that avoid the guard *and* give the AOT compiler the
   statically-visible instantiation it needs? What breaks: `GetServices<T>` fan-out ordering, duplicate
   registration, and - critically - the marker-interface constraint filtering that currently scopes
   behaviors per request?
4. **Prior art.** How do `martinothamar/Mediator`, Wolverine, MediatR, and FastEndpoints handle pipeline
   behavior composition under AOT? Source-generated closed chains, reference-type-only responses, or no AOT
   support at all?
5. **Reference-type outcomes.** Would a `record class` `OperationOutcome` sidestep the problem, given that
   reference-type generics *are* shared? What does the C# union preview feature actually emit - must a
   compiler-generated union be a struct, or can it be a class? What would be lost: per-dispatch allocation,
   exhaustiveness checking, struct equality, `default` behavior?
6. **The three levels.** Precisely distinguish analyzer-clean `IsAotCompatible`, a trimmed-but-JIT-ed
   publish, and full Native AOT. Does a trimmed publish also throw, or only warn? What does a consumer
   concretely gain from full AOT over trimming, and lose by stopping at trimming?
7. **CI.** Can each level be gated without the native ILCompiler toolchain on every agent? What does a real
   Linux `dotnet publish -p:PublishAot=true` require, and which GitHub Actions runner images have it? This
   repo has no CI workflows at all today, so any recommendation is greenfield.

Empirical verification is expected wherever possible, especially for sub-question 3 - hand-write a closed
registration in the sample and see whether the throw disappears.

## Answer

Full findings: [`docs/research/native-aot-and-trimming-options.md`](../../../docs/research/native-aot-and-trimming-options.md),
verified against a genuine native binary (the dev container turns out to manage a real AOT publish using gcc
as the linker driver, so these are not flipped-switch results).

**The constraint is real and shared.** The guard keys only off `IsDynamicCodeSupported` -
`VerifyOpenGenericServiceTrimmability` is irrelevant to it, and `PublishTrimmed` actually sets
`IsDynamicCodeSupported=true`, which is why trimmed publishes work. Autofac hits the same wall under a real
native binary, later and with a worse message, and annotates `RegisterGeneric` with `[RequiresDynamicCode]`
naming this exact scenario. The deciding factor is static visibility of the instantiation, not the container.

**There are two blockers, not one.** Under a genuine native binary the DI guard is never even reached:
`Mediator.Send<TResponse>` dies first on `Activator.CreateInstance(typeof(QueryHandlerWrapper<,>)
.MakeGenericType(query.GetType(), typeof(TResponse)))`, because `query.GetType()` has no static identity.
Fixing only the registrations moves the error rather than removing it. Both halves are required.

**Closed registrations do work** - proven twice, including end-to-end in this repo's own sample, where the
guard vanished and the onion composed in correct order. The open-generic descriptor must be *removed*, not
supplemented; one leftover re-triggers the throw.

**A reference-type outcome is not available.** A C# `union` is always a struct; `class union` and its
variants are syntax errors in the pinned SDK. Option (C) means abandoning the union feature entirely, so it
reverses [ADR-0001](../../../docs/adr/0001-use-csharp-15-unions-for-result-types.md). Rejected.

**Recommendation: hybrid.** Ship trim-compatibility now as a correctness fix, then pursue generated closed
pipeline composition as a scoped follow-up in independently verifiable steps.

### Findings that changed this session's work

Four `UnconditionalSuppressMessage` justifications written earlier in this session asserted the closed
generics were "closed only over reference types". They are false - `OperationOutcome<...>` is a struct
(confirmed directly: `IsValueType = True`, base `System.ValueType`). Those suppressions are precisely what
let an analyzer-clean build coexist with a runtime that failed every dispatch.

Two further false justifications were found beyond the four the research listed: every failure case type
(`NotFound`, `Forbidden`, `Conflict`, ...) is also a `readonly record struct`, so the same claim in
`ResultExtensions.InvokeMap` was untrue, and `INotification` carries no `class` constraint, so the claim in
`NotificationHandlerResolver` was overstated.

All are corrected. See the map's Decisions-so-far for what shipped as a result.

### Carried forward

- [§10](../../../docs/research/native-aot-and-trimming-options.md) lists what remains unverified. Two items
  matter for the decision: the behavior-catalog design for option (B) is **a proposal, not a validated
  design**, and the combinatorial size of a (behavior x request) catalog is an estimated risk, not a measured
  one. Neither should be treated as settled when ticket 03 is worked.
- Under `IsDynamicCodeSupported=false`, marker-interface constraint filtering is **already broken**
  independently of everything else: the value-type guard runs before `MakeGenericType`, so a constrained
  behavior throws for requests it was never meant to apply to, rather than being skipped.
