using System;
using System.IO;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Metadata.Constants;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_completion

public class CompletionHandler
{
    private readonly DocumentRegistry registry;
    private readonly CompletionProvider provider;
    private readonly CommandCompletionHandler commandHandler;

    private char charBehindCursor => line.GetCharBehindCursor(position);
    private Position position;
    private DocumentLine line;
    private string scriptName = string.Empty;

    public CompletionHandler (MetadataProvider meta, DocumentRegistry registry)
    {
        this.registry = registry;
        provider = new CompletionProvider(meta);
        commandHandler = new CommandCompletionHandler(meta, provider);
    }

    public CompletionItem[] Complete (string documentUri, Position position)
    {
        var documentLine = registry.Get(documentUri)[position.Line];
        var scriptName = Path.GetFileNameWithoutExtension(documentUri);
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
