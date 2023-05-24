using System.Collections.Generic;
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
    public void NotifiesAboutRemovedDocument ()
    {
        registry.Upsert("foo", CreateDocument(""));
        registry.Remove("foo");
        notifier.Verify(d => d.HandleDocumentAdded("foo"), Times.Once);
        notifier.Verify(d => d.HandleDocumentRemoved("foo"), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void NotifiesAboutRenamedDocument ()
    {
        registry.Upsert("foo", CreateDocument(""));
        registry.Rename("foo", "bar");
        notifier.Verify(d => d.HandleDocumentAdded("foo"), Times.Once);
        notifier.Verify(d => d.HandleDocumentRemoved("foo"), Times.Once);
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
        notifier.Verify(d => d.HandleDocumentChanging("foo", new LineRange(0, 2)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(0, 2)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanging("foo", new LineRange(0, 0)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(0, 0)), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void NotifiesAboutChangedDocument ()
    {
        registry.Upsert("foo", CreateDocument("a"));
        registry.Change("foo", new[] { new DocumentChange(new(new(0, 0), new(0, 1)), "b") });
        notifier.Verify(d => d.HandleDocumentAdded("foo"), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanging("foo", new LineRange(0, 0)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(0, 0)), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void ChangedRangeIncludesAllLinesAfterDeleted ()
    {
        registry.Upsert("foo", CreateDocument("0", "1", "2", "3"));
        registry.Change("foo", new[] { new DocumentChange(new(new(1, 0), new(2, 1)), "12") });
        notifier.Verify(d => d.HandleDocumentChanging("foo", new LineRange(1, 3)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(1, 2)), Times.Once);
    }

    [Fact]
    public void ChangedRangeIncludesAllLinesAfterInserted ()
    {
        registry.Upsert("foo", CreateDocument("0", "1234", "5"));
        registry.Change("foo", new[] { new DocumentChange(new(new(1, 0), new(1, 4)), "1\r2\n3\r\n4") });
        notifier.Verify(d => d.HandleDocumentChanging("foo", new LineRange(1, 2)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(1, 5)), Times.Once);
    }

    [Fact]
    public void ChangedRangeDoesntIncludesAllLinesWhenNotInsertingOrDeletingLines ()
    {
        registry.Upsert("foo", CreateDocument("0", "1", "2"));
        registry.Change("foo", new[] { new DocumentChange(new(new(1, 0), new(1, 1)), "") });
        notifier.Verify(d => d.HandleDocumentChanging("foo", new LineRange(1, 1)), Times.Once);
        notifier.Verify(d => d.HandleDocumentChanged("foo", new LineRange(1, 1)), Times.Once);
    }

    [Fact]
    public void EndpointsAreNotifiedBeforeOthers ()
    {
        var endpoints = new Mock<IEndpointRegistry>().As<IDocumentObserver>().Object;
        var other = new Mock<IDocumentObserver>().Object;
        observers.Verify(o => o.Order(It.Is<IComparer<IDocumentObserver>>(c => c.Compare(endpoints, other) == -1)));
        observers.Verify(o => o.Order(It.Is<IComparer<IDocumentObserver>>(c => c.Compare(other, endpoints) == 1)));
    }
}
