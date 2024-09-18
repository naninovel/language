using Naninovel.Metadata;

namespace Naninovel.Language;

public class MetadataUpdater (IObserverNotifier<IMetadataObserver> notifier) : IMetadataUpdater
{
    public void UpdateMetadata (Project meta)
    {
        notifier.Notify(n => n.HandleMetadataChanged(meta));
    }
}
