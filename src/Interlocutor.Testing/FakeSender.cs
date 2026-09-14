namespace Interlocutor.Testing;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;

/// <summary>
/// A fake <see cref="ISender"/> for unit tests: stubs a response for a given request type and records every
/// request it receives, without wiring a real pipeline, container, or handler.
/// </summary>
/// <remarks>
/// <para>
/// Construct one, call <see cref="SetResponse{TRequest, TResponse}(TResponse)"/> once per request type the
/// code under test dispatches, then pass the fake anywhere an <see cref="ISender"/> is expected. After the
/// call, assert against <see cref="ReceivedRequests"/> for what was sent, and against the awaited result for
/// what came back - no handler, pipeline behavior, or service provider is involved.
/// </para>
/// <para>
/// A response is looked up by the request's runtime type (<see cref="object.GetType"/>), so one stub answers
/// all four <see cref="ISender"/> overloads - queries, value-returning commands, bare commands, and the
/// runtime-typed <see cref="ISender.SendAsync(object, CancellationToken)"/> overload - for that request type.
/// Every overload throws <see cref="InvalidOperationException"/> if no response was stubbed for the request's
/// runtime type; there is no implicit default response.
/// </para>
/// <para>
/// Not thread-safe: like most test doubles, a <see cref="FakeSender"/> is expected to be exercised by one
/// test at a time, not shared across concurrently running tests.
/// </para>
/// </remarks>
public sealed class FakeSender : ISender
{
    private readonly Dictionary<Type, object> stubbedResponses = [];
    private readonly List<object> receivedRequests = [];

    /// <summary>Gets every request instance passed to any <c>SendAsync</c> overload, in dispatch order.</summary>
    /// <value>A snapshot-free view backed by the list this fake appends to as requests are sent.</value>
    public IReadOnlyList<object> ReceivedRequests => this.receivedRequests;

    /// <summary>
    /// Stubs the response this <see cref="FakeSender"/> returns for every request whose runtime type is
    /// exactly <typeparamref name="TRequest"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type to stub a response for.</typeparam>
    /// <typeparam name="TResponse">The <see cref="IUnion"/>-shaped response type.</typeparam>
    /// <param name="response">The response to return whenever a <typeparamref name="TRequest"/> is sent.</param>
    /// <returns>This <see cref="FakeSender"/>, so calls can be chained when stubbing more than one request type.</returns>
    public FakeSender SetResponse<TRequest, TResponse>(TResponse response)
        where TResponse : IUnion
    {
        this.stubbedResponses[typeof(TRequest)] = response;
        return this;
    }

    /// <inheritdoc/>
    public Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        where TResponse : IUnion => this.ResolveAsync<TResponse>(query);

    /// <inheritdoc/>
    public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IUnion => this.ResolveAsync<TResponse>(command);

    /// <inheritdoc/>
    public Task<TResult> SendAsync<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IUnion => this.ResolveAsync<TResult>(command);

    /// <inheritdoc/>
    /// <remarks>
    /// The <see cref="RequiresUnreferencedCodeAttribute"/>/<see cref="RequiresDynamicCodeAttribute"/> pair below
    /// exists only to match <see cref="ISender.SendAsync(object, CancellationToken)"/>'s own annotation, which
    /// every implementation must carry identically - this fake performs no reflection of its own; it looks up
    /// <paramref name="request"/>'s runtime type in a plain dictionary.
    /// </remarks>
    [RequiresUnreferencedCode(
        "Present only to match ISender.SendAsync(object, CancellationToken)'s annotation - this fake performs no reflection of its own."
    )]
    [RequiresDynamicCode(
        "Present only to match ISender.SendAsync(object, CancellationToken)'s annotation - this fake performs no reflection of its own."
    )]
    public Task<IUnion> SendAsync(object request, CancellationToken ct = default) => this.ResolveAsync<IUnion>(request);

    /// <summary>Records <paramref name="request"/> and resolves the stubbed response for its runtime type.</summary>
    /// <typeparam name="TResponse">The response type the caller expects back.</typeparam>
    /// <param name="request">The request instance being dispatched.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// No response was stubbed via <see cref="SetResponse{TRequest, TResponse}(TResponse)"/> for
    /// <paramref name="request"/>'s runtime type.
    /// </exception>
    private Task<TResponse> ResolveAsync<TResponse>(object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        this.receivedRequests.Add(request);

        var requestType = request.GetType();
        if (!this.stubbedResponses.TryGetValue(requestType, out var response))
        {
            throw new InvalidOperationException(
                $"FakeSender has no stubbed response for request type '{requestType}'. Call "
                    + $"{nameof(this.SetResponse)}<{requestType.Name}, TResponse>(...) before sending it."
            );
        }

        return Task.FromResult((TResponse)response);
    }
}
