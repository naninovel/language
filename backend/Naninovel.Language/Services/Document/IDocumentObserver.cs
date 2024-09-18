namespace Naninovel.Language;

public interface IDocumentObserver
{
    /// <summary>
    /// A new document with specified URI has been added.
    /// </summary>
    /// <remarks>
    /// The document can be accessed via registry using specified URI.
    /// </remarks>
    void HandleDocumentAdded (string uri);
    /// <summary>
    /// A document with specified URI has been removed.
    /// </summary>
    /// <remarks>
    /// The removed document can still be accessed via registry using specified URI.
    /// </remarks>
    void HandleDocumentRemoved (string uri);
    /// <summary>
    /// Lines of an existing document in specified range are going to be changed.
    /// </summary>
    /// <remarks>
    /// The document is not changed yet; it's safe to get it from registry via specified URI
    /// and access lines via indexes in the specified range (including end).
    /// </remarks>
    void HandleDocumentChanging (string uri, LineRange range);
    /// <summary>
    /// Lines of an existing document in specified range were changed.
    /// </summary>
    /// <remarks>
    /// The document has changed; it's safe to get it from registry via specified URI
    /// and access lines via indexes in the specified range (including end).
    /// </remarks>
    void HandleDocumentChanged (string uri, LineRange range);
}
