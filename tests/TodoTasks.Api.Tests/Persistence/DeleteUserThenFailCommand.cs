namespace TodoTasks.Api.Tests.Persistence;

using TodoTasks.Api.Features.Users.DeleteUser;

/// <summary>
/// A test-only command, structurally identical to <see cref="DeleteUserCommand"/>, that
/// <see cref="DeleteUserThenFailHandler"/> pairs with to force a failure after the cascade has already
/// flushed - see <see cref="DeleteUserCascadeTests"/>'s load-bearing rollback test for why this exists.
/// </summary>
/// <param name="Id">The user to delete.</param>
internal sealed record DeleteUserThenFailCommand(int Id);
