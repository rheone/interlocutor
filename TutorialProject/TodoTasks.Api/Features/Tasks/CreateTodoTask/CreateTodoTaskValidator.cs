namespace TodoTasks.Api.Features.Tasks.CreateTodoTask;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateTodoTaskCommand"/> before <see cref="CreateTodoTaskHandler"/> ever runs -
/// registered automatically via <c>RegisterAssemblyTypes(...).AsClosedTypesOf(typeof(IValidator&lt;&gt;))</c>
/// in <c>Program.cs</c>, applied by <c>ValidationBehavior</c> (part of <c>UseRecommendedChain()</c>).
/// </summary>
public sealed class CreateTodoTaskValidator : AbstractValidator<CreateTodoTaskCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateTodoTaskValidator"/> class, declaring the rules a <see cref="CreateTodoTaskCommand"/> must satisfy.</summary>
    public CreateTodoTaskValidator()
    {
        this.RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        this.RuleFor(c => c.Notes).MaximumLength(2000);
    }
}
