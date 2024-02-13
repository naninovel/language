using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;
using static Naninovel.Metadata.Constants;

namespace Naninovel.Language;

public class CompletionHandler : ICompletionHandler, IMetadataObserver
{
    private readonly IDocumentRegistry docs;
    private readonly CompletionProvider provider = new();
    private readonly CommandCompletionHandler commandHandler;
    private readonly MetadataProvider metaProvider = new();

    private char charBehindCursor => line.GetCharBehindCursor(position);
    private Position position;
    private DocumentLine line;
    private string scriptName = string.Empty;

    public CompletionHandler (IDocumentRegistry docs, IEndpointRegistry endpoints)
    {
        this.docs = docs;
        commandHandler = new CommandCompletionHandler(metaProvider, provider, endpoints);
    }

    public void HandleMetadataChanged (Project meta)
    {
        metaProvider.Update(meta);
        provider.Update(metaProvider);
    }

    public IReadOnlyList<CompletionItem> Complete (string documentUri, Position position)
    {
        var documentLine = docs.Get(documentUri)[position.Line];
        var scriptName = ToScriptName(documentUri);
        ResetState(documentLine, position, scriptName);
        return documentLine.Script switch {
            GenericLine line => GetForGenericLine(line),
            CommandLine line => commandHandler.Handle(line.Command, position, documentLine, scriptName),
            _ => Array.Empty<CompletionItem>()
        };
    }

    private void ResetState (in DocumentLine line, in Position position, string scriptName)
    {
        this.line = line;
        this.position = position;
        this.scriptName = scriptName;
    }

    private bool IsCursorOver (ILineComponent? content) => line.IsCursorOver(content, position);

    private CompletionItem[] GetForGenericLine (GenericLine genericLine)
    {
        if (string.IsNullOrEmpty(line.Text) || IsCursorOver(genericLine.Prefix?.Author))
            return provider.GetActors(CharacterType);
        if (ShouldCompleteAuthorAppearance(genericLine, out var authorId))
            return provider.GetAppearances(authorId);
        if (genericLine.Content.OfType<InlinedCommand>().FirstOrDefault(IsCursorOver) is { } inlined)
            return commandHandler.Handle(inlined.Command, position, line, scriptName);
        if (genericLine.Content.OfType<MixedValue>().FirstOrDefault(IsCursorOver) is { } text)
            return GetForGenericText(text);
        return Array.Empty<CompletionItem>();
    }

    private bool ShouldCompleteAuthorAppearance (GenericLine genericLine, out string authorId)
    {
        authorId = genericLine.Prefix?.Author;
        if (IsCursorOver(genericLine.Prefix?.Appearance)) return true;
        return charBehindCursor == Identifiers.AuthorAppearance[0] &&
               !(authorId = line.Text[..(position.Character - 1)]).Any(char.IsWhiteSpace);
    }

    private CompletionItem[] GetForGenericText (MixedValue text)
    {
        if (!text.OfType<Expression>().Any(IsCursorOver))
            return Array.Empty<CompletionItem>();
        if (charBehindCursor == Identifiers.ExpressionClose[0])
            return Array.Empty<CompletionItem>();
        return provider.GetExpressions();
    }
}
