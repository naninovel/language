using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class ExpressionCompletionHandler (IMetadata meta, CompletionProvider completions, IEndpointRegistry endpoints)
{
    public CompletionItem[] Handle (Parsing.PlainText? expressionBody, in Position position,
        in DocumentLine line, string scriptName)
    {
        if (expressionBody is null)
            return completions.GetExpressions();

        // TODO: ...
        return completions.GetExpressions();
    }
}
