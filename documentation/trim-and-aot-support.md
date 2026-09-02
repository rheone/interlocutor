# Trim & AOT Support

> [!IMPORTANT]
> **Interlocutor is trim-compatible. It is not currently compatible with ahead-of-time compilation.**
> Every shipping package is marked trimmable and builds clean under trim analysis - CI fails the build on
> any trim-analyzer regression. A trimmed, self-contained publish has previously been verified end to end
> (built, published, and actually run against every dispatch path) rather than only at the analyzer level,
> but that runtime verification is not currently part of CI - see Known limitations below. Publishing
> ahead-of-time compiled (`PublishAot=true`) does **not** work: it produces a warning-free build that then
> fails every dispatch at runtime.

## Why ahead-of-time compilation doesn't work yet

A response is a compiler-generated union type (`Outcome<TSuccess, ...>` and its arities), which is
always a value type. Pipeline behaviors are registered as open generics
(`IPipelineBehavior<TRequest, TResponse>`) and closed over the concrete request/response pair by the
container the first time a matching request is dispatched.

Closing a generic type over a value type argument the runtime has never seen before needs a just-in-time
compiler to synthesize the new code. A trimmed, self-contained publish still has one. An ahead-of-time
compiled publish does not - every instantiation has to already exist in the binary before the process
starts. This is a property of the .NET runtime, not a quirk of one dependency injection container:
Microsoft.Extensions.DependencyInjection fails loudly and immediately with a clear exception; other
containers that close open generics the same way hit the identical wall, later and less clearly.

This is why the failure mode matters as much as the fact: attempting `PublishAot=true` today does not
produce a warning at build time. It produces a clean build that crashes on the first request. If you are
evaluating Interlocutor for a Native AOT deployment, stop at trimming for now - see Known limitations below.

## Trim-safety guarantees

Every shipping package (`Interlocutor.Abstractions`, `Interlocutor.AspNetCore`, `Interlocutor.CommonKernel`,
`Interlocutor.DependencyInjection.Microsoft`, `Interlocutor.DependencyInjection.Autofac`,
`Interlocutor.Persistence.EntityFrameworkCore`, `Interlocutor.FluentValidation`, `Interlocutor.Persistence.NHibernate`) is marked
trimmable and builds clean under trim analysis. Compile-time handler discovery means the default dispatch
path performs no runtime reflection at all - no `Assembly.GetTypes()`, no `Activator.CreateInstance` over a
dynamically-discovered type - so trimming the assemblies a compiled application doesn't actually use is
safe.

A handful of call sites are still genuinely reflective by design - notification fan-out across a
notification's base-type hierarchy, and the generic `NotFound<TId>` dispatch inside `OutcomeMapperBase<TResult>`
(shared by every `IOutcomeMapper<TResult>` implementation, HTTP or otherwise - `DefaultHttpOutcomeStatusMapper`
is its only shipped consumer today) that dispatches on a failure's runtime type - and are documented as safe
under trimming specifically because every type they close over is a reference type, which the trimmer keeps
reachable through the application's own ordinary use of it.

## The runtime scanning fallback, and what it costs

`ScanAssembly`/`ScanAssemblies`/`ScanAssemblyContaining<TMarker>`/`ScanEntryAssembly`, `AddTypeMatch`, and
`InterlocutorStartupValidation.ValidateHandlerRegistrations` are the one part of the library that is
genuinely unsafe to trim, because they exist specifically to discover types reflectively - the point of
calling them. Every one of these methods carries a `RequiresUnreferencedCode` annotation, so a trimmed
publish that actually calls one produces a build warning naming exactly which call is at risk, rather than a
silent trim that might remove the very handler the scan was supposed to find.

None of these methods run unless a consumer explicitly calls one. A bare `AddInterlocutor()` with only
`AddGeneratedRegistrations()` and explicit `AddHandler`/`AddBehavior` calls never reflects at all, and
carries none of this cost.

## Publishing trimmed

```bash
dotnet publish -c Release -p:PublishTrimmed=true -r <your-runtime-identifier> --self-contained
```

Every dispatch path exercised by ordinary usage - typed `Send`/`Publish` calls, pipeline behaviors,
notification fan-out, the ASP.NET Core outcome mapping - has previously been verified working under this
publish mode, built and actually run, not just built (see the Known limitations note on CI coverage below).
If your application also calls one of the runtime scanning methods above, resolve the
`RequiresUnreferencedCode` warning it produces before trusting the trimmed output for that path: either keep
the assembly it scans un-trimmed, or move that registration to compile-time discovery instead.

## Known limitations

- **The end-to-end runtime verification is not currently automated in CI.** It previously ran against a
  dedicated runnable sample project that has since been removed; CI now only fails the build on a
  trim-analyzer regression (`IL2026` and friends), which catches a large class of trim-unsafe code but does
  not prove a trimmed publish actually dispatches correctly at runtime the way running one does. Rebuilding
  that runtime verification is tracked separately and deliberately hasn't been folded into any other
  in-flight project's scope.
- **No Native AOT support today.** See "Why ahead-of-time compilation doesn't work yet" above. There is no
  workaround that keeps the library's pipeline-behavior composition working under `PublishAot=true`.
- **JSON serialization for HTTP bodies still needs your own attention under trimming.** `ToActionResult()`
  and `ToMinimalApiResult()` serialize the outcome's success or failure body through whatever
  `System.Text.Json` configuration your application supplies. Under trimming, register a source-generated
  `JsonSerializerContext` covering every type your endpoints actually return - including failure bodies like
  `ProblemDetails` - the same way any trimmed ASP.NET Core minimal API application would.
- **A store integration's own trim/AOT support is out of Interlocutor's control - and for EF Core, it is not
  optional.** `Interlocutor.Persistence.EntityFrameworkCore` is trim-compatible itself, but EF Core's normal
  model building reflectively constructs a value comparer per property type the first time any operation
  touches the database - not only `EnsureCreated()`/`Migrate()`, any operation - and trimming removes that
  constructor if nothing else references it, producing a `MissingMethodException` at that point despite a
  clean build. A compiled model (`dotnet ef dbcontext optimize`, then `optionsBuilder.UseModel(...)`) avoids
  this by building that step in at compile time instead. `Interlocutor.Persistence.NHibernate` has its own
  trimming story under NHibernate - consult that library's own documentation.

## Links

- [Handler Selection & Dispatch](handler-selection-and-dispatch.md) - compile-time discovery, which removes reflection from the dispatch path and is what makes trimming safe
