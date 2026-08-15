namespace Interlocutor.EntityFrameworkCore.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

public class TransactionBehaviorTests
{
    [Fact]
    public async Task ExternalStoreRequest_SkipsTransaction_EvenThoughAlsoTransactional()
    {
        var db = new Lazy<Microsoft.EntityFrameworkCore.DbContext>(() =>
            throw new InvalidOperationException("Should not be resolved")
        );
        var behavior = new TransactionBehavior<
            RequestClaimingBothTransactionMarkers,
            OperationOutcome<string, Failure>,
            Microsoft.EntityFrameworkCore.DbContext
        >(db);
        var handled = false;

        var result = await behavior.Handle(
            new RequestClaimingBothTransactionMarkers(1),
            () =>
            {
                handled = true;
                return Task.FromResult<OperationOutcome<string, Failure>>("done");
            },
            CancellationToken.None
        );

        Assert.True(handled);
        Assert.Equal("done", result.Value);
    }

    [Fact]
    public void TRequest_Constraint_RejectsNonTransactionalRequestAtCompileTime()
    {
        // A request that implements neither marker cannot close TransactionBehavior<,,>'s TRequest parameter
        // at all — proven here via reflection (the same MakeGenericType mechanism both Microsoft DI and
        // Autofac use internally to decide whether an open generic registration applies), since the compiler
        // itself already rejects this at every other call site in this file and would reject a source-level
        // attempt to write TransactionBehavior<NonTransactionalRequest, OperationOutcome<string, Failure>, DbContext> outright.
        var openGeneric = typeof(TransactionBehavior<,,>);
        var nonTransactionalRequest = typeof(NonTransactionalRequest);
        var response = typeof(OperationOutcome<string, Failure>);
        var context = typeof(Microsoft.EntityFrameworkCore.DbContext);

        var ex = Assert.Throws<ArgumentException>(() =>
            openGeneric.MakeGenericType(nonTransactionalRequest, response, context)
        );
        Assert.Contains("constraint", ex.Message, StringComparison.OrdinalIgnoreCase);

        // The same closure succeeds once the request satisfies ITransactionalRequest.
        var closed = openGeneric.MakeGenericType(typeof(TransactionalTestRequest), response, context);
        Assert.NotNull(closed);
    }

    private sealed record NonTransactionalRequest(int Value) : ICommand<OperationOutcome<string, Failure>>;

    private sealed record TransactionalTestRequest(int Value)
        : ICommand<OperationOutcome<string, Failure>>,
            ITransactionalRequest;

    /// <summary>
    /// A request implementing both <see cref="ITransactionalRequest"/> and <see cref="IExternalStoreRequest"/> —
    /// the malformed-but-not-impossible case the compile-time <c>TRequest : ITransactionalRequest</c> constraint
    /// cannot rule out (a constraint can only require a marker's presence, never another marker's absence), so
    /// <see cref="TransactionBehavior{TRequest,TResponse,TContext}"/> retains a residual runtime check for it.
    /// </summary>
    private sealed record RequestClaimingBothTransactionMarkers(int Value)
        : ICommand<OperationOutcome<string, Failure>>,
            ITransactionalRequest,
            IExternalStoreRequest;
}
