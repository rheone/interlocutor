# ADR-0012: HTTP-Agnostic Core with Pluggable Status Mapping

**Status:** Accepted

## Context

The core result types (`Outcome<T>`, `Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, ...) live in `Interlocutor.CommonKernel` and `Interlocutor.Abstractions`, packages that must never depend on ASP.NET Core (see the package-layering rules in `CLAUDE.md`/`AGENTS.md`). Despite that dependency boundary being enforced in code, the project's own documentation - the glossary in particular - describes several result types by the HTTP status code they map to, and disagrees in places with what the ASP.NET Core package's mapping code actually does. That is a library that is architecturally HTTP-agnostic but describes itself as if it weren't, which misleads a consumer who isn't using ASP.NET Core at all (a queue consumer, a console job, a gRPC service) into thinking a transport concern belongs to the domain type.

## Decision

- **The core has no HTTP knowledge, in code or in documentation.** No result type, XML documentation comment, or glossary entry references a status code. `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, and the rest describe domain outcomes; what a given transport does with them is entirely that transport's concern.
- **The ASP.NET Core module owns the mapping, and the mapping is pluggable.** `Interlocutor.AspNetCore` keeps its `ToActionResult()`-style conversion helpers, but they delegate to a mapper abstraction with the library's current status-code choices supplied as an overridable default. A consumer who wants different conventions (a different status for `ValidationFailed`, a body on `NotFound` where the default has none, ProblemDetails shaped differently) replaces the mapper without forking the package.
- **This is a documentation and architecture-boundary decision, not a new package boundary.** The dependency rule that `CommonKernel` and `Abstractions` must never reference ASP.NET Core already exists and is enforced by `tests/Interlocutor.Architecture.Tests`; this ADR records *why* that boundary matters for documentation, not just for the compiler, and directs the accompanying documentation pass to stop contradicting it.

## Consequences

- **Positive:** A consumer who never touches ASP.NET Core - a background worker, a queue consumer, a different web framework - reads result-type documentation that describes their domain, not somebody else's transport.
- **Positive:** Resolves a standing inconsistency where the glossary and the ASP.NET Core mapping code disagreed about which status a validation failure produces; removing HTTP from the glossary removes the thing that could disagree.
- **Positive:** A consumer with different status-code conventions gets there by supplying a mapper, not by forking `Interlocutor.AspNetCore`.
- **Negative:** A reader coming from a web-only background loses the at-a-glance "this outcome means HTTP 404" shortcut in the core docs. Mitigated - that mapping still exists, documented in `Interlocutor.AspNetCore`'s own documentation, exactly where a web consumer would look for it.

## Alternatives considered

- **Keep HTTP status codes as fixed documentation on the core types, purely descriptive:** Rejected - "purely descriptive" doesn't hold once the mapping is pluggable; a fixed status in the glossary would immediately be wrong for any consumer who supplied a different mapper, recreating the disagreement this decision exists to close.
- **Move result types into `Interlocutor.AspNetCore` entirely:** Rejected - the result types are used by consumers with no HTTP surface at all (queue consumers, background jobs); coupling them to a web package would break those consumers for no benefit.
- **Fixed (non-pluggable) default mapping in `Interlocutor.AspNetCore`:** Rejected - status-code conventions are a documented point of disagreement between teams and API styles; hard-coding one opinion forces forking for a one-line difference.
