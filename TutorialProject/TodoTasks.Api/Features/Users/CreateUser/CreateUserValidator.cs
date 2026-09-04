namespace TodoTasks.Api.Features.Users.CreateUser;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateUserCommand"/> before <see cref="CreateUserHandler"/> ever runs -
/// registered automatically by <c>ScanAssembly</c> in <c>Program.cs</c>, applied by
/// <c>ValidationBehavior</c>.
/// </summary>
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateUserValidator"/> class, declaring the rules a <see cref="CreateUserCommand"/> must satisfy.</summary>
    public CreateUserValidator()
    {
        this.RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
        this.RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
