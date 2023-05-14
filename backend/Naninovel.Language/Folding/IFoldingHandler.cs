using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_foldingRange

public interface IFoldingHandler
{
    IReadOnlyList<FoldingRange> GetFoldingRanges (string documentUri);
}
