## Labels

ready-for-agent

## What to build

Let a developer publish an application using this library ahead-of-time compiled, with no trim warnings — something the incumbent alternatives cannot offer.

Most of the work is already done by the time this ticket starts: compile-time discovery removed runtime type construction from the dispatch path, and the generated dispatch switch removed the last reflection from runtime-typed dispatch. What remains is making the guarantee real and keeping it real.

Mark the shipping modules as trim- and AOT-compatible and fix whatever that surfaces. Annotate the runtime scanning fallback so its reflection is declared rather than silently unsafe, and keep it off unless explicitly requested — a library that works in development and fails in a trimmed publish is precisely the surprise this effort exists to eliminate.

Add a publish check to continuous integration so the guarantee cannot silently regress. Without a gate, an ahead-of-time claim becomes a false claim within a few commits.

## Acceptance criteria

- [ ] Shipping modules are marked trim- and AOT-compatible and build clean under that analysis
- [ ] The sample application publishes ahead-of-time compiled with no trim or AOT warnings
- [ ] The runtime scanning fallback is annotated, and using it produces the expected warning rather than a silent failure
- [ ] Dispatch, pipeline composition, and notification fan-out all work in the published output
- [ ] Continuous integration runs the ahead-of-time publish and fails on new warnings
- [ ] Support is documented, including what the runtime scanning fallback costs

## Blocked by

12 — Cross-assembly module aggregation
13 — Generated dispatch switch
