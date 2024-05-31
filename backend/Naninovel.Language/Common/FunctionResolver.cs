using System.Diagnostics.CodeAnalysis;
using Naninovel.Expression;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class FunctionResolver
{
    private readonly Parser parser;
    private readonly IMetadata meta;
    private readonly List<ExpressionRange> ranges = [];
    private readonly List<ResolvedFunction> resolved = [];
    private DocumentLine line;
    private PlainText body = null!;

    public FunctionResolver (IMetadata meta)
    {
        this.meta = meta;
        parser = new(new() { Syntax = meta.Syntax, HandleRange = ranges.Add });
    }

    public bool TryResolve (PlainText expressionBody, DocumentLine line, out ResolvedFunction[] resolved)
    {
        resolved = [];
        ResetState(expressionBody, line);
        if (!parser.TryParse(line.Extract(expressionBody), out _)) return false;
        foreach (var range in ranges)
            if (TryFunction(range, out var fn))
                this.resolved.Add(fn);
        if (this.resolved.Count == 0) return false;
        resolved = this.resolved.ToArray();
        return true;
    }

    private void ResetState (PlainText body, DocumentLine line)
    {
        ranges.Clear();
        resolved.Clear();
        this.line = line;
        this.body = body;
    }

    private bool TryFunction (ExpressionRange expRange, [NotNullWhen(true)] out ResolvedFunction? resolved)
    {
        resolved = default;
        if (expRange.Expression is not Expression.Function fn) return false;
        if (meta.FindFunction(fn.Name) is not { } fnMeta) return false;
        line.TryResolve(body, expRange, out var fnRange);
        resolved = new ResolvedFunction(fnMeta, fnRange, []);
        return true;
    }

    private bool TryParameter ([NotNullWhen(true)] out ResolvedFunctionParameter? param)
    {
        param = default;
        return false;
    }
}
