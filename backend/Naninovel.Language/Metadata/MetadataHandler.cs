using Naninovel.Metadata;

namespace Naninovel.Language;

public class MetadataHandler(IObserverNotifier<IMetadataObserver> notifier) : IMetadataHandler
{
    public void UpdateMetadata (Project meta)
    {
        notifier.Notify(n => n.HandleMetadataChanged(meta));
    }
}
