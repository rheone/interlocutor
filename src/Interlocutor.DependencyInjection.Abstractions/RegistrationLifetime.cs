namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Container-neutral service lifetime for an <see cref="InterlocutorRegistration"/>.
/// </summary>
/// <remarks>
/// Mirrors the lifetime concepts every supported container exposes under its own name
/// (for example, Microsoft.Extensions.DependencyInjection's <c>ServiceLifetime</c> or Autofac's
/// per-registration scope methods) so the neutral registration record never has to reference a
/// specific container's types.
/// </remarks>
public enum RegistrationLifetime
{
    /// <summary>A single instance is created and shared for the lifetime of the container.</summary>
    Singleton,

    /// <summary>A single instance is created and shared within one DI scope.</summary>
    Scoped,

    /// <summary>A new instance is created every time the service is resolved.</summary>
    Transient,
}
