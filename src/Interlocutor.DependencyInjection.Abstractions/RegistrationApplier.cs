namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Drives the seven-step order every container adapter's <c>AddInterlocutor</c> applies an
/// <see cref="InterlocutorBuilder"/>'s accumulated registrations in - handlers, generated registrations,
/// stages, stage factories, behaviors, matched registrations, then try-add defaults - against three small,
/// container-agnostic callbacks a container adapter supplies.
/// </summary>
/// <remarks>
/// <para>
/// Container-neutral: this type knows nothing about <c>IServiceCollection</c>, <c>ContainerBuilder</c>, or
/// any other container-specific primitive. The three callbacks - <paramref name="addRegistration"/>,
/// <paramref name="addFactory"/>, and <paramref name="tryAddDefault"/> - are where a container adapter's own
/// translation (open/closed generic service-type resolution, its container's lifetime enum, try-add
/// semantics) lives; this type's only job is calling the right one, for the right registration list, in the
/// right order. A third container adapter only has to implement those three callbacks, not re-derive the
/// seven-step order by reading a sibling package.
/// </para>
/// <para>
/// <paramref name="addRegistration"/> is reused across five of the seven steps (handlers, generated
/// registrations, stages, behaviors, matched registrations) rather than each step taking its own callback,
/// because all five are ordinary <see cref="InterlocutorRegistration"/> entries applied with the same
/// add-not-try-add semantics - only <see cref="InterlocutorFactoryRegistration"/> entries (stage factories)
/// and try-add entries (defaults) need a different shape or semantic, hence their own callbacks.
/// </para>
/// </remarks>
/// <param name="addRegistration">
/// Applies one <see cref="InterlocutorRegistration"/> with normal (non-try-add) semantics - used for
/// handlers, generated registrations, stages, behaviors, and matched registrations.
/// </param>
/// <param name="addFactory">Applies one <see cref="InterlocutorFactoryRegistration"/> - used for stage factories.</param>
/// <param name="tryAddDefault">
/// Applies one <see cref="InterlocutorRegistration"/> with try-add semantics - used for
/// <see cref="InterlocutorBuilder.Defaults"/>, so a consumer's own registration of the same service type
/// always wins.
/// </param>
public sealed class RegistrationApplier(
    Action<InterlocutorRegistration> addRegistration,
    Action<InterlocutorFactoryRegistration> addFactory,
    Action<InterlocutorRegistration> tryAddDefault
)
{
    /// <summary>
    /// Applies every registration <paramref name="builder"/> has accumulated, in the same seven-step order
    /// every container adapter's <c>AddInterlocutor</c> uses: <see cref="InterlocutorBuilder.Handlers"/>,
    /// <see cref="InterlocutorBuilder.GeneratedRegistrations"/>, <see cref="InterlocutorBuilder.Stages"/>,
    /// <see cref="InterlocutorBuilder.StageFactories"/>, <see cref="InterlocutorBuilder.Behaviors"/>,
    /// <see cref="InterlocutorBuilder.MatchedRegistrations"/>, then <see cref="InterlocutorBuilder.Defaults"/>.
    /// </summary>
    /// <param name="builder">The builder whose accumulated registrations to apply.</param>
    public void Apply(InterlocutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ApplyEach(builder.Handlers, addRegistration);
        ApplyEach(builder.GeneratedRegistrations, addRegistration);
        ApplyEach(builder.Stages, addRegistration);
        ApplyEach(builder.StageFactories, addFactory);
        ApplyEach(builder.Behaviors, addRegistration);
        ApplyEach(builder.MatchedRegistrations, addRegistration);
        ApplyEach(builder.Defaults, tryAddDefault);
    }

    /// <summary>Applies <paramref name="apply"/> to every item in <paramref name="items"/>, in order.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to apply <paramref name="apply"/> to.</param>
    /// <param name="apply">The action to apply to each item.</param>
    private static void ApplyEach<T>(IEnumerable<T> items, Action<T> apply)
    {
        foreach (var item in items)
        {
            apply(item);
        }
    }
}
