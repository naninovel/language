using Naninovel.Metadata;

namespace Naninovel.Language;

public interface IMetadataObserver
{
    void HandleMetadataChanged (Project project);
}
