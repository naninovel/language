namespace Naninovel.Language;

public readonly record struct Settings
{
    public string ScenarioRoot { get; init; }
    public int DebounceDelay { get; init; }
    public bool DiagnoseSyntax { get; init; }
    public bool DiagnoseSemantics { get; init; }
    public bool DiagnoseNavigation { get; init; }
    public bool RefactorFileRenames { get; init; }
}
