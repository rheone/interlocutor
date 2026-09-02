namespace Interlocutor.CommonKernel.Tests;

/// <summary>The gateway did not respond within its own service-level time budget - one of <see cref="PaymentOutcome"/>'s failure cases.</summary>
internal readonly record struct GatewayTimeout;
