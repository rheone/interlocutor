namespace TodoTasks.Api.Persistence;

using FluentNHibernate.Mapping;
using TodoTasks.Api.Domain;

/// <summary>
/// The Fluent NHibernate mapping for <see cref="User"/> - picked up automatically by
/// <see cref="TodoTasksSessionFactory.Build"/>, which scans this assembly for every
/// <c>ClassMap&lt;TEntity&gt;</c> it can find.
/// </summary>
/// <remarks>
/// Mirrors <c>TodoTaskMap</c>'s shape (an upcoming ticket) - see
/// <c>documentation/beginners-guide-todo-api.md</c> Step 4 for the pattern both follow.
/// </remarks>
public sealed class UserMap : ClassMap<User>
{
    /// <summary>Initializes a new instance of the <see cref="UserMap"/> class, declaring how <see cref="User"/>'s members map to columns.</summary>
    public UserMap()
    {
        this.Id(u => u.Id);
        this.Map(u => u.DisplayName).Not.Nullable().Length(200);
        this.Map(u => u.Email).Not.Nullable().Length(320); // 320 = the longest an RFC 5321 email can be.
    }
}
