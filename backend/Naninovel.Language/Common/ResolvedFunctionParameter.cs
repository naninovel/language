using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

/// <param name="Meta">Null when function meta doesn't include this parameter.</param>
internal readonly record struct ResolvedFunctionParameter
(
    string Value,
    InlineRange Range,
    FunctionParameter? Meta,
    bool IsOperand
);
