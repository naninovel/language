using Naninovel.Metadata;
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
        registry = new(notifier);
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
        Assert.False(registry.Contains("foo"));
    }

    [Fact]
    public void ContainsChecksBothUriAndLabel ()
    {
        registry.Upsert("foo", CreateDocument("# bar"));
        Assert.False(registry.Contains("baz"));
        Assert.False(registry.Contains("foo", "baz"));
        Assert.True(registry.Contains("foo"));
        Assert.True(registry.Contains("foo", "bar"));
    }

    [Fact]
    public void ContainsUpdatedWhenDocumentRemoved ()
    {
        registry.Upsert("foo", CreateDocument("# bar"));
        registry.Remove("foo");
        Assert.False(registry.Contains("foo"));
    }

    [Fact]
    public void ContainsUpdatedWhenDocumentChanged ()
    {
        registry.Upsert("foo", CreateDocument("# bar"));
        registry.Change("foo", new[] { new DocumentChange(new(new(0, 2), new(0, 5)), "baz") });
        Assert.False(registry.Contains("foo", "bar"));
        Assert.True(registry.Contains("foo", "baz"));
    }

    [Fact]
    public void ContainsTrueAfterRemovingDuplicateLabel ()
    {
        registry.Upsert("foo", CreateDocument("# bar", "# bar"));
        registry.Change("foo", new[] { new DocumentChange(new(new(0, 5), new(1, 5)), "") });
        Assert.True(registry.Contains("foo", "bar"));
    }

    [Fact]
    public void UsedEndpointsAreDetectedCorrectly ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        registry.Upsert("foo.nani", CreateDocument("# label1", "# label2", "@goto .label1"));
        Assert.False(registry.IsEndpointUsed("bar"));
        Assert.False(registry.IsEndpointUsed("foo", "label2"));
        Assert.True(registry.IsEndpointUsed("foo"));
        Assert.True(registry.IsEndpointUsed("foo", "label1"));
    }

    [Fact]
    public void UsedEndpointsUpdatedWhenDocumentRemoved ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        registry.Upsert("script1.nani", CreateDocument("# label", "@goto script2.label"));
        Assert.False(registry.IsEndpointUsed("script1"));
        Assert.False(registry.IsEndpointUsed("script1", "label"));
        Assert.True(registry.IsEndpointUsed("script2"));
        Assert.True(registry.IsEndpointUsed("script2", "label"));
        registry.Upsert("script2.nani", CreateDocument("# label", "@goto script1.label"));
        Assert.True(registry.IsEndpointUsed("script1"));
        Assert.True(registry.IsEndpointUsed("script1", "label"));
        Assert.True(registry.IsEndpointUsed("script2"));
        Assert.True(registry.IsEndpointUsed("script2", "label"));
        registry.Remove("script2.nani");
        Assert.False(registry.IsEndpointUsed("script1"));
        Assert.False(registry.IsEndpointUsed("script1", "label"));
        Assert.True(registry.IsEndpointUsed("script2"));
        Assert.True(registry.IsEndpointUsed("script2", "label"));
        registry.Remove("script1.nani");
        Assert.False(registry.IsEndpointUsed("script1"));
        Assert.False(registry.IsEndpointUsed("script1", "label"));
        Assert.False(registry.IsEndpointUsed("script2"));
        Assert.False(registry.IsEndpointUsed("script2", "label"));
    }

    [Fact]
    public void UsedEndpointsUpdatedWhenDocumentChanged ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        registry.Upsert("script1.nani", CreateDocument("# label", "[goto script2.label]"));
        registry.Upsert("script2.nani", CreateDocument("# label", "[goto script1.label]"));
        Assert.True(registry.IsEndpointUsed("script2"));
        Assert.True(registry.IsEndpointUsed("script2", "label"));
        registry.Change("script1.nani", new[] { new DocumentChange(new(new(1, 6), new(1, 13)), "") });
        Assert.False(registry.IsEndpointUsed("script2"));
        Assert.False(registry.IsEndpointUsed("script2", "label"));
        registry.Change("script2.nani", new[] { new DocumentChange(new(new(1, 6), new(1, 13)), "") });
        Assert.True(registry.IsEndpointUsed("script2"));
        Assert.True(registry.IsEndpointUsed("script2", "label"));
    }

    [Fact]
    public void UsedEndpointsTrueAfterRemovingDuplicateEndpoint ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        registry.Upsert("foo", CreateDocument("@goto foo", "@goto foo"));
        registry.Change("foo", new[] { new DocumentChange(new(new(0, 9), new(1, 9)), "") });
        Assert.True(registry.IsEndpointUsed("foo"));
    }
}
