namespace Mediance.Abstractions.Markers;

public interface IAuditableRequest
{
    string ActionName { get; }
}
