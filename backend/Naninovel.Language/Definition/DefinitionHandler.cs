using System.Linq;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_definition

public class DefinitionHandler
{
    private readonly IDocumentRegistry registry;
    private readonly IEndpointResolver resolver;
    private Position position;
    private DocumentLine line;
    private string documentUri = null!;

    public DefinitionHandler (IDocumentRegistry registry, IEndpointResolver resolver)
    {
        this.registry = registry;
        this.resolver = resolver;
    }

    public LocationLink[]? GotoDefinition (string documentUri, Position position)
    {
        var documentLine = registry.Get(documentUri)[position.Line];
        ResetState(documentLine, position, documentUri);
        return documentLine.Script switch {
            GenericLine line => FromGenericLine(line),
            CommandLine line => FromCommand(line.Command),
            _ => null
        };
    }

    private void ResetState (in DocumentLine line, in Position position, string documentUri)
    {
        this.line = line;
        this.position = position;
        this.documentUri = documentUri;
    }

    private bool IsCursorOver (ILineComponent? content) => line.IsCursorOver(content, position);

    private LocationLink[]? FromGenericLine (GenericLine line)
    {
        if (line.Content.OfType<InlinedCommand>().FirstOrDefault(IsCursorOver) is { } inlined)
            return FromCommand(inlined.Command);
        return null;
    }

    private LocationLink[]? FromCommand (Command command)
    {
        if (!resolver.TryResolve(command, out var script, out var label)) return null;
        var uri = script != null ? FindDocumentUriByName(script) : documentUri;
        if (uri is null) return null;
        var document = registry.Get(uri);
        var (range, selection) = GetRanges(document, label);
        return new[] { new LocationLink(null, uri, range, selection) };
    }

    private string? FindDocumentUriByName (string name)
    {
        foreach (var uri in registry.GetAllUris())
            if (uri.EndsWith(name + ".nani"))
                return uri;
        return null;
    }

    private (Range Range, Range Selection) GetRanges (IDocument document, string? label)
    {
        var startLineIndex = FindLabelLineIndex(document, label) ?? 0;
        var endLineIndex = (FindNextLabelLineIndex(document, startLineIndex + 1) ?? document.LineCount) - 1;
        var range = new Range(new(startLineIndex, 0), new(endLineIndex, document[endLineIndex].Range.EndIndex + 1));
        var selection = new Range(new(startLineIndex, 0), new(startLineIndex, document[startLineIndex].Range.EndIndex + 1));
        return (range, selection);
    }

    private int? FindLabelLineIndex (IDocument document, string? label)
    {
        for (int i = 0; i < document.LineCount; i++)
            if (document[i] is { Script: LabelLine labelLine } && labelLine.Label == label)
                return i;
        return null;
    }

    private int? FindNextLabelLineIndex (IDocument document, int startLineIndex)
    {
        for (int i = startLineIndex; i < document.LineCount; i++)
            if (document[i] is { Script: LabelLine })
                return i;
        return null;
    }
}
