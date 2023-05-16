using Naninovel.Metadata;

namespace Naninovel.Language;

public class MetadataHandler : IMetadataHandler
{
    private readonly IObserverNotifier<IMetadataObserver> notifier;

    public MetadataHandler (IObserverNotifier<IMetadataObserver> notifier)
    {
        this.notifier = notifier;
    }

    public void UpdateMetadata (Project meta)
    {
        notifier.Notify(n => n.HandleMetadataChanged(meta));
    }
}
