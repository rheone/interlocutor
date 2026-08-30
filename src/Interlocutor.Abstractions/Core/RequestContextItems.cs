namespace Interlocutor.Abstractions.Core;

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A mutable, thread-safe, request-scoped key-value bag carried on <see cref="RequestContext.Items"/>.
/// </summary>
/// <remarks>
/// <para>
/// Any handler or pipeline behavior that can reach the ambient <see cref="RequestContext"/> - typically via
/// <see cref="IRequestContextAccessor"/> - can read and write this bag in place, at any point during a
/// dispatch: no <c>with</c>-expression record replacement is needed the way it is for
/// <see cref="RequestContext"/>'s own <c>init</c>-only properties. <see cref="RequestContext"/> itself stays a
/// structurally immutable record - its <see cref="RequestContext.Items"/> property reference never changes
/// after construction - but the object that property points to is mutable, the same way a record holding a
/// <see cref="List{T}"/> is immutable only with respect to its own properties, not transitively.
/// </para>
/// <para>
/// Deliberately a plain, open-ended key-value bag rather than a closed set of named properties: a future
/// ambient concern this library doesn't know about yet can be carried through an existing dispatch without
/// ever requiring a breaking change to <see cref="RequestContext"/> - the same "stay open to what the library
/// doesn't know about yet" goal <c>IOutcomeCaseRegistry</c>/<c>IOutcomeClassifier</c> pursue for outcome
/// cases, applied here to per-request metadata instead.
/// </para>
/// <para>
/// Thread-safe by construction (backed by <see cref="ConcurrentDictionary{TKey, TValue}"/>): nothing prevents
/// a pipeline behavior from forking work internally within an ordinary dispatch, and at least one documented
/// execution path in this library already runs handler-adjacent work concurrently
/// (<c>ParallelNotificationHandlerWrapper</c>'s fan-out).
/// </para>
/// <para>
/// <b>Granularity:</b> <see cref="IRequestContextAccessor"/> is scoped per DI scope (typically one HTTP
/// request), not per mediator dispatch - if a single scope issues more than one <c>Send</c> call (for example
/// a background service reusing a scope across a loop), this bag is shared across all of them, identically to
/// how <see cref="RequestContext.UserId"/>/<see cref="RequestContext.TenantId"/> already behave. Call
/// <see cref="Clear"/> at the top of each iteration, or start a new scope per iteration, if that sharing is
/// not wanted.
/// </para>
/// </remarks>
public sealed class RequestContextItems : IEnumerable<KeyValuePair<string, object?>>
{
    private readonly ConcurrentDictionary<string, object?> items;

    /// <summary>Initializes a new instance of the <see cref="RequestContextItems"/> class, with an empty item bag.</summary>
    public RequestContextItems() => this.items = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="RequestContextItems"/> class, seeded with <paramref name="seed"/>.</summary>
    /// <param name="seed">The initial entries to populate the bag with.</param>
    public RequestContextItems(IEnumerable<KeyValuePair<string, object?>> seed) =>
        this.items = new ConcurrentDictionary<string, object?>(seed, StringComparer.Ordinal);

    /// <summary>Gets or sets the value stored under <paramref name="key"/>, or <see langword="null"/> if not present.</summary>
    /// <param name="key">The key to look up or assign.</param>
    /// <value>The value stored under <paramref name="key"/>, or <see langword="null"/> if not present.</value>
    public object? this[string key]
    {
        get => this.items.TryGetValue(key, out var value) ? value : null;
        set => this.items[key] = value;
    }

    /// <summary>Sets <paramref name="key"/> to <paramref name="value"/>, overwriting any existing value.</summary>
    /// <typeparam name="TValue">The type of <paramref name="value"/>.</typeparam>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    public void Set<TValue>(string key, TValue value) => this.items[key] = value;

    /// <summary>Attempts to get the value stored under <paramref name="key"/>, typed as <typeparamref name="TValue"/>.</summary>
    /// <typeparam name="TValue">The expected type of the stored value.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The stored value, if present and assignable to <typeparamref name="TValue"/>; otherwise the default value.</param>
    /// <returns><see langword="true"/> if <paramref name="key"/> is present and its value is a <typeparamref name="TValue"/>; otherwise <see langword="false"/>.</returns>
    public bool TryGet<TValue>(string key, [MaybeNullWhen(false)] out TValue value)
    {
        if (this.items.TryGetValue(key, out var stored) && stored is TValue typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Gets the value stored under <paramref name="key"/>, creating and storing one via <paramref name="factory"/> if absent.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key to look up or populate.</param>
    /// <param name="factory">Produces the value to store when <paramref name="key"/> is not yet present.</param>
    /// <returns>The existing or newly created value.</returns>
    public TValue GetOrAdd<TValue>(string key, Func<string, TValue> factory) =>
        (TValue)this.items.GetOrAdd(key, k => factory(k))!;

    /// <summary>Removes the entry stored under <paramref name="key"/>, if present.</summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if an entry was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(string key) => this.items.TryRemove(key, out _);

    /// <summary>Determines whether <paramref name="key"/> has a stored value.</summary>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> if <paramref name="key"/> is present; otherwise <see langword="false"/>.</returns>
    public bool ContainsKey(string key) => this.items.ContainsKey(key);

    /// <summary>
    /// Removes every entry. Useful when a DI scope is reused across more than one dispatch (see this type's
    /// remarks on granularity) and a later dispatch should not see an earlier one's entries.
    /// </summary>
    public void Clear() => this.items.Clear();

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this.items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
