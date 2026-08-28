# 06 - Guard the HandlerKind enum-bounds assumption in ReferencedModuleDiscovery

**What to build:** `ReferencedModuleDiscovery.TryReadHandler` bounds-checks a decoded `int kindValue` against `(int)HandlerKind.Notification` as an implicit "max enum value." This silently breaks if `HandlerKind` ever gains a new case that isn't added last, and nothing at the `HandlerKind` enum declaration warns that its member order is load-bearing for this cross-assembly decode path.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `HandlerKind`'s doc comment warns that member order/values are load-bearing for `ReferencedModuleDiscovery`'s decode path, or the bounds check is replaced with something that doesn't depend on enum ordering (e.g. `Enum.IsDefined`)
- [x] A test covers what happens when `ReferencedModuleDiscovery` decodes an out-of-range `kindValue` (from a referenced assembly built against a newer generator version)

## Comments

Verified already implemented: the bounds check in `ReferencedModuleDiscovery` uses `Enum.IsDefined(typeof(HandlerKind), kindValue)` (with an inline comment explaining why the old max-value comparison was rejected), and `HandlerKind`'s own doc comment documents the load-bearing member-order/value constraint. `CrossAssemblyAggregationTests` covers the out-of-range `kindValue` decode path. `dotnet test tests/Interlocutor.Generator.Tests` passes (62/62).
