# Sub-Agent Coordination

## Briefing Template

Every sub-agent writing or modifying a test class must receive all of the following in its prompt. Do not omit any item — omissions caused the most expensive recoveries in practice.

Sub-agent spawn thresholds (production class has ≥5 public members OR projected test file length >300 lines) are defined in the detected test framework skill see -

- [skills/xunit-csharp/SKILL.md](../skills/xunit-csharp/SKILL.md) for xUnit projects.
- [skills/nunit-csharp/SKILL.md](../skills/nunit-csharp/SKILL.md) for nUnit projects.
- [skills/mstest-csharp/SKILL.md](../skills/mstest-csharp/SKILL.md) for MSTest projects.

1. **The production class** under test (full source or path)
2. **The gap report** from Discovery for that class
3. **The full rules** from the detected test framework skill and mocking library skill
4. **These sharp edges** (paste verbatim):

   > - **Parameterized Tests**: Prefer strongly typed test data and data sources when possible
   > - **Mocking the class under test**: never mock or substitute the class under test itself; if the method under test is declared on an abstract base class, use a concrete subclass — see the detected mocking library's companion skill
   > - **Parse-roundtrip data**: derive theory inputs from the same normalized form the expected value uses — never from a raw pre-normalization source
   > - **Loop-generated theory data dedup**: prefer reducing the source set to one canonical value per equivalence class; fall back to `HashSet<string>` keyed on the serialized form only if the full cross-product is needed
   > - **C# version floor**: write at the lowest common denominator TFM — see `references/multiframework.md` for the language feature version table

5. **Instruction to sub-agent**: after completing, report what was written and what was skipped; do not run tests (the main agent runs verification per step 6 of the sweep loop)

## Sweep State

At the start of any full or namespace sweep, create `SWEEP-STATE.md` in the test project root:

```markdown
# Sweep State

## Done
<!-- classes completed -->

## In Progress
<!-- class currently being worked on -->

## Pending
<!-- classes not yet started — populate from Discovery Phase directory listing -->

## Notes
<!-- ambiguities, unresolved issues, flagged data sources -->
```

Update `SWEEP-STATE.md` after each class completes (move it from Pending → Done). If the session resets or hits a context limit, the next session reads `SWEEP-STATE.md` to resume from the correct point without re-reading conversation history.

Delete `SWEEP-STATE.md` after the final verification pass at End of Sweep.
