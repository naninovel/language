namespace Naninovel.Language;

public readonly record struct Settings
{
    public string ScriptRootUri { get; init; }
    public bool DiagnoseSyntax { get; init; }
    public bool DiagnoseSemantics { get; init; }
    public bool DiagnoseNavigation { get; init; }
    public int DebounceDelay { get; init; }
}
