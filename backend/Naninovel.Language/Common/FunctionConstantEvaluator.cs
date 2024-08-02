using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class FunctionConstantEvaluator (ISyntax stx)
{
    private readonly NamedValueParser namedParser = new(stx);
    private ResolvedFunction fn;

    public IReadOnlyList<string> EvaluateNames (string scriptId, ValueContext ctx, ResolvedFunction fn)
    {
        this.fn = fn;
        return ConstantEvaluator.EvaluateNames(ctx.SubType ?? "", scriptId, GetParamValue);
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
