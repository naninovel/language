using System.Text;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class HoverHandler (IMetadata meta, IDocumentRegistry docs) : IHoverHandler
{
    private readonly StringBuilder builder = new();
    private readonly FunctionResolver fnResolver = new(meta);

    private Position position;
    private DocumentLine line;

    public Hover? Hover (string documentUri, Position position)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        ResetState(position);
        line = docs.Get(documentUri)[position.Line];
        return line.Script switch {
            GenericLine line => HoverGenericLine(line),
            CommandLine line => HoverCommand(line.Command),
            _ => null
        };
    }

    private void ResetState (in Position position)
    {
        this.position = position;
        builder.Clear();
    }

    private bool IsCursorOver (ILineComponent content) => line.IsCursorOver(content, position);

    private Hover? HoverGenericLine (GenericLine line)
    {
        foreach (var content in line.Content)
            if (content is InlinedCommand inlined && IsCursorOver(inlined))
                return HoverCommand(inlined.Command);
            else if (content is MixedValue mixed)
                foreach (var cmp in mixed)
                    if (cmp is Parsing.Expression exp && IsCursorOver(exp))
                        return HoverExpression(exp.Body);
        return null;
    }

    private Hover? HoverCommand (Parsing.Command command)
    {
        var cmdMeta = meta.FindCommand(command.Identifier);
        if (cmdMeta is null) return null;
        if (IsCursorOver(command.Identifier))
            return HoverCommandIdentifier(command, cmdMeta);
        foreach (var parameter in command.Parameters)
            if (IsCursorOver(parameter))
                return HoverParameter(cmdMeta, parameter);
        return null;
    }

    private Hover HoverCommandIdentifier (Parsing.Command command, Metadata.Command commandMeta)
    {
        AppendCommand(commandMeta);
        var range = line.GetRange(command.Identifier, position.Line);
        return new Hover(builder.ToString(), range);
    }

    private Hover? HoverParameter (Metadata.Command commandMeta, Parsing.Parameter param)
    {
        var paramMeta = meta.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null) return null;

        var isExpCtx = paramMeta.ValueContext is [{ Type: ValueContextType.Expression }];
        foreach (var cmp in param.Value)
            if (!IsCursorOver(cmp)) continue;
            else if (cmp is Parsing.Expression exp)
                return HoverExpression(exp.Body);
            else if (isExpCtx && cmp is PlainText text)
                return HoverExpression(text);

        if (string.IsNullOrEmpty(paramMeta.Documentation?.Summary)) return null;
        var range = line.GetRange(param, position.Line);
        return new Hover(paramMeta.Documentation.Summary, range);
    }

    private Hover? HoverExpression (PlainText expBody)
    {
        var fns = fnResolver.Resolve(expBody, line);
        if (fns.FirstOrNull(fn => line.IsCursorOver(fn.Range, position)) is not { } fn) return null;
        if (string.IsNullOrEmpty(fn.Meta?.Documentation?.Summary)) return null;
        AppendFunction(fn.Meta);
        return new Hover(builder.ToString(), line.GetRange(fn.Range, position.Line));
    }

    private void AppendCommand (Metadata.Command cmd)
    {
        if (!string.IsNullOrEmpty(cmd.Documentation?.Summary))
            builder.Append($"## Summary\n{cmd.Documentation.Summary}\n");
        if (!string.IsNullOrEmpty(cmd.Documentation?.Remarks))
            builder.Append($"## Remarks\n{cmd.Documentation.Remarks}\n");
        if (cmd.Parameters.Length > 0)
            AppendParameters(cmd.Parameters);
        if (!string.IsNullOrEmpty(cmd.Documentation?.Examples))
            builder.Append($"## Examples\n```naniscript\n{cmd.Documentation.Examples}\n```");
    }

    private void AppendParameters (Metadata.Parameter[] parameters)
    {
        builder.Append("## Parameters\nName | Type | Summary\n:--- | :--- | :---\n");
        foreach (var param in parameters)
            AppendParameter(param);
    }

    private void AppendParameter (Metadata.Parameter param)
    {
        if (param.Nameless) builder.Append('~');
        if (param.Required) builder.Append("**");
        builder.Append(param.Label);
        if (param.Required) builder.Append("**");
        if (param.Nameless) builder.Append('~');
        builder.Append(" | ");
        builder.Append(param.TypeLabel);
        builder.Append(" | ");
        if (!string.IsNullOrEmpty(param.Documentation?.Summary))
            builder.Append(param.Documentation.Summary);
        builder.Append('\n');
    }

    private void AppendFunction (Function fn)
    {
        if (!string.IsNullOrEmpty(fn.Documentation?.Summary))
            builder.Append($"## Summary\n{fn.Documentation.Summary}\n");
        if (!string.IsNullOrEmpty(fn.Documentation?.Remarks))
            builder.Append($"## Remarks\n{fn.Documentation.Remarks}\n");
        if (fn.Parameters.Length > 0)
            AppendFunctionsParameters(fn.Parameters);
        if (!string.IsNullOrEmpty(fn.Documentation?.Examples))
            builder.Append($"## Examples\n```naniscript\n{fn.Documentation.Examples}\n```");
    }

    private void AppendFunctionsParameters (FunctionParameter[] parameters)
    {
        builder.Append("## Parameters\nName | Type\n:--- | :---\n");
        foreach (var param in parameters)
            AppendFunctionParameter(param);
    }

    private void AppendFunctionParameter (FunctionParameter param)
    {
        builder.Append(param.Name);
        builder.Append(" | ");
        builder.Append(param.Type.ToString().FirstToLower());
        builder.Append(" | ");
        builder.Append('\n');
    }
}
