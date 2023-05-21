namespace Naninovel.Language;

public interface IDocumentObserver
{
    void HandleDocumentAdded (string uri);
    void HandleDocumentRemoved (string uri);
    void HandleDocumentChanging (string uri, LineRange range);
    void HandleDocumentChanged (string uri, LineRange range);
}
