using Naninovel.Parsing;

namespace Naninovel.Language;

public class RenameHandler (IEndpointRenamer renamer, IDocumentRegistry docs) : IRenameHandler
{
    public Range? PrepareRename (string documentUri, Position position)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        if (docs.Get(documentUri)[position.Line] is not { Script: LabelLine labelLine } docLine) return null;
        return docLine.GetRange(labelLine.Label, position.Line);
    }

    public WorkspaceEdit? Rename (string documentUri, Position position, string newName)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        if (docs.Get(documentUri)[position.Line] is not { Script: LabelLine labelLine }) return null;
        return renamer.RenameLabel(documentUri, labelLine.Label, newName);
    }
}
