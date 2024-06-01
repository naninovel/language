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
        _ = parser.TryParse(line.Extract(expressionBody), out _);
        foreach (var range in ranges)
            if (TryFunction(range, out var fn))
                this.resolved.Add(fn);
        return (resolved = this.resolved.ToArray()).Length > 0;
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
        resolved = new ResolvedFunction(fnMeta, fnRange, ResolveParameters(fn, fnMeta));
        return true;
    }

    private ResolvedFunctionParameter[] ResolveParameters (Expression.Function fn, Metadata.Function fnMeta)
    {
        if (fnMeta.Parameters is []) return [];
        var resolved = new ResolvedFunctionParameter[fnMeta.Parameters.Length];
        for (var i = 0; i < fnMeta.Parameters.Length; i++)
            resolved[i] = ResolveParameter(fn.Parameters.ElementAtOrDefault(i), fnMeta.Parameters.ElementAtOrDefault(i));
        return resolved;
    }

    private ResolvedFunctionParameter ResolveParameter (IExpression? exp, FunctionParameter? paramMeta)
    {
        var expRange = ranges.FirstOrDefault(r => r.Expression == exp);
        if (expRange.Length == 0) return new(null, default, paramMeta);
        line.TryResolve(body, expRange, out var range);
        var content = line.Extract(range);
        var value = content.Length >= 2 ? content.Substring(1, content.Length - 2) : content;
        return new(value, range, paramMeta);
    }
}
