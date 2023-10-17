using Moq;
using Naninovel.TestUtilities;
using static Naninovel.Language.Test.Common;

namespace Naninovel.Language.Test;

public class DocumentRegistryTest
{
    private readonly NotifierMock<IDocumentObserver> notifier = new();
    private readonly DocumentRegistry registry;

    public DocumentRegistryTest ()
    {
        registry = new(new Mock<IObserverRegistry<IDocumentObserver>>().Object, notifier);
    }

    [Fact]
    public void GetAllUrisReturnsEmptyByDefault ()
    {
        Assert.Empty(registry.GetAllUris());
    }

    [Fact]
    public void ErrsWhenGettingUnknownUri ()
    {
        Assert.Contains("not found", Assert.Throws<Error>(() => registry.Get("foo")).Message);
    }

    [Fact]
    public void ErrsWhenRemovingUnknownDocument ()
    {
        Assert.Contains("not found", Assert.Throws<Error>(() => registry.Remove("foo")).Message);
    }

    [Fact]
    public void ErrsWhenRenamingUnknownDocument ()
    {
        Assert.Contains("not found", Assert.Throws<Error>(() => registry.Rename("foo", "bar")).Message);
    }

    [Fact]
    public void CanGetUpsertDocument ()
    {
        registry.Upsert("foo", CreateDocument(""));
        Assert.Equal(1, registry.Get("foo").LineCount);
    }

    [Fact]
    public void WhenExistingDocumentUpsertItsReplaced ()
    {
        registry.Upsert("foo", CreateDocument("1"));
        registry.Upsert("foo", CreateDocument("2"));
        Assert.Equal("2", registry.Get("foo")[0].Text);
    }

    [Fact]
    public void CanCheckExistenceByUri ()
    {
        Assert.False(registry.Contains("foo"));
        registry.Upsert("foo", CreateDocument(""));
        Assert.True(registry.Contains("foo"));
    }

    [Fact]
    public void CanRemoveDocument ()
    {
        registry.Upsert("foo", CreateDocument(""));
        registry.Remove("foo");
        Assert.False(registry.Contains("foo"));
    }

    [Fact]
    public void CanRenameDocument ()
    {
        var doc = CreateDocument("");
        registry.Upsert("foo", doc);
        registry.Rename("foo", "bar");
        Assert.False(registry.Contains("foo"));
        Assert.True(registry.Contains("bar"));
        Assert.Equal(doc, registry.Get("bar"));
    }
}
