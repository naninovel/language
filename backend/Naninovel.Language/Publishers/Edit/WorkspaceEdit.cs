namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceEdit

/// <param name="ChangeAnnotations">Keys referenced by <see cref="TextEdit.AnnotationId"/>.</param>
public readonly record struct WorkspaceEdit (
    IReadOnlyList<DocumentEdit> DocumentChanges,
    IReadOnlyDictionary<string, EditAnnotation>? ChangeAnnotations = null
);
