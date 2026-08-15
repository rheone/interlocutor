namespace Interlocutor.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.NHibernate;

public record NonTransactionalTestRequest(int Value) : ICommand<OperationOutcome<string, Failure>>;

public class TransactionBehaviorTests
{
    [Fact]
    public async Task NonTransactionalRequest_SkipsSession()
    {
        var session = new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved"));
        var behavior = new NHibernate.TransactionBehavior<NonTransactionalTestRequest, string>(session);
        var handled = false;

        var result = await behavior.Handle(
            new NonTransactionalTestRequest(1),
            () =>
            {
                handled = true;
                return Task.FromResult("done");
            },
            CancellationToken.None
        );

        Assert.True(handled);
    }
}
