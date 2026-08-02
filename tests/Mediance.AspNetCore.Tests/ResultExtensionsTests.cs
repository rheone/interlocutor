//using Mediance.CommonKernel;
//using Mediance.CommonKernel.ResultTypes;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace Mediance.AspNetCore.Tests;

//public class ResultExtensionsTests
//{
//    [Fact]
//    public void ToActionResult_WithSuccessValue_ReturnsOkObjectResult()
//    {
//        OperationOutcome<string> result = new("hello");
//        var actionResult = result.ToActionResult();

//        var okResult = Assert.IsType<OkObjectResult>(actionResult);
//        Assert.Equal("hello", okResult.Value);
//    }

//    [Fact]
//    public void ToActionResult_WithSuccessNoPayload_ReturnsNoContent()
//    {
//        OperationOutcome result = new(new Success());
//        var actionResult = result.ToActionResult();

//        Assert.IsType<NoContentResult>(actionResult);
//    }

//    [Fact]
//    public void ToActionResult_WithNotFound_ReturnsNotFoundResult()
//    {
//        OperationOutcome<string> result = new(new NotFound("Widget", 42));
//        var actionResult = result.ToActionResult();

//        Assert.IsType<NotFoundResult>(actionResult);
//    }

//    [Fact]
//    public void ToActionResult_WithForbidden_ReturnsStatusCode403()
//    {
//        OperationOutcome<string> result = new(new Forbidden("No access"));
//        var actionResult = result.ToActionResult();

//        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
//        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
//    }

//    [Fact]
//    public void ToActionResult_WithValidationFailed_ReturnsBadRequest()
//    {
//        var errors = new Dictionary<string, IReadOnlyList<(string, string)>> { ["Name"] = [("NotEmpty", "Name is required")] };
//        OperationOutcome<string> result = new(new ValidationFailed(errors));
//        var actionResult = result.ToActionResult();

//        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
//        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
//        Assert.Contains("Name", problemDetails.Errors);
//    }

//    [Fact]
//    public void ToActionResult_WithRuleViolation_Returns422()
//    {
//        OperationOutcome<string> result = new(new ValidationFailure("insufficient.balance", "Not enough funds"));
//        var actionResult = result.ToActionResult();

//        var objectResult = Assert.IsType<UnprocessableEntityObjectResult>(actionResult);
//        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
//        Assert.Equal("insufficient.balance", problemDetails.Title);
//    }

//    [Fact]
//    public void ToActionResult_WithConflict_Returns409()
//    {
//        OperationOutcome<string> result = new(new Conflict("stale.data", "Data changed"));
//        var actionResult = result.ToActionResult();

//        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult);
//        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
//        Assert.Equal("stale.data", problemDetails.Title);
//    }
//}
