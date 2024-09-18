namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspace_applyEdit

public interface IEditPublisher
{
    void PublishEdit (string label, WorkspaceEdit edit);
}
