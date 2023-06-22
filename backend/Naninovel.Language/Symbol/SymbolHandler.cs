using System.Collections.Generic;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class SymbolHandler : ISymbolHandler, IMetadataObserver
{
    private readonly MetadataProvider metaProvider = new();
    private readonly IDocumentRegistry registry;
    private readonly List<Symbol> symbols = new();

    private int lineIndex;
    private DocumentLine line;
    private string commandId = "";

    public SymbolHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public void HandleMetadataChanged (Project meta) => metaProvider.Update(meta);

    public IReadOnlyList<Symbol> GetSymbols (string documentUri)
    {
        symbols.Clear();
        var document = registry.Get(documentUri);
        for (int i = 0; i < document.LineCount; i++)
            symbols.Add(CreateForLine(document[i], i));
        return symbols.ToArray();
    }

    private Symbol CreateForLine (in DocumentLine documentLine, int lineIndex)
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
        Kind = (int)SymbolKind.Namespace,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = new[] { CreateForLabelText(labelLine.Label) }
    };

    private Symbol CreateForCommentLine (CommentLine commentLine) => new() {
        Name = nameof(CommentLine),
        Kind = (int)SymbolKind.String,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = new[] { CreateForCommentText(commentLine.Comment) }
    };

    private Symbol CreateForCommandLine (CommandLine commandLine) => new() {
        Name = nameof(CommandLine),
        Kind = (int)SymbolKind.Struct,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = new[] { CreateForCommand(commandLine.Command) }
    };

    private Symbol CreateForGenericLine (GenericLine genericLine) => new() {
        Name = "GenericTextLine",
        Kind = (int)SymbolKind.String,
        Range = line.GetRange(lineIndex),
        SelectionRange = line.GetRange(lineIndex),
        Children = CreateGenericChildren(genericLine)
    };

    private Symbol CreateForLabelText (PlainText text) => new() {
        Name = "LabelText",
        Kind = (int)SymbolKind.String,
        Range = line.GetRange(text, lineIndex),
        SelectionRange = line.GetRange(text, lineIndex)
    };

    private Symbol CreateForCommentText (PlainText text) => new() {
        Name = "CommentText",
        Kind = (int)SymbolKind.String,
        Range = line.GetRange(text, lineIndex),
        SelectionRange = line.GetRange(text, lineIndex)
    };

    private Symbol CreateForCommand (Parsing.Command command) => new() {
        Name = nameof(Parsing.Command),
        Kind = (int)SymbolKind.Function,
        Range = line.GetRange(command, lineIndex),
        SelectionRange = line.GetRange(command, lineIndex),
        Children = CreateCommandChildren(command)
    };

    private IReadOnlyList<Symbol> CreateCommandChildren (Parsing.Command command)
    {
        commandId = command.Identifier;
        var symbols = new List<Symbol>();
        symbols.Add(CreateForCommandIdentifier(command.Identifier));
        foreach (var parameter in command.Parameters)
            symbols.Add(CreateForCommandParameter(parameter));
        return symbols;
    }

    private IReadOnlyList<Symbol> CreateGenericChildren (GenericLine genericLine)
    {
        var symbols = new List<Symbol>();
        if (genericLine.Prefix is not null)
            symbols.Add(CreateForGenericPrefix(genericLine.Prefix));
        foreach (var content in genericLine.Content)
            if (content is InlinedCommand inlined) symbols.Add(CreateForInlined(inlined));
            else symbols.Add(CreateForGenericText((MixedValue)content));
        return symbols;
    }

    private Symbol CreateForGenericPrefix (GenericPrefix prefix)
    {
        var children = new List<Symbol> { CreateForGenericAuthor(prefix.Author) };
        if (prefix.Appearance is not null)
            children.Add(CreateForGenericAppearance(prefix.Appearance));
        return new Symbol {
            Name = "GenericTextPrefix",
            Kind = (int)SymbolKind.Constant,
            Range = line.GetRange(prefix, lineIndex),
            SelectionRange = line.GetRange(prefix, lineIndex),
            Children = children
        };
    }

    private Symbol CreateForGenericAuthor (PlainText author) => new() {
        Name = "GenericTextAuthor",
        Kind = (int)SymbolKind.Key,
        Range = line.GetRange(author, lineIndex),
        SelectionRange = line.GetRange(author, lineIndex)
    };

    private Symbol CreateForGenericAppearance (PlainText appearance) => new() {
        Name = "GenericTextAuthorAppearance",
        Kind = (int)SymbolKind.Enum,
        Range = line.GetRange(appearance, lineIndex),
        SelectionRange = line.GetRange(appearance, lineIndex)
    };

    private Symbol CreateForInlined (InlinedCommand inlined) => new() {
        Name = nameof(InlinedCommand),
        Kind = (int)SymbolKind.Struct,
        Range = line.GetRange(inlined, lineIndex),
        SelectionRange = line.GetRange(inlined, lineIndex),
        Children = new[] { CreateForCommand(inlined.Command) }
    };

    private Symbol CreateForGenericText (MixedValue text) => new() {
        Name = "GenericText",
        Kind = (int)SymbolKind.String,
        Range = line.GetRange(text, lineIndex),
        SelectionRange = line.GetRange(text, lineIndex),
        Children = CreateForMixed(text)
    };

    private Symbol CreateForCommandIdentifier (PlainText identifier) => new() {
        Name = "CommandIdentifier",
        Kind = (int)SymbolKind.Key,
        Range = line.GetRange(identifier, lineIndex),
        SelectionRange = line.GetRange(identifier, lineIndex)
    };

    private Symbol CreateForCommandParameter (Parsing.Parameter parameter) => new() {
        Name = nameof(Parsing.Parameter),
        Kind = (int)SymbolKind.Field,
        Range = line.GetRange(parameter, lineIndex),
        SelectionRange = line.GetRange(parameter, lineIndex),
        Children = CreateParameterChildren(parameter)
    };

    private IReadOnlyList<Symbol> CreateParameterChildren (Parsing.Parameter parameter)
    {
        var symbols = new List<Symbol>();
        if (!parameter.Nameless)
            symbols.Add(CreateForParameterIdentifier(parameter.Identifier));
        symbols.Add(CreateForParameterValue(parameter));
        return symbols;
    }

    private Symbol CreateForParameterIdentifier (PlainText? identifier) => new() {
        Name = "ParameterIdentifier",
        Kind = (int)SymbolKind.Key,
        Range = line.GetRange(identifier, lineIndex),
        SelectionRange = line.GetRange(identifier, lineIndex)
    };

    private Symbol CreateForParameterValue (Parsing.Parameter parameter) => new() {
        Name = "ParameterValue",
        Kind = (int)GetParameterValueKind(parameter),
        Range = line.GetRange(parameter.Value, lineIndex),
        SelectionRange = line.GetRange(parameter.Value, lineIndex),
        Children = CreateForMixed(parameter.Value)
    };

    private IReadOnlyList<Symbol> CreateForMixed (MixedValue mixed)
    {
        var symbols = new List<Symbol>();
        foreach (var component in mixed)
            if (component is Expression expression)
                symbols.Add(CreateForExpression(expression));
            else if (component is IdentifiedText id)
                symbols.Add(CreateForTextIdentifier(id.Id));
        return symbols;
    }

    private Symbol CreateForExpression (Expression expression) => new() {
        Name = "Expression",
        Kind = (int)SymbolKind.Property,
        Range = line.GetRange(expression, lineIndex),
        SelectionRange = line.GetRange(expression, lineIndex)
    };

    private Symbol CreateForTextIdentifier (TextIdentifier textIdentifier) => new() {
        Name = "TextIdentifier",
        Kind = (int)SymbolKind.String,
        Range = line.GetRange(textIdentifier, lineIndex),
        SelectionRange = line.GetRange(textIdentifier, lineIndex)
    };

    private SymbolKind GetParameterValueKind (Parsing.Parameter parameter)
    {
        var paramMeta = metaProvider.FindParameter(commandId, parameter.Identifier);
        if (paramMeta is null || parameter.Value.Dynamic)
            return SymbolKind.String;
        if (paramMeta.ValueContainerType is ValueContainerType.List or ValueContainerType.NamedList)
            return SymbolKind.Array;
        return paramMeta.ValueType switch {
            ValueType.Integer => SymbolKind.Number,
            ValueType.Decimal => SymbolKind.Number,
            ValueType.Boolean => SymbolKind.Boolean,
            _ => SymbolKind.String
        };
    }
}
