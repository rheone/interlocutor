Status: ready-for-agent

## Labels

ready-for-agent

## What to build

Of 21 MediatR/Wolverine mentions found repo-wide (`documentation/`, `README.md`, `CLAUDE.md`, `maintainer_docs/`, `.scratch/`, inline/XML comments), 18 are already fine - comparisons, explicitly-rejected alternatives, or differentiation language (e.g. `maintainer_docs/adr/0003-mediator-as-service-locator.md`, `documentation/builder-and-registration.md:29`, `maintainer_docs/research/native-aot-and-trimming-options.md`). Exactly 3, all in `maintainer_docs/bootstrapreference/Conversation.md`, cite MediatR as *the reason* a name or technique was chosen rather than deriving it from the Mediator pattern (GoF 1994) itself - including the traceable origin of the shipped `IPipelineBehavior<TRequest,TResponse>` name. MediatR/Wolverine are prior art for a well-understood pattern, not authorities Interlocutor answers to; reword the justification, don't touch the shipped API.

Reword these three spots so MediatR is cited as prior art/comparison, never as the reason for an Interlocutor name or technique:

- **Line 262** - `"Decorator" is fine informally, but the more precise term is **pipeline behavior** (MediatR's term) or **middleware** (ASP.NET's term).` - drop the "(MediatR's term)"/"(ASP.NET's term)" attributions; describe "pipeline behavior" and "middleware" as standard terminology for a decorator-style pipeline stage in this space generally, not coined by or borrowed from either library. Interlocutor and MediatR converge on similar names because they're solving the same well-understood problem, not because Interlocutor deferred to MediatR's choice.
- **Line 518** - `...MediatR, ASP.NET's controller activation, and AutoMapper's internal resolution all do the same, for the same reason.` - keep as a "these are independent examples of the same general confinement pattern" observation, but tighten the wording so it reads as parallel prior art, not a justification ("we do this because MediatR does").
- **Line 526** - `Fixed with a small **handler-wrapper hierarchy** (the same technique MediatR uses internally): ...` - reword to describe the handler-wrapper hierarchy on its own technical merits (a non-generic base + statically-typed generic implementations, cached via reflection), dropping "the same technique MediatR uses internally" as the justification. A passing "MediatR uses a similar technique" aside is fine if it stays informational rather than justificatory.

Leave unchanged: the other 18 MediatR/Wolverine mentions repo-wide; the shipped `IPipelineBehavior<TRequest,TResponse>` type name (renaming shipped surface is a separate, much larger decision, and "pipeline behavior" is a fine name on its own terms); the "creator of MediatR" credibility citation in `.scratch/unit-of-work/research.md` (cites a person's independent opinion on an unrelated topic - repositories - not MediatR's conventions); the historical, explicitly-superseded `Conversation.md:620` heading ("what's missing for a 'true' mediator/CQRS implementation" - `maintainer_docs/README.md` already marks this whole file superseded/historical-context-only).

## Acceptance criteria

- [ ] Line 262 reworded per above
- [ ] Line 518 reworded per above
- [ ] Line 526 reworded per above
- [ ] No other line in `Conversation.md`, or any other file, touched
- [ ] `rg -n "MediatR's term|the same technique MediatR uses internally" maintainer_docs/` returns nothing
- [ ] `git diff` shows only wording changes, no structural/heading changes

## Blocked by

None - can start immediately.

## Comments
