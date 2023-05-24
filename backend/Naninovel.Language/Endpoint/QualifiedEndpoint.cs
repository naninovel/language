namespace Naninovel.Language;

/// <summary>
/// An <see cref="Naninovel.Metadata.Endpoint"/> with script name always specified;
/// in cases when endpoint doesn't have script name (eg, @goto .label), the name equals containing script name.
/// When label is null, endpoint is considered pointing to the first line of the script.
/// </summary>
public readonly record struct QualifiedEndpoint(string ScriptName, string? Label = QualifiedEndpoint.NoLabel)
{
    public const string? NoLabel = null;
}
