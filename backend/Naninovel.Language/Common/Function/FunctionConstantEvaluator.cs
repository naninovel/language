using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class FunctionConstantEvaluator
{
    private readonly ExpressionEvaluator expEval;
    private readonly NamedValueParser namedParser;
    private ResolvedFunction fn;

    public FunctionConstantEvaluator (IMetadata meta, Func<string> getInspectedScript)
    {
        expEval = new(meta, getInspectedScript, GetParamValue);
        namedParser = new(meta.Syntax);
    }

    public IReadOnlyList<string> EvaluateNames (ValueContext ctx, ResolvedFunction fn)
    {
        this.fn = fn;
        var names = new List<string>();
        expEval.Evaluate(ctx.SubType ?? "", names);
        return names;
    }

    private string? GetParamValue (string name, int? idx)
    {
        var value = fn.Parameters.FirstOrNull(p =>
            string.Equals(p.Meta?.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
        if (!idx.HasValue) return value;
        var parsed = namedParser.Parse(value ?? "");
        return idx == 0 ? parsed.Name : parsed.Value;
    }
}
