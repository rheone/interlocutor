namespace Interlocutor.IntegrationTests;

/// <summary>
/// The single entity persisted by this project's tracer-bullet command - deliberately minimal, since the
/// point of this project is proving the pipeline wiring across real package boundaries, not exercising rich
/// domain behavior.
/// </summary>
public sealed class Widget
{
    /// <summary>Gets or sets the primary key, assigned by the database on insert.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the widget's name, as supplied by <see cref="CreateWidgetCommand"/>.</summary>
    public string Name { get; set; } = string.Empty;
}
