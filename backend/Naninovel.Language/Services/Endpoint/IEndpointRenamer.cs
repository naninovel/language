namespace Naninovel.Language;

public interface IEndpointRenamer
{
    WorkspaceEdit? RenameLabel (string documentUri, string oldLabel, string newLabel);
    WorkspaceEdit? RenameScript (string oldDocumentUri, string newDocumentUri);
    WorkspaceEdit? RenameDirectory (string oldDirectoryUri, string newDirectoryUri);
}
