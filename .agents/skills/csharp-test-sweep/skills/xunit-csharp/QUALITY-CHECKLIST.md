# xUnit Quality Checklist

Design judgment best practices for xUnit v3. Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] `Assert.Equivalent` preferred over manual property-by-property comparison when the type lacks `Equals` — don't write per-property checks unless you need an explicit exception
- [ ] `Assert.Contains` on exception messages preferred over `Assert.True(ex.Message.Contains(...))` — better failure messages
- [ ] `ITestOutputHelper` used for diagnostic context only, never as an assertion substitute
- [ ] `TheoryData<T>` with meaningful row documentation preferred over inline array literals in `[MemberData]`
- [ ] `Assert.Multiple` used only for independent properties within a single scenario — never used to combine unrelated behaviors into one test
- [ ] `[Fact]` preferred over single-row `[Theory]` even when both would compile
- [ ] Collection assertions (`Assert.Collection`, `Assert.All`) preferred over manual `foreach` + `Assert` loops
- [ ] xUnit analyzers enabled — add `xunit.analyzers` NuGet to catch common mistakes at build time
