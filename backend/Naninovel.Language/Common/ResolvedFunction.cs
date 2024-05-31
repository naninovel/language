using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal record ResolvedFunction
(
    Function Meta,
    InlineRange Range,
    IReadOnlyList<ResolvedFunctionParameter> Parameters
);
