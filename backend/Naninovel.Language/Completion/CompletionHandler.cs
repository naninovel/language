using System;
using System.IO;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Metadata.Constants;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#textDocument_completion

public class CompletionHandler
{
    private readonly DocumentRegistry registry;
    private readonly CompletionProvider provider;
    private readonly CommandCompletionHandler commandHandler;

    private char charBehindCursor => position.GetCharBehindCursor(lineText);
    private Position position = null!;
    private string lineText = string.Empty;
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
        ResetState(documentLine.Text, position, scriptName);
        return documentLine.Script switch {
            GenericTextLine line => GetForGenericLine(line),
            CommandLine line => commandHandler.Handle(line.Command, position, lineText, scriptName),
            EmptyLine => provider.GetActors(CharacterType),
            _ => Array.Empty<CompletionItem>()
        };
    }

    private void ResetState (string lineText, Position position, string scriptName)
    {
        this.lineText = lineText;
        this.position = position;
        this.scriptName = scriptName;
    }

    private bool IsCursorOver (LineContent content) => position.IsCursorOver(content);

    private CompletionItem[] GetForGenericLine (GenericTextLine line)
    {
        if (IsCursorOver(line.Prefix.AuthorIdentifier))
            return provider.GetActors(CharacterType);
        if (ShouldCompleteAuthorAppearance(line, out var authorId))
            return provider.GetAppearances(authorId);
        if (line.Content.OfType<InlinedCommand>().FirstOrDefault(IsCursorOver) is { } inlined)
            return commandHandler.Handle(inlined.Command, position, lineText, scriptName);
        if (line.Content.OfType<GenericText>().FirstOrDefault(IsCursorOver) is { } text)
            return GetForGenericText(text);
        return Array.Empty<CompletionItem>();
    }

    private bool ShouldCompleteAuthorAppearance (GenericTextLine line, out string authorId)
    {
        authorId = line.Prefix.AuthorIdentifier;
        if (IsCursorOver(line.Prefix.AuthorAppearance)) return true;
        return charBehindCursor == Identifiers.AuthorAppearance[0] &&
               !(authorId = lineText[..(position.Character - 1)]).Any(char.IsWhiteSpace);
    }

    private CompletionItem[] GetForGenericText (GenericText text)
    {
        if (!text.Expressions.Any(IsCursorOver))
            return Array.Empty<CompletionItem>();
        if (charBehindCursor == Identifiers.ExpressionClose[0])
            return Array.Empty<CompletionItem>();
        return provider.GetExpressions();
    }
}
