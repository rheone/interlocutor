# MSTest Quality Checklist

Design judgment best practices for MSTest v4. Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] `CollectionAssert` and `StringAssert` preferred over manual `foreach` + `Assert.IsTrue` loops — clearer intent and better failure messages
- [ ] `Assert.ThrowsException<T>` / `Assert.ThrowsExceptionAsync<T>` preferred over try/catch + `Assert.Fail`
- [ ] `[DynamicData]` preferred over repetitive `[DataRow]` when the data set has meaning beyond its values
- [ ] `[DiscoverInternals]` preferred over making test methods public purely for test discoverability
- [ ] `Assert.Inconclusive` includes a tracking item reference — never used as a placeholder without follow-up
- [ ] `[DataRow]` argument order matches the test method parameter order — expected values last, inputs first
- [ ] MSTest Analyzers enabled — add `MSTest.Analyzers` NuGet to catch missing attributes and assertion anti-patterns at build time
