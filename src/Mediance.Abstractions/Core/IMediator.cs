namespace Mediance.Abstractions.Core;

/// <summary>Combined send-and-publish interface. Exposes both <see cref="ISender"/> and <see cref="IPublisher"/>.</summary>
public interface IMediator : ISender, IPublisher;
