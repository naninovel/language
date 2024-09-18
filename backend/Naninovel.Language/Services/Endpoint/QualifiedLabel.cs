namespace Naninovel.Language;

/// <summary>
/// Label part of <see cref="Naninovel.Metadata.Endpoint"/> with script path always specified;
/// in cases when endpoint doesn't have script path (eg, @goto .label), the path equals containing script path.
/// </summary>
public readonly record struct QualifiedLabel (string ScriptPath, string Label);
