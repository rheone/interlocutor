## Labels

ready-for-agent

## What to build

The aggregate failure types in the common kernel are value types backed by an immutable array. A zero-initialized value bypasses the constructor, leaving the backing array in its default null-backed state, so every accessor throws a null reference exception.

This is reachable in normal operation: the pipeline passes a default value for the response type when a handler has not yet produced one.

Make the default value behave as an empty, inert instance across the validation-error aggregate, its generic variant, and the failure aggregate. Keep the at-least-one-entry constructor guard, which still holds for explicitly constructed values. Document the default-value semantics on each type, since this is the cost of the value-type conversion.

## Acceptance criteria

- [ ] The default value of each aggregate type reports a count of zero and returns an empty collection without throwing
- [ ] Every public member that reads the backing array is guarded, not just the count
- [ ] Explicitly constructing any of these types with an empty sequence still throws, as before
- [ ] Each type's documentation states what its default value means
- [ ] Tests are written at the public result-type seam, red before green

## Blocked by

None — can start immediately
