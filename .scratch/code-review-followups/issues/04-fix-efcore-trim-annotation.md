# 04 - Remove or justify the mismatched trim annotation on EF Core's TransactionBehavior

**What to build:** `Interlocutor.EntityFrameworkCore`'s `TransactionBehavior<TRequest, TResponse, TContext>` carries `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]` on `TContext`, but nothing in `Handle` constructs `TContext` reflectively - it's obtained exclusively through the DI-injected `Lazy<TContext>` parameter, and `DbContext`-derived types in practice have a `DbContextOptions<T>` constructor, not a parameterless one. The annotation doesn't correspond to any actual reflective construction path and isn't explained by the surrounding doc comments or the csproj's trim-compatibility comment (ADR-0016, trim-compatible not AOT-compatible).

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] The `DynamicallyAccessedMembers(PublicParameterlessConstructor)` annotation on `TContext` is removed, or a comment explains the actual reflective path it's protecting (if one exists that the review missed)
- [ ] Trimming/publish verification (per the CI gate added for ADR-0016) still passes
