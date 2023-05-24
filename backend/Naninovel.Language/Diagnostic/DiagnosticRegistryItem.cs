namespace Naninovel.Language;

internal readonly record struct DiagnosticRegistryItem(
    int Line,
    DiagnosticContext Context,
    Diagnostic Diagnostic
);
