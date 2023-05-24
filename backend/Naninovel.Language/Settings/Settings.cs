namespace Naninovel.Language;

public record Settings
{
    public bool DiagnoseSyntax { get; init; }
    public bool DiagnoseSemantics { get; init; }
    public bool DiagnoseNavigation { get; init; }
}
