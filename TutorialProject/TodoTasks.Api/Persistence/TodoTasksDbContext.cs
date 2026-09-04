namespace TodoTasks.Api.Persistence;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// The EF Core <see cref="DbContext"/> this project runs on, against an in-memory SQLite database - see
/// <c>Program.cs</c> for the connection strategy that keeps that database alive across the scoped
/// instances of this type each request gets.
/// </summary>
/// <remarks>
/// Deliberately empty for now - this project has no features yet (see
/// .scratch/tutorial-project-common-stack/issues/02-project-and-test-harness-scaffolding.md). Each
/// feature ticket that lands a mapped entity adds its own <c>DbSet&lt;TEntity&gt;</c> property and
/// <c>OnModelCreating</c> configuration here, the same way
/// <c>documentation/sample-app-walkthrough.md</c>'s <c>Domain/</c>/<c>Persistence/</c> folders describe
/// entities and mappings as the two things shared across every vertical slice.
/// </remarks>
/// <param name="options">The options this context is configured with - see <c>Program.cs</c>'s <c>AddDbContext</c> call.</param>
public sealed class TodoTasksDbContext(DbContextOptions<TodoTasksDbContext> options) : DbContext(options);
