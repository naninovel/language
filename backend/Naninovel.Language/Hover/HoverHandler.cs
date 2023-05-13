using System.Text;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-16/#textDocument_hover

public class HoverHandler
{
    private readonly MetadataProvider meta;
    private readonly IDocumentRegistry registry;
    private readonly StringBuilder builder = new();

    private Position position;
    private DocumentLine line;

    public HoverHandler (MetadataProvider meta, IDocumentRegistry registry)
    {
        this.meta = meta;
        this.registry = registry;
    }

    public Hover? Hover (string documentUri, Position position)
    {
        ResetState(position);
        line = registry.Get(documentUri)[position.Line];
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
        return null;
    }

    private Hover? HoverCommand (Parsing.Command command)
    {
        var commandMeta = meta.FindCommand(command.Identifier);
        if (commandMeta is null) return null;
        if (IsCursorOver(command.Identifier))
            return HoverCommandIdentifier(command, commandMeta);
        foreach (var parameter in command.Parameters)
            if (IsCursorOver(parameter))
                return HoverParameter(commandMeta, parameter);
        return null;
    }

    private Hover HoverCommandIdentifier (Parsing.Command command, Metadata.Command commandMeta)
    {
        if (!string.IsNullOrEmpty(commandMeta.Summary))
            builder.Append($"## Summary\n{commandMeta.Summary}\n");
        if (!string.IsNullOrEmpty(commandMeta.Remarks))
            builder.Append($"## Remarks\n{commandMeta.Remarks}\n");
        if (commandMeta.Parameters.Length > 0)
            AppendParameters(commandMeta.Parameters);
        if (!string.IsNullOrEmpty(commandMeta.Examples))
            builder.Append($"## Examples\n```nani\n{commandMeta.Examples}\n```");
        var range = line.GetRange(command.Identifier, position.Line);
        return new Hover(builder.ToString(), range);
    }

    private Hover? HoverParameter (Metadata.Command commandMeta, Parsing.Parameter param)
    {
        var paramMeta = meta.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null || string.IsNullOrEmpty(paramMeta.Summary)) return null;
        var range = line.GetRange(param, position.Line);
        return new Hover(paramMeta.Summary, range);
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
        if (!string.IsNullOrEmpty(param.Summary))
            builder.Append(param.Summary);
        builder.Append('\n');
    }
}
