using Moq;
using Naninovel.TestUtilities;
using Xunit;
using static Naninovel.Language.Test.Common;

namespace Naninovel.Language.Test;

public class DocumentNotifierTest
{
    private readonly Mock<IObserverRegistry<IDocumentObserver>> observers = new();
    private readonly NotifierMock<IDocumentObserver> notifier = new();
    private readonly DocumentRegistry registry;

    public DocumentNotifierTest ()
    {
        registry = new(observers.Object, notifier);
    }

    [Fact]
    public void NotifiesAboutAddedDocument ()
    {
        registry.Upsert("foo", CreateDocument(""));
        registry.Upsert("bar", CreateDocument(""));
        notifier.Verify(d => d.HandleDocumentAdded("foo"), Times.Once);
        notifier.Verify(d => d.HandleDocumentAdded("bar"), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void WhenInsertingExistingDocumentNotifiesAboutFullChange ()
    {
        registry.Upsert("foo", CreateDocument("a"));
        registry.Upsert("foo", CreateDocument("a", "b", "c"));
        registry.Upsert("foo", CreateDocument(""));
        notifier.Verify(d => d.HandleDocumentAdded("foo"), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(0, 2)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(0, 0)), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void NotifiesAboutChangedDocument ()
    {
        registry.Upsert("foo", CreateDocument("a"));
        registry.Change("foo", new[] { new DocumentChange(new(new(0, 0), new(0, 1)), "b") });
        notifier.Verify(d => d.HandleDocumentAdded("foo"), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(0, 0)), Times.Once);
        notifier.VerifyNoOtherCalls();
    }
}
