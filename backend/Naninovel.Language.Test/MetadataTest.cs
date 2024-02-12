using Moq;
using Naninovel.Metadata;
using Naninovel.TestUtilities;

namespace Naninovel.Language.Test;

public class MetadataTest
{
    private readonly NotifierMock<IMetadataObserver> notifier = new();
    private readonly MetadataHandler handler;

    public MetadataTest ()
    {
        handler = new(notifier);
    }

    [Fact]
    public void NotifiesOnMetadataUpdate ()
    {
        var meta = new Project();
        handler.UpdateMetadata(meta);
        notifier.Verify(n => n.HandleMetadataChanged(meta), Times.Once);
        notifier.VerifyNoOtherCalls();
    }
}
