using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

/// <param name="Meta">Null when function meta is not found.</param>
internal record ResolvedFunction
(
    Function? Meta,
    InlineRange Range,
    IReadOnlyList<ResolvedFunctionParameter> Parameters
);
