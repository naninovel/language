using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class ExpressionCompletionHandler (IMetadata meta, IEndpointRegistry endpoints, CompletionProvider completions)
{
    private readonly IMetadata meta = meta;
    private readonly IEndpointRegistry endpoints = endpoints;
    private readonly FunctionResolver fnResolver = new(meta);

    public CompletionItem[] Handle (PlainText? expBody, in Position position,
        in DocumentLine line, string scriptName)
    {
        if (expBody is null || !fnResolver.TryResolve(line, position, expBody,
                out var fn, out var fnRange, out var parameters))
            return completions.GetExpressions();

        return completions.GetExpressions();
    }
}
