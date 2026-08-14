namespace Interlocutor.Abstractions.Core;

/// <summary>Combined send-and-publish interface. Exposes both <see cref="ISender"/> and <see cref="IPublisher"/>.</summary>
/// <remarks>
/// <para>
/// This is the main abstraction for CQRS dispatch. Inject <see cref="IMediator"/> in your application to send queries,
/// dispatch commands, and publish notifications all from one interface. If you only need queries/commands or only
/// notifications, inject <see cref="ISender"/> or <see cref="IPublisher"/> respectively for better API clarity.
/// </para>
/// </remarks>
public interface IMediator : ISender, IPublisher;
