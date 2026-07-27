using Mediance.Abstractions;
using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

namespace Mediance.EntityFrameworkCore.Tests;

public record NonTransactionalTestRequest(int value) : ICommand<string>;

public class TransactionBehaviorTests
{
    [Fact]
    public async Task NonTransactionalRequest_SkipsTransaction()
    {
        var db = new Lazy<Microsoft.EntityFrameworkCore.DbContext>(() =>
            throw new InvalidOperationException("Should not be resolved")
        );
        var behavior = new TransactionBehavior<NonTransactionalTestRequest, string, Microsoft.EntityFrameworkCore.DbContext>(
            db
        );
        var handled = false;

        var result = await behavior.Handle(
            new NonTransactionalTestRequest(1),
            () =>
            {
                handled = true;
                return Task.FromResult<Result<string>>(new Result<string>("done"));
            },
            CancellationToken.None
        );

        Assert.True(handled);
    }
}
