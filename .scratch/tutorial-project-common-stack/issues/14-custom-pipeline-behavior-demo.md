# 14 - Custom pipeline behavior demo (slow-handler warning)

**What to build:** A second custom `IPipelineBehavior` beyond the recommended chain (Logging → Auditing → Authorization → Validation → Transaction), demonstrating how to add one of your own - a slow-handler warning behavior that logs when a handler exceeds a threshold duration, per the original guide's own example.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] The behavior is registered and demonstrably runs in the pipeline (a test proving it fires above the threshold and not below it)
- [ ] A code comment explains where it sits in the onion ordering and why
