# RhinoMocks Quality Checklist

Design judgment best practices for RhinoMocks (legacy). Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] Every stubbed call has a corresponding `AssertWasCalled` — setup without verification is dead configuration
- [ ] Specific matchers (`Arg<T>.Is.Equal(v)`) preferred over `Arg<T>.Is.Anything` when the expected argument value is knowable
- [ ] Record/replay model avoided for any new tests — AAA style (`GenerateMock` / `Stub` / `AssertWasCalled`) is the only acceptable pattern for new additions
- [ ] `GeneratePartialMock<T>` avoided — prefer a concrete subclass over partial mocks; partial mocks mask design issues
- [ ] `StructureMap.AutoMocking` / `RhinoAutoMocker` used with awareness that it hides dependency configuration — consider explicit mock creation when setup is non-trivial
- [ ] Every test file touched during a sweep flagged for migration to NSubstitute or Moq — track in the sweep summary
