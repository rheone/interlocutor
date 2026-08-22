namespace Interlocutor.Sample.Handlers;

using Interlocutor.Sample.Handlers.Tags;
using Interlocutor.Sample.Handlers.Todos;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The sample's only <see cref="DbContext"/>. Deliberately thin - two entity sets, no fluent configuration
/// beyond what EF Core's conventions already infer, since the point of the sample is the mediator
/// composition around it, not the persistence model.
/// </summary>
public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    /// <summary>Gets the to-do items table.</summary>
    /// <value>The to-do items table.</value>
    public DbSet<TodoItem> TodoItems => this.Set<TodoItem>();

    /// <summary>Gets the tags table.</summary>
    /// <value>The tags table.</value>
    public DbSet<TagItem> Tags => this.Set<TagItem>();
}
