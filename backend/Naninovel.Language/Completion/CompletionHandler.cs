using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Metadata.Constants;

namespace Naninovel.Language;

public class CompletionHandler : ICompletionHandler, IMetadataObserver
{
    private readonly IMetadata meta;
    private readonly IDocumentRegistry docs;
    private readonly CompletionProvider completions;
    private readonly CommandCompletionHandler commandHandler;
    private readonly ExpressionCompletionHandler expHandler;

    private char charBehindCursor;
    private Position position;
    private DocumentLine line;
    private string scriptName = string.Empty;

    public CompletionHandler (IMetadata meta, IDocumentRegistry docs, IEndpointRegistry endpoints)
    {
        this.meta = meta;
        this.docs = docs;
        completions = new CompletionProvider(meta.Syntax);
        commandHandler = new CommandCompletionHandler(meta, completions, endpoints);
        expHandler = new ExpressionCompletionHandler(meta, endpoints, completions);
    }

    public void HandleMetadataChanged (Project project)
    {
        completions.Update(meta);
    }

    public IReadOnlyList<CompletionItem> Complete (string documentUri, Position position)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        var documentLine = docs.Get(documentUri)[position.Line];
        var scriptName = ToScriptName(documentUri);
        ResetState(documentLine, position, scriptName);
        return documentLine.Script switch {
            GenericLine line => GetForGenericLine(line),
            CommandLine line => commandHandler.Handle(line.Command, position, documentLine, scriptName, false),
            _ => []
        };
    }

    private void ResetState (in DocumentLine line, in Position position, string scriptName)
    {
        this.line = line;
        this.position = position;
        this.scriptName = scriptName;
        charBehindCursor = line.GetCharBehindCursor(position);
    }

    private bool IsCursorOver (ILineComponent? content) => line.IsCursorOver(content, position);

    private CompletionItem[] GetForGenericLine (GenericLine genericLine)
    {
        if (string.IsNullOrEmpty(line.Text) || IsCursorOver(genericLine.Prefix?.Author))
            return completions.GetActors(CharacterType);
        if (ShouldCompleteAuthorAppearance(genericLine, out var authorId))
            return completions.GetAppearances(authorId, CharacterType);
        if (genericLine.Content.OfType<InlinedCommand>().FirstOrDefault(IsCursorOver) is { } inlined)
            return commandHandler.Handle(inlined.Command, position, line, scriptName, true);
        if (genericLine.Content.OfType<MixedValue>().FirstOrDefault(IsCursorOver) is { } text)
            return GetForGenericText(text);
        return [];
    }

    private bool ShouldCompleteAuthorAppearance (GenericLine genericLine, out string authorId)
    {
        authorId = genericLine.Prefix?.Author;
        if (IsCursorOver(genericLine.Prefix?.Appearance)) return true;
        return charBehindCursor == meta.Syntax.AuthorAppearance[0] &&
               !(authorId = line.Text[..(position.Character - 1)]).Any(char.IsWhiteSpace);
    }

    private CompletionItem[] GetForGenericText (MixedValue text)
    {
        if (text.OfType<Parsing.Expression>().FirstOrDefault(IsCursorOver) is not { } exp)
            return [];
        if (charBehindCursor == meta.Syntax.ExpressionClose[0])
            return [];
        return expHandler.Handle(exp.Body, position, line, scriptName);
    }
}
