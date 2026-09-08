namespace Interlocutor.Abstractions.Pipeline.Diagnostics;

/// <summary>
/// Configures <see cref="IPipelineExplainer"/> - opt-in and disabled by default, since resolving live
/// pipeline-behavior instances from the container reveals internal wiring that shouldn't run unexamined
/// against a production container.
/// </summary>
public sealed class PipelineExplainerOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="IPipelineExplainer.Explain"/> is permitted to run.
    /// Defaults to <see langword="false"/> - calling <see cref="IPipelineExplainer.Explain"/> while this is
    /// <see langword="false"/> throws <see cref="InvalidOperationException"/> naming this property, rather
    /// than silently no-op-ing or granting an unexamined diagnostic path in production by default.
    /// </summary>
    /// <value><see langword="true"/> to permit <see cref="IPipelineExplainer.Explain"/> to run; otherwise, <see langword="false"/>.</value>
    public bool Enabled { get; set; }
}
