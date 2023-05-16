namespace Naninovel.Language;

public interface IDocumentObserver
{
    void HandleDocumentAdded (string uri);
    void HandleDocumentRemoved (string uri);
    void HandleDocumentChanged (string uri, in LineRange range);
}
