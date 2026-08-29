namespace TodoTasks.Api.Features.Users.CreateUser;

/// <summary>The shape of a create-user HTTP request body - deliberately narrower than <see cref="CreateUserCommand"/>.</summary>
/// <param name="DisplayName">The new user's display name.</param>
/// <param name="Email">The new user's email address.</param>
public sealed record CreateUserRequest(string DisplayName, string Email);
