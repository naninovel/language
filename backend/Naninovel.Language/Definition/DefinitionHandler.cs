using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class DefinitionHandler : IDefinitionHandler, IMetadataObserver
{
    private readonly MetadataProvider meta = new();
    private readonly EndpointResolver resolver;
    private readonly IDocumentRegistry registry;
    private Position position;
    private DocumentLine line;
    private string documentUri = null!;

    public DefinitionHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
        resolver = new(meta);
    }

    public void HandleMetadataChanged (Project project) => meta.Update(project);

    public IReadOnlyList<LocationLink>? GotoDefinition (string documentUri, Position position)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
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
        this.documentUri = Uri.UnescapeDataString(documentUri);
    }

    private bool IsCursorOver (ILineComponent? content) => line.IsCursorOver(content, position);

    private LocationLink[]? FromGenericLine (GenericLine line)
    {
        if (line.Content.OfType<InlinedCommand>().FirstOrDefault(IsCursorOver) is { } inlined)
            return FromCommand(inlined.Command);
        return null;
    }

    private LocationLink[]? FromCommand (Parsing.Command command)
    {
        if (!resolver.TryResolve(command, out var point)) return null;
        var uri = point.Script != null ? FindDocumentUriByName(point.Script) : documentUri;
        if (uri is null) return null;
        var document = registry.Get(uri);
        var (range, selection) = GetRanges(document, point.Label);
        return [new LocationLink(null, uri, range, selection)];
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
        var range = new Range(new(startLineIndex, 0), new(endLineIndex, document[endLineIndex].Range.End + 1));
        var selection = new Range(new(startLineIndex, 0), new(startLineIndex, document[startLineIndex].Range.End + 1));
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
