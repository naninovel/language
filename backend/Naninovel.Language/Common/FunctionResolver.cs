using System.Diagnostics.CodeAnalysis;
using Naninovel.Expression;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class FunctionResolver
{
    private readonly IMetadata meta;
    private readonly List<ExpressionRange> ranges = [];
    private readonly Parser parser;

    public FunctionResolver (IMetadata meta)
    {
        this.meta = meta;
        parser = new(new() { Syntax = meta.Syntax, HandleRange = ranges.Add });
    }

    public bool TryResolve (
        DocumentLine line,
        Position position,
        PlainText expressionBody,
        [NotNullWhen(true)] out Metadata.Function? functionMeta,
        out InlineRange functionRange,
        out Span<(string Value, InlineRange Range, FunctionParameter Meta)> parameters
    )
    {
        ranges.Clear();
        functionMeta = default;
        functionRange = default;
        parameters = default;

        if (!parser.TryParse(line.Extract(expressionBody), out _))
            return false;

        foreach (var expRange in ranges)
        {
            if (expRange.Expression is not Expression.Function fn) continue;
            line.TryResolve(expressionBody, expRange, out functionRange);
            if (!line.IsCursorOver(functionRange, position)) continue;
            if ((functionMeta = meta.FindFunction(fn.Name)) is null) return false;
            // TODO: Resolve parameters.
            return true;
        }

        return false;
    }
}
