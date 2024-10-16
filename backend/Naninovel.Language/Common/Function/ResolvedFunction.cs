using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

/// <param name="Meta">Null when function meta is not found.</param>
internal readonly record struct ResolvedFunction (
    Function? Meta,
    Expression.Function Function,
    InlineRange Range,
    IReadOnlyList<ResolvedFunctionParameter> Parameters
);
