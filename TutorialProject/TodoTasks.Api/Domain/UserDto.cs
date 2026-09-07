namespace TodoTasks.Api.Domain;

/// <summary>
/// The shape of a <see cref="User"/> returned to callers - deliberately narrower than the entity itself,
/// the same way a future <c>TodoTaskDto</c> stands in front of <c>TodoTask</c>. Today the two shapes
/// happen to match member-for-member, but keeping them as separate types means the API's response shape
/// never accidentally changes just because the persistence model does.
/// </summary>
/// <param name="Id">The user's identifier.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
public sealed record UserDto(int Id, string DisplayName, string Email)
{
    /// <summary>Projects a <see cref="User"/> entity to its response shape.</summary>
    /// <param name="user">The entity to project.</param>
    /// <returns>The equivalent <see cref="UserDto"/>.</returns>
    public static UserDto FromEntity(User user) => new(user.Id, user.DisplayName, user.Email);
}
