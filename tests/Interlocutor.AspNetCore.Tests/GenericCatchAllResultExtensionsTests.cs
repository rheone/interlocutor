namespace Interlocutor.AspNetCore.Tests;

using System.Linq.Expressions;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Tests for the generic <c>ToActionResult&lt;TResponse&gt;</c>/<c>ToMinimalApiResult&lt;TResponse&gt;</c>
/// catch-all overloads on <see cref="ResultExtensions"/>: that the six built-in union arities still bind to
/// their dedicated overloads at compile time (ticket 13's overloads), and that the catch-all is what finally
/// lets a hand-rolled, non-union <see cref="IOperationOutcome"/> implementation produce an HTTP result at all.
/// </summary>
public class GenericCatchAllResultExtensionsTests
{
    private static readonly IOutcomeCaseRegistry Registry = new OutcomeCaseRegistryBuilder()
        .RegisterExtractor<CustomOperationOutcome>(
            (CustomOperationOutcome response, out Type caseType, out object? caseValue) =>
            {
                caseType = response.CaseType;
                caseValue = response.CaseValue;
                return true;
            }
        )
        .Build();

    // --- Overload resolution: the dedicated arity overloads must still win over the catch-all ---
    //
    // Both `result.ToActionResult()` calls below would compile and behave identically whether bound to the
    // dedicated arity-2 overload or the new catch-all - both end up producing an OkObjectResult/mapped
    // failure. Behavioral assertions on the *result* can't distinguish which overload the compiler actually
    // picked. An expression tree captures the compiler's overload-resolution decision directly: the
    // MethodCallExpression.Method it produces IS the exact MethodInfo the compiler bound, so asserting on its
    // shape (generic arity, parameter count) is a real regression test for C#'s "constructed generic beats
    // bare type parameter" specificity rule, not an assertion of faith.

    [Fact]
    public void ToActionResult_ArityTwoOverload_BindsDedicatedOverloadNotCatchAll()
    {
        // Arrange
        Expression<Func<OperationOutcome<string, Failure>, IActionResult>> expression = result => result.ToActionResult();

        // Act
        var method = ((MethodCallExpression)expression.Body).Method;

        // Assert - the dedicated arity-2 overload is `ToActionResult<TSuccess, TFailure>(this
        // OperationOutcome<TSuccess, TFailure>, IOutcomeStatusMapper? mapper = null)` - two type arguments,
        // two parameters (result, mapper). The catch-all is `ToActionResult<TResponse>(this TResponse,
        // IOutcomeStatusMapper?, IOutcomeCaseRegistry?)` - one type argument, three parameters. The two
        // overloads are distinguishable by shape alone.
        Assert.Equal(2, method.GetGenericArguments().Length);
        Assert.Equal([typeof(string), typeof(Failure)], method.GetGenericArguments());
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void ToMinimalApiResult_ArityTwoOverload_BindsDedicatedOverloadNotCatchAll()
    {
        // Arrange
        Expression<Func<OperationOutcome<string, Failure>, IResult>> expression = result => result.ToMinimalApiResult();

        // Act
        var method = ((MethodCallExpression)expression.Body).Method;

        // Assert
        Assert.Equal(2, method.GetGenericArguments().Length);
        Assert.Equal([typeof(string), typeof(Failure)], method.GetGenericArguments());
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void ToActionResult_ArityFiveOverload_BindsDedicatedOverloadNotCatchAll()
    {
        // Arrange
        Expression<Func<OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors>, IActionResult>> expression =
            result => result.ToActionResult();

        // Act
        var method = ((MethodCallExpression)expression.Body).Method;

        // Assert - five type arguments (TSuccess + four case types) is only possible on the dedicated arity-5
        // overload - the catch-all always has exactly one type argument (TResponse).
        Assert.Equal(5, method.GetGenericArguments().Length);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void ToActionResult_VoidOutcome_BindsDedicatedNonGenericOverloadNotCatchAll()
    {
        // Arrange
        Expression<Func<OperationOutcome, IActionResult>> expression = result => result.ToActionResult();

        // Act
        var method = ((MethodCallExpression)expression.Body).Method;

        // Assert - the dedicated void overload isn't even generic; the catch-all always has one type parameter.
        Assert.False(method.IsGenericMethod);
        Assert.Equal(2, method.GetParameters().Length);
    }

    // --- Catch-all end to end, against a hand-rolled, non-union IOperationOutcome ---

    [Fact]
    public void ToActionResult_CatchAll_HandRolledOutcome_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        var response = CustomOperationOutcome.FromSuccess(default(Success));

        // Act
        var actionResult = response.ToActionResult(caseRegistry: Registry);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(default(Success), okResult.Value);
    }

    [Fact]
    public void ToActionResult_CatchAll_HandRolledOutcome_WithFailure_MapsThroughStatusMapper()
    {
        // Arrange
        var response = CustomOperationOutcome.FromFailure(new NotFound("Widget not found"));

        // Act
        var actionResult = response.ToActionResult(caseRegistry: Registry);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_CatchAll_HandRolledOutcome_WithCustomMapper_UsesCustomMapping()
    {
        // Arrange
        var response = CustomOperationOutcome.FromFailure(new Forbidden("No access"));

        // Act
        var actionResult = response.ToActionResult(Registry, new TeapotOutcomeStatusMapper());

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status418ImATeapot, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToMinimalApiResult_CatchAll_HandRolledOutcome_WithSuccess_CreatesOkResult()
    {
        // Arrange
        var response = CustomOperationOutcome.FromSuccess(default(Success));

        // Act
        var minimalResult = response.ToMinimalApiResult(caseRegistry: Registry);

        // Assert
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_CatchAll_HandRolledOutcome_WithFailure_MapsThroughStatusMapper()
    {
        // Arrange
        var response = CustomOperationOutcome.FromFailure(new Conflict("Data changed", "stale.data"));

        // Act
        var minimalResult = response.ToMinimalApiResult(caseRegistry: Registry);

        // Assert
        var statusCode = (int?)minimalResult.GetType().GetProperty("StatusCode")?.GetValue(minimalResult);
        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
    }

    [Fact]
    public void ToActionResult_CatchAll_HandRolledOutcome_WithoutRegisteredRegistry_ThrowsOutcomeCaseNotSupportedException()
    {
        // Arrange - a null caseRegistry falls back to DefaultOutcomeCaseRegistry, which cannot decompose a
        // non-union IOperationOutcome reflectively (this is exactly what registering a custom registry, as the
        // tests above do, is for).
        var response = CustomOperationOutcome.FromSuccess(default(Success));

        // Act & Assert
        Assert.Throws<OutcomeCaseNotSupportedException>(() => response.ToActionResult(caseRegistry: null));
    }

    // --- Catch-all also handles the built-in unions correctly when called explicitly as TResponse ---
    // (proves the catch-all's own extraction/classification logic is correct, independent of which overload
    // a given call site happens to bind to).

    [Fact]
    public void ToActionResult_CatchAll_CalledExplicitlyAgainstRealUnion_BehavesLikeDedicatedOverload()
    {
        // Arrange
        OperationOutcome<string, Failure> result = new(new Failure("boom"));

        // Act - explicit type argument forces the compiler to bind this specific generic method rather than
        // let overload resolution pick for it, so this exercises the catch-all's own logic directly.
        var actionResult = ResultExtensions.ToActionResult<OperationOutcome<string, Failure>>(result, caseRegistry: null);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }
}
