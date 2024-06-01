using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal record ResolvedFunctionParameter
(
    /// <summary>
    /// Null when argument is not a value, but an expression.
    /// </summary>
    string? Value,
    /// <summary>
    /// Null when argument is not a value, but an expression.
    /// </summary>
    InlineRange Range,
    /// <summary>
    /// Null when function meta doesn't include this parameter.
    /// </summary>
    FunctionParameter? Meta
);
