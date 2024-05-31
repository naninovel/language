using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal record ResolvedFunctionParameter
(
    /// <summary>
    /// Null when argument is not a value, but an expression.
    /// </summary>
    string? Value,
    InlineRange Range,
    FunctionParameter Meta
);
