namespace Interlocutor.Persistence.Abstractions.Tests;

/// <summary>
/// Covers the "expand" half of the wide <c>Cancel</c> rename: <see cref="IUnitOfWork.CancelAsync"/> is a default
/// interface implementation that forwards to the existing, unchanged <see cref="IUnitOfWork.Cancel"/> member.
/// The fake below implements only <see cref="IUnitOfWork.Cancel"/> - never <see cref="IUnitOfWork.CancelAsync"/>
/// - to prove that no implementer anywhere needs to change for the new member to work.
/// </summary>
public class UnitOfWorkAsyncForwardingTests
{
    [Fact]
    public async Task CancelAsync_ForwardsToCancel()
    {
        // Arrange
        var cancelCallCount = 0;
        IUnitOfWork unitOfWork = new AsyncForwardingUnitOfWork(() => cancelCallCount++);

        // Act
        await unitOfWork.CancelAsync(TestContext.Current.CancellationToken);

        // Assert - only Cancel (not CancelAsync) was implemented, so this proves the default forwarded to it.
        Assert.Equal(1, cancelCallCount);
    }
}

/// <summary>Implements only <see cref="IUnitOfWork.Cancel"/> - never the new <see cref="IUnitOfWork.CancelAsync"/>.</summary>
public class AsyncForwardingUnitOfWork(Action onCancel) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Cancel(CancellationToken cancellationToken = default)
    {
        onCancel();
        return Task.CompletedTask;
    }
}
