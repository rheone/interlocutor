# C# Test Sweep

Systematic test suite improvement for C# projects. Detects the test framework and mocking library from project files, then dispatches to framework-specific companion skills.

## Supported Frameworks

| Test Framework | Skill |
|---------------|-------|
| xUnit v3 | [`skills/xunit-csharp/`](skills/xunit-csharp/SKILL.md) |
| NUnit v5 | [`skills/nunit-csharp/`](skills/nunit-csharp/SKILL.md) |
| MSTest v4 | [`skills/mstest-csharp/`](skills/mstest-csharp/SKILL.md) |

| Mocking Library | Skill |
|-----------------|-------|
| Moq | [`skills/moq-csharp/`](skills/moq-csharp/SKILL.md) |
| NSubstitute | [`skills/nsubstitute-csharp/`](skills/nsubstitute-csharp/SKILL.md) |
| RhinoMocks | [`skills/rhinomocks-csharp/`](skills/rhinomocks-csharp/SKILL.md) |
| Telerik JustMock | [`skills/justmock-csharp/`](skills/justmock-csharp/SKILL.md) |

## Skill File Convention

Each sub-skill follows a consistent layout:

| File | Purpose |
|------|---------|
| `SKILL.md` | Rules and decisions — what to do |
| `REFERENCE.md` | API lookup tables — what's available |
| `EXAMPLES.md` | Worked examples — what it looks like |
| `ANTI-PATTERNS.md` | Framework-specific pitfalls to avoid |

Framework-agnostic rules (naming, AAA structure, no shared state) live in [`references/quality-checklist.md`](references/quality-checklist.md), applied at every class regardless of framework.

## Invocation

```
/csharp-test-sweep                      # full project sweep
/csharp-test-sweep ClassName            # single test class
/csharp-test-sweep ClassName.Method_Test  # single test method
```
