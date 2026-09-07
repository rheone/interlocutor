namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Handles <see cref="CreateWidgetCommand"/> by inserting one <see cref="Widget"/> through the scoped
/// <see cref="IntegrationTestDbContext"/> - discovered and registered purely via
/// <c>Interlocutor.Generator</c>'s compile-time dispatch, never via runtime assembly scanning (this project
/// never calls <c>ScanAssembly</c>). The insert's own transaction is opened, and later committed or rolled
/// back, entirely by <c>TestDbContextUnitOfWorkBehavior</c> further out in the pipeline - this handler never
/// calls <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/>
/// itself, matching the begin-on-first-write model documentation/transactions-and-store-integration.md describes.
/// </summary>
/// <param name="context">The scoped <see cref="IntegrationTestDbContext"/> this dispatch writes through.</param>
public sealed class CreateWidgetCommandHandler(IntegrationTestDbContext context)
    : ICommandHandler<CreateWidgetCommand, Outcome<Success, ValidationErrors, Forbidden>>
{
    /// <inheritdoc/>
    public async Task<Outcome<Success, ValidationErrors, Forbidden>> HandleAsync(
        CreateWidgetCommand command,
        CancellationToken ct
    )
    {
        await context.Widgets.AddAsync(new Widget { Name = command.Name }, ct).ConfigureAwait(false);
        return default(Success);
    }
}
