using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokens

public record Tokens(IReadOnlyList<int> Data);
