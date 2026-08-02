namespace Mediance.NHibernate.Tests;

using global::NHibernate;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.NHibernate;

public record NonTransactionalTestRequest(int Value) : ICommand<string>;

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
