# 04 - Spike: should INTERLOC0xx diagnostic descriptors move nearer their raise sites?

**What to build:** Not a rewrite - a written decision. `src/Interlocutor.Generator/InterlocutorDiagnostics.cs` centralizes all eleven diagnostic descriptors (severity rationale, `.editorconfig` escalation snippets) while the code that actually raises each one lives across six separate analyzer files (`HandlerDiagnosticAnalyzer`, `OpenGenericHandlerResolver`, `NotificationHandlerOrderPlanner`, `DuplicateOutcomeCaseTypeAnalyzer`, `ValidationShapeMismatchAnalyzer`, `OutcomeMemberGenerator`). Each descriptor already cross-references its raising analyzer via `<see cref>`, which substantially mitigates the split - this is the standard shape for a Roslyn analyzer diagnostic catalog, not an obvious defect. Produce a short written recommendation: relocate descriptors next to their raise sites, or keep the centralized catalog (with the reason - e.g. severity rationale benefiting from living in one auditable place). Do not move any descriptors in this ticket unless the recommendation is unambiguous and small enough to fold into the same ticket.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] All eleven descriptors and their six raise sites are enumerated in the decision
- [x] Decision weighs the `<see cref>` cross-referencing already in place against the cost of jumping files
- [x] Decision is recorded in this ticket's `## Comments`
- [x] If the decision is "relocate" and the move is purely mechanical (descriptor + doc comment moved, no behavior change), it may be completed within this same ticket; otherwise file a follow-up
- [x] If the decision is "keep centralized," no code changes are made

## Comments

**Enumeration** (`src/Interlocutor.Generator/Diagnostics/InterlocutorDiagnostics.cs`, all eleven `INTERLOC0xx` descriptors, confirmed by grepping every non-`<see cref>` `InterlocutorDiagnostics.<Name>` reference outside the catalog file itself):

| Descriptor | Raise site |
|---|---|
| `MissingHandler`, `UnresolvedGenericRequestUsage`, `DuplicateHandler`, `CatchAllNotificationHandler` | `Analysis/HandlerDiagnosticAnalyzer.cs` |
| `OpenGenericHandlerAmbiguity`, `HandlerOverrideInvalidTarget` | `Analysis/OpenGenericHandlerResolver.cs` |
| `NotificationHandlerOrderingCycle`, `NotificationHandlerOrderingReferenceIsNotAHandler` | `Analysis/NotificationHandlerOrderPlanner.cs` |
| `DuplicateOutcomeCaseType` | `Analysis/DuplicateOutcomeCaseTypeAnalyzer.cs` |
| `ValidationShapeMismatch` | `Analysis/ValidationShapeMismatchAnalyzer.cs` |
| `TooFewOutcomeCaseTypes` | `OutcomeMemberGenerator.cs` |

**Decision: keep centralized.** No code changes.

Each descriptor already carries substantial, largely self-contained doc-comment content that has little to do with its raise site's own analysis logic: default-severity rationale, the exact `.editorconfig` escalation snippet, and edge-case/blind-spot discussion (e.g. `MissingHandler`'s remarks on the acyclic-reference blind spot span a full paragraph unrelated to how `HandlerDiagnosticAnalyzer` itself walks the handler list). Several descriptors' remarks explicitly cross-reference sibling descriptors for shared reasoning ("escalated the same way as `MissingHandler`") - that convenience depends on the siblings being in the same file; splitting across six raise-site files would turn a same-file glance into a cross-file jump for exactly the comparisons this catalog exists to make easy. The existing `<see cref>` links (`ReferencedModuleDiscovery`, `RequestKind`, and each raise-site's analyzer type) already give a reader sitting in the catalog a one-click path to the raising code when they need it - the cost this ticket weighs against relocating is already mitigated in the direction that matters (catalog → raise site), just not the reverse.

This is also, per the ticket's own framing, "the standard shape for a Roslyn analyzer diagnostic catalog" - Roslyn's own analyzers and StyleCop.Analyzers both centralize their `DiagnosticDescriptor` catalogs in one file rather than co-locating each with its raise site, for the same reason: a diagnostic catalog is a *severity/id/message* audit surface first, an implementation-detail surface second. `AnalyzerReleases.Shipped.md`/`AnalyzerReleases.Unshipped.md` (this project's own id-tracking files, sitting beside the catalog) already assume one file is the canonical id→descriptor mapping; splitting the catalog would weaken that pairing without a corresponding readability win at any of the six raise sites, which don't need eleven descriptors' worth of severity prose interleaved with their own semantic-model logic.
