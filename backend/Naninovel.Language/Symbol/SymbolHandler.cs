using System.Collections.Generic;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_documentSymbol

public class SymbolHandler
{
    private readonly MetadataProvider meta;
    private readonly DocumentRegistry registry;
    private readonly List<Symbol> symbols = new();

    private int lineIndex;
    private DocumentLine line = null!;
    private string commandId = "";

    public SymbolHandler (MetadataProvider meta, DocumentRegistry registry)
    {
        this.meta = meta;
        this.registry = registry;
    }

    public Symbol[] GetSymbols (string documentUri)
    {
        symbols.Clear();
        var document = registry.Get(documentUri);
        for (int i = 0; i < document.Lines.Count; i++)
            symbols.Add(CreateForLine(document.Lines[i], i));
        return symbols.ToArray();
    }

    private Symbol CreateForLine (DocumentLine documentLine, int lineIndex)
    {
        this.lineIndex = lineIndex;
        line = documentLine;
        return documentLine.Script switch {
            LabelLine label => CreateForLabelLine(label),
            CommentLine comment => CreateForCommentLine(comment),
            CommandLine command => CreateForCommandLine(command),
            _ => CreateForGenericLine((GenericLine)documentLine.Script)
        };
    }

    private Symbol CreateForLabelLine (LabelLine labelLine) => new() {
        Name = nameof(LabelLine),
        Kind = SymbolKind.Namespace,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = new[] { CreateForLabelText(labelLine.Label) }
    };

    private Symbol CreateForCommentLine (CommentLine commentLine) => new() {
        Name = nameof(CommentLine),
        Kind = SymbolKind.String,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = new[] { CreateForCommentText(commentLine.Comment) }
    };

    private Symbol CreateForCommandLine (CommandLine commandLine) => new() {
        Name = nameof(CommandLine),
        Kind = SymbolKind.Struct,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = new[] { CreateForCommand(commandLine.Command) }
    };

    private Symbol CreateForGenericLine (GenericLine genericLine) => new() {
        Name = "GenericTextLine",
        Kind = SymbolKind.String,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = CreateGenericChildren(genericLine)
    };

    private Symbol CreateForLabelText (PlainText text) => new() {
        Name = "LabelText",
        Kind = SymbolKind.String,
        Range = line.GetRange(text, lineIndex),
        SelectionRange = line.GetRange(text, lineIndex)
    };

    private Symbol CreateForCommentText (PlainText text) => new() {
        Name = "CommentText",
        Kind = SymbolKind.String,
        Range = line.GetRange(text, lineIndex),
        SelectionRange = line.GetRange(text, lineIndex)
    };

    private Symbol CreateForCommand (Parsing.Command command) => new() {
        Name = nameof(Parsing.Command),
        Kind = SymbolKind.Function,
        Range = line.GetRange(command, lineIndex),
        SelectionRange = line.GetRange(command, lineIndex),
        Children = CreateCommandChildren(command)
    };

    private Symbol[] CreateCommandChildren (Parsing.Command command)
    {
        commandId = command.Identifier;
        var symbols = new List<Symbol>();
        symbols.Add(CreateForCommandIdentifier(command.Identifier));
        foreach (var parameter in command.Parameters)
            symbols.Add(CreateForCommandParameter(parameter));
        return symbols.ToArray();
    }

    private Symbol[] CreateGenericChildren (GenericLine genericLine)
    {
        var symbols = new List<Symbol>();
        if (genericLine.Prefix is not null)
            symbols.Add(CreateForGenericPrefix(genericLine.Prefix));
        foreach (var content in genericLine.Content)
            if (content is InlinedCommand inlined) symbols.Add(CreateForInlined(inlined));
            else symbols.Add(CreateForGenericText((MixedValue)content));
        return symbols.ToArray();
    }

    private Symbol CreateForGenericPrefix (GenericPrefix prefix)
    {
        var children = new List<Symbol> { CreateForGenericAuthor(prefix.Author) };
        if (prefix.Appearance is not null)
            children.Add(CreateForGenericAppearance(prefix.Appearance));
        return new Symbol {
            Name = "GenericTextPrefix",
            Kind = SymbolKind.Constant,
            Range = line.GetRange(prefix, lineIndex),
            SelectionRange = line.GetRange(prefix, lineIndex),
            Children = children.ToArray()
        };
    }

    private Symbol CreateForGenericAuthor (PlainText author) => new() {
        Name = "GenericTextAuthor",
        Kind = SymbolKind.Key,
        Range = line.GetRange(author, lineIndex),
        SelectionRange = line.GetRange(author, lineIndex)
    };

    private Symbol CreateForGenericAppearance (PlainText appearance) => new() {
        Name = "GenericTextAuthorAppearance",
        Kind = SymbolKind.Enum,
        Range = line.GetRange(appearance, lineIndex),
        SelectionRange = line.GetRange(appearance, lineIndex)
    };

    private Symbol CreateForInlined (InlinedCommand inlined) => new() {
        Name = nameof(InlinedCommand),
        Kind = SymbolKind.Struct,
        Range = line.GetRange(inlined, lineIndex),
        SelectionRange = line.GetRange(inlined, lineIndex),
        Children = new[] { CreateForCommand(inlined.Command) }
    };

    private Symbol CreateForGenericText (MixedValue text) => new() {
        Name = "GenericText",
        Kind = SymbolKind.String,
        Range = line.GetRange(text, lineIndex),
        SelectionRange = line.GetRange(text, lineIndex),
        Children = text.OfType<Expression>().Select(CreateForExpression).ToArray()
    };

    private Symbol CreateForCommandIdentifier (PlainText identifier) => new() {
        Name = "CommandIdentifier",
        Kind = SymbolKind.Key,
        Range = line.GetRange(identifier, lineIndex),
        SelectionRange = line.GetRange(identifier, lineIndex)
    };

    private Symbol CreateForCommandParameter (Parsing.Parameter parameter) => new() {
        Name = nameof(Parsing.Parameter),
        Kind = SymbolKind.Field,
        Range = line.GetRange(parameter, lineIndex),
        SelectionRange = line.GetRange(parameter, lineIndex),
        Children = CreateParameterChildren(parameter)
    };

    private Symbol[] CreateParameterChildren (Parsing.Parameter parameter)
    {
        var symbols = new List<Symbol>();
        if (!parameter.Nameless)
            symbols.Add(CreateForParameterIdentifier(parameter.Identifier));
        symbols.Add(CreateForParameterValue(parameter));
        return symbols.ToArray();
    }

    private Symbol CreateForParameterIdentifier (PlainText? identifier) => new() {
        Name = "ParameterIdentifier",
        Kind = SymbolKind.Key,
        Range = line.GetRange(identifier, lineIndex),
        SelectionRange = line.GetRange(identifier, lineIndex)
    };

    private Symbol CreateForParameterValue (Parsing.Parameter parameter) => new() {
        Name = "ParameterValue",
        Kind = GetParameterValueKind(parameter),
        Range = line.GetRange(parameter.Value, lineIndex),
        SelectionRange = line.GetRange(parameter.Value, lineIndex),
        Children = parameter.Value.OfType<Expression>().Select(CreateForExpression).ToArray()
    };

    private Symbol CreateForExpression (Expression expression) => new() {
        Name = "Expression",
        Kind = SymbolKind.Property,
        Range = line.GetRange(expression, lineIndex),
        SelectionRange = line.GetRange(expression, lineIndex)
    };

    private SymbolKind GetParameterValueKind (Parsing.Parameter parameter)
    {
        var paramMeta = meta.FindParameter(commandId, parameter.Identifier);
        if (paramMeta is null || parameter.Value.Dynamic)
            return SymbolKind.String;
        if (paramMeta.ValueContainerType is ValueContainerType.List or ValueContainerType.NamedList)
            return SymbolKind.Array;
        return paramMeta.ValueType switch {
            ValueType.Integer => SymbolKind.Number,
            ValueType.Decimal => SymbolKind.Number,
            ValueType.Boolean => SymbolKind.Boolean,
            ValueType.String or _ => SymbolKind.String
        };
    }
}
