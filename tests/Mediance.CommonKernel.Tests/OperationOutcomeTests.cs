//using System.Diagnostics;
//using Mediance.CommonKernel.ResultTypes;

//namespace Mediance.CommonKernel.Tests;

//public class OperationOutcomeTests
//{
//    [Fact]
//    public void Result_WithValue_CanMatchSuccess()
//    {
//        var result = CreateSuccessResult(42);
//        var value = result.Value switch
//        {
//            int v => v,
//            Failure => -1,
//            _ => throw new UnreachableException(),
//        };
//        Assert.Equal(42, value);
//    }

//    [Fact]
//    public void Result_WithFailure_CanMatchFailure()
//    {
//        var result = CreateFailureResult(new NotFound("Entity", 1));
//        var value = result.Value switch
//        {
//            int => -1,
//            Failure f => f switch
//            {
//                NotFound => 0,
//                _ => -1,
//            },
//            _ => throw new UnreachableException(),
//        };
//        Assert.Equal(0, value);
//    }

//    [Fact]
//    public void Result_NonGeneric_Success_MatchesAsSuccess()
//    {
//        var result = new OperationOutcome(new Success());
//        var outcome = result.Value switch
//        {
//            Success => true,
//            Failure => false,
//            _ => throw new UnreachableException(),
//        };
//        Assert.True(outcome);
//    }

//    [Fact]
//    public void Success_Record_CanBeConstructed()
//    {
//        var success = new Success();
//        Assert.NotNull(success);
//    }

//    [Fact]
//    public void Failure_Cases_HaveExpectedProperties()
//    {
//        var validationFailed = new ValidationFailed(
//            new Dictionary<string, IReadOnlyList<(string, string)>> { ["Name"] = [("NotEmpty", "Name is required")] }
//        );
//        Assert.Single(validationFailed.errors);

//        var ruleViolation = new ValidationFailure("insufficient.balance", "Balance too low");
//        Assert.Equal("insufficient.balance", ruleViolation.code);

//        var notFound = new NotFound("Widget", 99);
//        Assert.Equal("Widget", notFound.entityType);

//        var forbidden = new Forbidden("Not your resource");
//        Assert.Equal("Not your resource", forbidden.Reason);

//        var conflict = new Conflict("stale.data", "Data was modified");
//        Assert.Equal("stale.data", conflict.code);
//    }

//    [Fact]
//    public void Forbidden_WithDefaultReason_IsNull()
//    {
//        var forbidden = new Forbidden();
//        Assert.Null(forbidden.Reason);
//    }

//    private static OperationOutcome<int> CreateSuccessResult(int value) => new(value);

//    private static OperationOutcome<int> CreateFailureResult(Failure failure) => new(failure);
//}
