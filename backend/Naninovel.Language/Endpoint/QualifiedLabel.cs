namespace Naninovel.Language;

/// <summary>
/// Label part of <see cref="Naninovel.Metadata.Endpoint"/> with script name always specified;
/// in cases when endpoint doesn't have script name (eg, @goto .label), the name equals containing script name.
/// </summary>
public readonly record struct QualifiedLabel (string ScriptName, string Label);
