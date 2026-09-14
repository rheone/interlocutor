namespace Interlocutor.IntegrationTests;

using global::FluentValidation;

/// <summary>
/// Validates <see cref="CreateWidgetCommand"/> - the rule <c>ValidationBehavior</c> runs to prove the
/// canonical chain's validation stage short-circuits before the transaction opens, when it fails.
/// </summary>
public sealed class CreateWidgetCommandValidator : AbstractValidator<CreateWidgetCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateWidgetCommandValidator"/> class.</summary>
    public CreateWidgetCommandValidator() => this.RuleFor(command => command.Name).NotEmpty();
}
