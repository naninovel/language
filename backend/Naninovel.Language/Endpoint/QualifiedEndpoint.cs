namespace Naninovel.Language;

/// <summary>
/// An <see cref="Naninovel.Metadata.Endpoint"/> with script path always specified;
/// in cases when endpoint doesn't have script path (eg, @goto .label), the path equals containing script path.
/// When label is null, endpoint is considered pointing to the first line of the script.
/// </summary>
public readonly record struct QualifiedEndpoint (string ScriptPath, string? Label = QualifiedEndpoint.NoLabel)
{
    public const string? NoLabel = null;
}
