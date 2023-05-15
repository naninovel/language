using Moq;
using Naninovel.Metadata;
using Naninovel.TestUtilities;
using Xunit;

namespace Naninovel.Language.Test;

public class MetadataTest
{
    private readonly NotifierMock<IMetadataObserver> notifier = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IDiagnoser> diagnoser = new();
    private readonly MetadataHandler handler;

    public MetadataTest ()
    {
        handler = new(notifier, docs.Object, diagnoser.Object);
    }

    [Fact]
    public void NotifiesOnMetadataUpdate ()
    {
        var meta = new Project();
        docs.SetupScript("foo");
        handler.UpdateMetadata(meta);
        notifier.Verify(n => n.HandleMetadataChanged(meta), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void DiagnosesAllDocumentsOnMetadataUpdate ()
    {
        docs.SetupScript("foo");
        docs.SetupScript("bar");
        handler.UpdateMetadata(new());
        diagnoser.Verify(d => d.Diagnose("foo", null), Times.Once);
        diagnoser.Verify(d => d.Diagnose("bar", null), Times.Once);
        diagnoser.VerifyNoOtherCalls();
    }
}
