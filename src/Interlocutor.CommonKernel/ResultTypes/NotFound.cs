namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that a requested resource was not found.
/// </summary>
/// <param name="Message">An optional human-readable description of what was not found.</param>
/// <remarks>
/// <para>
/// Use <see cref="NotFound"/> as a result case when a handler's core operation depends on a resource that doesn't exist.
/// This is the outcome case for queries/commands that try to load or modify an entity by ID that's not found.
/// In HTTP, this maps to a 404 status code.
/// </para>
/// <para>
/// Provide a message like <c>"User with ID 42 was not found"</c> for better debugging. If you need to track the
/// entity type and ID separately, use the generic <see cref="NotFound{TId}"/> variant instead.
/// </para>
/// </remarks>
public readonly record struct NotFound(string? Message = null);

/// <summary>
/// Indicates that a resource of a specific type and identifier was not found.
/// </summary>
/// <typeparam name="TId">The type of the resource identifier.</typeparam>
/// <param name="EntityType">The type of entity that was not found.</param>
/// <param name="Id">The identifier of the resource that was searched for.</param>
/// <param name="Message">An optional human-readable description of what was not found.</param>
/// <remarks>
/// <para>
/// This is the typed variant of <see cref="NotFound"/>. Use it when you want to track the entity type and ID
/// separately, enabling callers to reconstruct a not-found message in their own locale or provide context-specific
/// error handling. The <typeparamref name="TId"/> type parameter lets you use the same ID type your domain model uses
/// (e.g., <c>Guid</c>, <c>int</c>, or a composite key).
/// </para>
/// </remarks>
public readonly record struct NotFound<TId>(Type EntityType, TId Id, string? Message = null);
