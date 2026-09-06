namespace TodoTasks.Api.Features.Users.SendWelcomeEmail;

using Interlocutor.Abstractions.Commands;

/// <summary>
/// Sends a welcome email to an existing <see cref="TodoTasks.Api.Domain.User"/> - a deliberately
/// standalone feature, not wired into <c>CreateUser</c> or any other flow, that exists to be a real,
/// working example of a hand-rolled <see cref="System.Runtime.CompilerServices.IUnion"/> response type
/// rather than this project's usual compiler-generated <c>Outcome&lt;...&gt;</c> union - see
/// <see cref="SendWelcomeEmailResult"/>.
/// </summary>
/// <param name="UserId">The user to email.</param>
public sealed record SendWelcomeEmailCommand(int UserId) : ICommand<SendWelcomeEmailResult>;
