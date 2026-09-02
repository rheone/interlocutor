namespace Interlocutor.CommonKernel.Tests;

/// <summary>An error at the network layer, distinct from a gateway-reported timeout - one of <see cref="PaymentOutcome"/>'s failure cases.</summary>
/// <param name="Detail">The underlying network error's own message.</param>
internal readonly record struct NetworkError(string Detail);
