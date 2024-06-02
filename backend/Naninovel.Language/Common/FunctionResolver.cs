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
    private readonly List<Metadata.Function> metas = [];
    private DocumentLine line;
    private PlainText body = null!;

    public FunctionResolver (IMetadata meta)
    {
        this.meta = meta;
        parser = new(new() { Syntax = meta.Syntax, HandleRange = ranges.Add });
    }

    public ResolvedFunction[] Resolve (PlainText expressionBody, DocumentLine line)
    {
        ResetState(expressionBody, line);
        _ = parser.TryParse(line.Extract(expressionBody), out _);
        foreach (var range in ranges)
            if (TryFunction(range, out var fn))
                resolved.Add(fn);
        return resolved.ToArray();
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
        line.TryResolve(body, expRange, out var fnRange);
        var fnMeta = ResolveFunctionMeta(fn);
        var @params = fnMeta is null || fnMeta.Parameters.Length == 0 ? [] : ResolveParameters(fn, fnMeta);
        resolved = new ResolvedFunction(fnMeta, fnRange, @params);
        return true;
    }

    private Metadata.Function? ResolveFunctionMeta (Expression.Function fn)
    {
        if (!meta.FindFunctions(fn.Name, metas)) return null;
        if (metas.Count == 1) return metas[0];
        var values = fn.Parameters.Select(ExtractValue).ToArray();
        var maxCompatibleParams = 0;
        var maxCompatibleMeta = metas[0];
        foreach (var meta in metas)
        {
            if (meta.Parameters.Length == 0 && values.Length == 0) return meta;
            var compatibleParams = 0;
            for (var i = 0; i < values.Length; i++)
                if (IsCompatible(values[i], meta.Parameters.ElementAtOrDefault(i)?.Type))
                    compatibleParams++;
            if (compatibleParams > maxCompatibleParams)
            {
                maxCompatibleParams = compatibleParams;
                maxCompatibleMeta = meta;
            }
        }
        return maxCompatibleMeta;

        string ExtractValue (IExpression e)
        {
            var expRange = ranges.FirstOrDefault(r => r.Expression == e);
            line.TryResolve(body, expRange, out var range);
            return line.Extract(range);
        }

        bool IsCompatible (string value, Metadata.ValueType? type)
        {
            if (string.IsNullOrEmpty(value) || !type.HasValue) return false;
            if (type.Value == Metadata.ValueType.String) return value.StartsWith('"');
            if (type.Value == Metadata.ValueType.Boolean)
                return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("false", StringComparison.OrdinalIgnoreCase);
            if (type.Value == Metadata.ValueType.Integer) return int.TryParse(value, out _);
            return double.TryParse(value, out _);
        }
    }

    private ResolvedFunctionParameter[] ResolveParameters (Expression.Function fn, Metadata.Function fnMeta)
    {
        var paramCount = Math.Max(fn.Parameters.Count, fnMeta.Parameters.Length);
        var resolved = new ResolvedFunctionParameter[paramCount];
        for (var i = 0; i < paramCount; i++)
            resolved[i] = ResolveParameter(fn.Parameters.ElementAtOrDefault(i), fnMeta.Parameters.ElementAtOrDefault(i));
        return resolved;
    }

    private ResolvedFunctionParameter ResolveParameter (IExpression? exp, FunctionParameter? paramMeta)
    {
        var expRange = ranges.FirstOrDefault(r => r.Expression == exp);
        line.TryResolve(body, expRange, out var range);
        var value = line.Extract(range);
        if (exp is Expression.String && value.Length >= 2)
            value = value.Substring(1, value.Length - 2);
        else if (exp != null && exp is not IOperand) value = "imlazy";
        return new(value, range, paramMeta, exp is IOperand);
    }
}
