> **Status: done** - implemented alongside Ticket 01 in the same session; see `PRD.md` for the full background.

## Labels

ready-for-agent

## What to build

A handler or any pipeline behavior needs a per-request metadata bag it can write into and read from in place, at any point during a dispatch - no record-replace dance required the way the rest of the ambient request context works. This extends the library's existing (but previously unused) ambient request-context accessor rather than introducing a second, parallel mechanism.

The bag must stay open to future needs without forcing a breaking change every time a new ambient concern shows up: a plain, typed key-value surface rather than a closed set of named properties. It must be safe to read and write concurrently, since nothing in the pipeline model prevents a behavior from forking work internally within one dispatch.

Scope is explicitly the per-DI-scope granularity the ambient context already has today (one bag per scope, shared across every dispatch issued in that scope) - not a new per-dispatch isolation mechanism. Document the caveat for a scope that issues more than one dispatch (e.g. a background service looping over messages in one scope) rather than solving it with new machinery.

While implementing this, a latent bug in the ASP.NET Core ambient-context implementation needs fixing: it rebuilds a brand-new context object on every single read within the same request, which would make anything written into the new bag invisible to a later read in the same request. The context must be built once per request and cached for the life of the request.

## Acceptance criteria

- [ ] The ambient request context's extension-data property is always non-null and mutable in place - a handler or behavior can set and read a value without replacing the surrounding context object
- [ ] Reading the bag is thread-safe under concurrent writers with distinct keys
- [ ] Repeated reads of the ambient context within the same ASP.NET Core request return the same context instance, and therefore the same bag instance - fixing the existing rebuild-on-every-read behavior
- [ ] Swapping an unrelated property on the ambient context via its existing immutable-record update pattern does not lose what was already written into the bag
- [ ] The lifetimes/ambient-context reference documentation is updated to show in-place mutation instead of the old whole-record replacement pattern, and documents the multi-dispatch-per-scope caveat
- [ ] The glossary's entry for the ambient context is updated to describe the bag as mutable in place, with a new entry for the bag type itself

## Blocked by

None - can start immediately.
