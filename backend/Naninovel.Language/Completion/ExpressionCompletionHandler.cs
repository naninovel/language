using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class ExpressionCompletionHandler (IMetadata meta, IEndpointRegistry endpoints, CompletionProvider completions)
{
    private readonly IMetadata meta = meta;
    private readonly IEndpointRegistry endpoints = endpoints;
    private readonly FunctionResolver fnResolver = new(meta);

    public CompletionItem[] Handle (PlainText? expBody, in Position pos,
        in DocumentLine line, string scriptName)
    {
        if (GetFunctionOverCursor(expBody, pos, line) is not { } fn)
            return completions.GetExpressions();

        return completions.GetExpressions();
    }

    private ResolvedFunction? GetFunctionOverCursor (PlainText? body, in Position pos, in DocumentLine line)
    {
        if (body is null) return null;
        if (!fnResolver.TryResolve(body, line, out var fns)) return null;
        foreach (var fn in fns)
            if (line.IsCursorOver(fn.Range, pos))
                return fn;
        return null;
    }
}
