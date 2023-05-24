using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_completion

public interface ICompletionHandler
{
    IReadOnlyList<CompletionItem> Complete (string documentUri, Position position);
}
