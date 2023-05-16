namespace Naninovel.Language;

public record Settings(
    bool DiagnoseSyntax,
    bool DiagnoseSemantics,
    bool DiagnoseNavigation
);
