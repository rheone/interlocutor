# 09 - Samples update

**What to build:** The runnable sample app demonstrates a write going through the new unit-of-work-owned transaction model end to end, with no reference to the retired `TransactionBehavior` packages. The bootstrap reference doc's code is updated to match.

**Blocked by:** 04, 05.

**Status:** ready-for-agent

- [ ] `samples/Interlocutor.Sample.WebApi` builds and runs against the new model
- [ ] `docs/bootstrapreference/TransactionBehavior.cs` reference sample updated or replaced to match the new model
- [ ] Sample is verified against a real `ServiceProvider`, per the project's existing convention for this sample
