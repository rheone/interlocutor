namespace Interlocutor.AspNetCore.Tests;

using System.Net;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

/// <summary>
/// Manual sanity check: a minimal, in-memory ASP.NET Core test host with one endpoint returning a built-in
/// arity-4 union (bound to its dedicated <see cref="ResultExtensions"/> overload) and one endpoint returning
/// a hand-rolled, non-union <see cref="IOperationOutcome"/> (bound to the generic catch-all overload), hit
/// with a real <see cref="HttpClient"/> to confirm both produce the expected response shape and status code
/// over an actual HTTP round trip - not just against the in-process <see cref="Microsoft.AspNetCore.Http.IResult"/>/
/// <see cref="Microsoft.AspNetCore.Mvc.IActionResult"/> objects the other tests in this project assert against
/// directly.
/// </summary>
public sealed class ResultExtensionsHttpHostTests : IAsyncLifetime
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

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        this._app = builder.Build();

        this._app.MapGet(
            "/union-success",
            () =>
            {
                OperationOutcome<string, NotFound, Forbidden, Failure> result = "hello from a union";
                return result.ToMinimalApiResult();
            }
        );
        this._app.MapGet(
            "/union-notfound",
            () =>
            {
                OperationOutcome<string, NotFound, Forbidden, Failure> result = new NotFound("Widget not found");
                return result.ToMinimalApiResult();
            }
        );
        this._app.MapGet(
            "/custom-success",
            () =>
            {
                var result = CustomOperationOutcome.FromSuccess(default(Success));
                return result.ToMinimalApiResult(caseRegistry: Registry);
            }
        );
        this._app.MapGet(
            "/custom-conflict",
            () =>
            {
                var result = CustomOperationOutcome.FromFailure(new Conflict("Data changed", "stale.data"));
                return result.ToMinimalApiResult(caseRegistry: Registry);
            }
        );

        await this._app.StartAsync();
        this._client = this._app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        this._client.Dispose();
        await this._app.DisposeAsync();
    }

    [Fact]
    public async Task Get_UnionSuccessEndpoint_Returns200WithBody()
    {
        // Arrange - the endpoint and its client are wired up in InitializeAsync.

        // Act
        var response = await this._client.GetAsync(
            new Uri("/union-success", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hello from a union", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_UnionNotFoundEndpoint_Returns404()
    {
        // Arrange - the endpoint and its client are wired up in InitializeAsync.

        // Act
        var response = await this._client.GetAsync(
            new Uri("/union-notfound", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_CustomOutcomeSuccessEndpoint_Returns200()
    {
        // Arrange - the endpoint and its client are wired up in InitializeAsync.

        // Act
        var response = await this._client.GetAsync(
            new Uri("/custom-success", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_CustomOutcomeConflictEndpoint_Returns409WithProblemDetailsBody()
    {
        // Arrange - the endpoint and its client are wired up in InitializeAsync.

        // Act
        var response = await this._client.GetAsync(
            new Uri("/custom-conflict", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("stale.data", body);
        Assert.Contains("Data changed", body);
    }
}
