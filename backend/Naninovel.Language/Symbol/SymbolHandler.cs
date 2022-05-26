using System.Collections.Generic;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#textDocument_documentSymbol

public class SymbolHandler
{
    private readonly MetadataProvider meta;
    private readonly DocumentRegistry registry;
    private readonly List<Symbol> symbols = new();

    private int lineIndex;
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
            symbols.Add(CreateForLine(document.Lines[i].Script, i));
        return symbols.ToArray();
    }

    private Symbol CreateForLine (IScriptLine scriptLine, int lineIndex)
    {
        this.lineIndex = lineIndex;
        return scriptLine switch {
            LabelLine label => CreateForLabelLine(label),
            CommentLine comment => CreateForCommentLine(comment),
            CommandLine command => CreateForCommandLine(command),
            GenericTextLine generic => CreateForGenericLine(generic),
            _ => CreateForEmptyLine()
        };
    }

    private Symbol CreateForLabelLine (LabelLine line) => new() {
        Name = nameof(LabelLine),
        Kind = SymbolKind.Namespace,
        Range = Range.FromContent(line, lineIndex),
        SelectionRange = Range.FromContent(line, lineIndex),
        Children = new[] { CreateForLabelText(line.LabelText) }
    };

    private Symbol CreateForCommentLine (CommentLine line) => new() {
        Name = nameof(CommentLine),
        Kind = SymbolKind.String,
        Range = Range.FromContent(line, lineIndex),
        SelectionRange = Range.FromContent(line, lineIndex),
        Children = new[] { CreateForCommentText(line.CommentText) }
    };

    private Symbol CreateForCommandLine (CommandLine line) => new() {
        Name = nameof(CommandLine),
        Kind = SymbolKind.Struct,
        Range = Range.FromContent(line, lineIndex),
        SelectionRange = Range.FromContent(line, lineIndex),
        Children = new[] { CreateForCommand(line.Command) }
    };

    private Symbol CreateForGenericLine (GenericTextLine line) => new() {
        Name = nameof(GenericTextLine),
        Kind = SymbolKind.String,
        Range = Range.FromContent(line, lineIndex),
        SelectionRange = Range.FromContent(line, lineIndex),
        Children = CreateGenericChildren(line)
    };

    private Symbol CreateForEmptyLine () => new() {
        Name = nameof(EmptyLine),
        Kind = SymbolKind.Null,
        Range = new Range(new(lineIndex, 0), new(lineIndex, 0)),
        SelectionRange = new Range(new(lineIndex, 0), new(lineIndex, 0))
    };

    private Symbol CreateForLabelText (LineText text) => new() {
        Name = "LabelText",
        Kind = SymbolKind.String,
        Range = Range.FromContent(text, lineIndex),
        SelectionRange = Range.FromContent(text, lineIndex)
    };

    private Symbol CreateForCommentText (LineText text) => new() {
        Name = "CommentText",
        Kind = SymbolKind.String,
        Range = Range.FromContent(text, lineIndex),
        SelectionRange = Range.FromContent(text, lineIndex)
    };

    private Symbol CreateForCommand (Parsing.Command command) => new() {
        Name = nameof(Parsing.Command),
        Kind = SymbolKind.Function,
        Range = Range.FromContent(command, lineIndex),
        SelectionRange = Range.FromContent(command, lineIndex),
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

    private Symbol[] CreateGenericChildren (GenericTextLine line)
    {
        var symbols = new List<Symbol>();
        if (!line.Prefix.Empty)
            symbols.Add(CreateForGenericPrefix(line.Prefix));
        foreach (var content in line.Content)
            if (content is InlinedCommand inlined) symbols.Add(CreateForInlined(inlined));
            else symbols.Add(CreateForGenericText((GenericText)content));
        return symbols.ToArray();
    }

    private Symbol CreateForGenericPrefix (GenericTextPrefix prefix)
    {
        var children = new List<Symbol> { CreateForGenericAuthor(prefix.AuthorIdentifier) };
        if (!prefix.AuthorAppearance.Empty)
            children.Add(CreateForGenericAppearance(prefix.AuthorAppearance));
        return new Symbol {
            Name = nameof(GenericTextPrefix),
            Kind = SymbolKind.Constant,
            Range = Range.FromContent(prefix, lineIndex),
            SelectionRange = Range.FromContent(prefix, lineIndex),
            Children = children.ToArray()
        };
    }

    private Symbol CreateForGenericAuthor (LineText author) => new() {
        Name = "GenericTextAuthor",
        Kind = SymbolKind.Key,
        Range = Range.FromContent(author, lineIndex),
        SelectionRange = Range.FromContent(author, lineIndex)
    };

    private Symbol CreateForGenericAppearance (LineText appearance) => new() {
        Name = "GenericTextAuthorAppearance",
        Kind = SymbolKind.Enum,
        Range = Range.FromContent(appearance, lineIndex),
        SelectionRange = Range.FromContent(appearance, lineIndex)
    };

    private Symbol CreateForInlined (InlinedCommand inlined) => new() {
        Name = nameof(InlinedCommand),
        Kind = SymbolKind.Struct,
        Range = Range.FromContent(inlined, lineIndex),
        SelectionRange = Range.FromContent(inlined, lineIndex),
        Children = new[] { CreateForCommand(inlined.Command) }
    };

    private Symbol CreateForGenericText (GenericText text) => new() {
        Name = nameof(GenericText),
        Kind = SymbolKind.String,
        Range = Range.FromContent(text, lineIndex),
        SelectionRange = Range.FromContent(text, lineIndex),
        Children = text.Expressions.Select(CreateForExpression).ToArray()
    };

    private Symbol CreateForCommandIdentifier (LineText identifier) => new() {
        Name = "CommandIdentifier",
        Kind = SymbolKind.Key,
        Range = Range.FromContent(identifier, lineIndex),
        SelectionRange = Range.FromContent(identifier, lineIndex)
    };

    private Symbol CreateForCommandParameter (Parsing.Parameter parameter) => new() {
        Name = nameof(Parsing.Parameter),
        Kind = SymbolKind.Field,
        Range = Range.FromContent(parameter, lineIndex),
        SelectionRange = Range.FromContent(parameter, lineIndex),
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

    private Symbol CreateForParameterIdentifier (LineText identifier) => new() {
        Name = "ParameterIdentifier",
        Kind = SymbolKind.Key,
        Range = Range.FromContent(identifier, lineIndex),
        SelectionRange = Range.FromContent(identifier, lineIndex)
    };

    private Symbol CreateForParameterValue (Parsing.Parameter parameter) => new() {
        Name = nameof(ParameterValue),
        Kind = GetParameterValueKind(parameter),
        Range = Range.FromContent(parameter.Value, lineIndex),
        SelectionRange = Range.FromContent(parameter.Value, lineIndex),
        Children = parameter.Value.Expressions.Select(CreateForExpression).ToArray()
    };

    private Symbol CreateForExpression (LineText expression) => new() {
        Name = "Expression",
        Kind = SymbolKind.Property,
        Range = Range.FromContent(expression, lineIndex),
        SelectionRange = Range.FromContent(expression, lineIndex)
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
