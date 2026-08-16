# General Quality Checklist

Apply at every class regardless of test framework. Step 2 of the sweep loop references this checklist.

- [ ] Use "object mothers" when repeatedly using similar data for testing purposes
- [ ] Test names clearly state: member under test, scenario, expected outcome -- {MemberUnderTest}_{Scenario}_{Expectations}_Test
- [ ] Each test follows Arrange / Act / Assert with section comments
- [ ] A parameterized test with exactly one data row is a violation — convert to non-parameterized test
- [ ] Each set of test cases covers at minimum: one happy-path case, one failure/null/invalid path, and relevant boundary values
- [ ] Strongly-typed data sources used where the framework supports it; avoid raw `object[]` arrays
- [ ] Parse-roundtrip data: when theory row inputs feed a parse API, derive them from the same normalized form the expected value uses — never from a raw pre-normalization source
- [ ] Loop-generated theory data: when iterating a source set through a normalizing constructor, prefer reducing the source set to one canonical value per equivalence class (**source reduction**); use a `HashSet<string>` keyed on the serialized form only as a fallback when the full cross-product is genuinely needed for coverage
- [ ] Exception tests use the framework's assertion helper, not try/catch
- [ ] Assert.Contains for exception message verification to prevent false positives
- [ ] Async tests return Task, never async void
- [ ] Use CancellationToken.None (not default) in tests where cancellation is not the subject
- [ ] No shared static mutable state between tests
- [ ] No `Thread.Sleep` or `Task.Delay(>100ms)` — arrange deterministic conditions instead
- [ ] No `Task.Wait()`, `Task.Result`, or `.GetAwaiter().GetResult()` in async tests — use `await` instead
- [ ] Deterministic tests — no `DateTime.Now`, `Random`, `Guid.NewGuid()` without explicit seeding or injection; prefer `TimeProvider` for time-dependent logic
- [ ] Assertion failure messages included when the default output would be ambiguous — multiple similar assertions or the asserted value alone doesn't reveal the failure cause
- [ ] Repeated magic values extracted to named constants — `const` at class level when repeated across tests, `const` in method when single-use
- [ ] Test observable behavior, not implementation details — assert against return values and state, not private internals (exception: verified mock interactions at dependency boundaries)
- [ ] Each test has all three AAA section comments (Arrange, Act, Assert) present and correctly placed
- [ ] Never mock/substitute the class under test — mocking frameworks intercept virtual methods, causing the method under test to return a default value instead of executing. Use a concrete subclass instead. (Applies to all frameworks: NSubstitute `Substitute.For<T>`, Moq `new Mock<T>`, JustMock `Mock.Create<T>`, RhinoMocks `MockRepository.GenerateMock<T>`.)
- [ ] Mock/substitute call assertions belong in the Assert section only
- [ ] Disabled/skipped tests include written justification
- [ ] Test output helpers used only for diagnostic context, not as assertion substitutes
- [ ] Every test method contains at least one assertion (`Assert.*`, `Received()`, `Verify()`, or `Assert.Throws`) — tests with no assertions pass vacuously and provide false confidence
- [ ] Any test using a mock, stub, or expectation should assert expectations
- [ ] Each test verifies one logical scenario (not combining unrelated behaviors)
