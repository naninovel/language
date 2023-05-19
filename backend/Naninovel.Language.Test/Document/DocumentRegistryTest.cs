using Moq;
using Naninovel.TestUtilities;
using Xunit;
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
    public void CanRemoveDocument ()
    {
        registry.Upsert("foo", CreateDocument(""));
        registry.Remove("foo");
        Assert.Contains("not found", Assert.Throws<Error>(() => registry.Get("foo")).Message);
    }
}
