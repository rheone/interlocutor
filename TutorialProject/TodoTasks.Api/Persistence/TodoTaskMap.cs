namespace TodoTasks.Api.Persistence;

using FluentNHibernate.Mapping;
using TodoTasks.Api.Domain;

/// <summary>
/// The Fluent NHibernate mapping for <see cref="TodoTask"/> - picked up automatically by
/// <see cref="TodoTasksSessionFactory.Build"/>, which scans this assembly for every
/// <c>ClassMap&lt;TEntity&gt;</c> it can find. Matches
/// <c>documentation/beginners-guide-todo-api.md</c> Step 4 exactly.
/// </summary>
public sealed class TodoTaskMap : ClassMap<TodoTask>
{
    /// <summary>Initializes a new instance of the <see cref="TodoTaskMap"/> class, declaring how <see cref="TodoTask"/>'s members map to columns.</summary>
    public TodoTaskMap()
    {
        this.Id(t => t.Id);
        this.Map(t => t.Title).Not.Nullable().Length(200);
        this.Map(t => t.Notes).Nullable().Length(2000);
        this.Map(t => t.OwnerId).Not.Nullable();
        this.Map(t => t.IsCompleted).Not.Nullable();
        // CustomType: SQLite's NHibernate dialect has no native DbType.DateTimeOffset mapping at all -
        // see DateTimeOffsetAsStringUserType's own remarks for the confirmed error this avoids.
        this.Map(t => t.CreatedAtUtc).Not.Nullable().CustomType<DateTimeOffsetAsStringUserType>();
    }
}
