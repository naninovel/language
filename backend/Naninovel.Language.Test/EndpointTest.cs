using Moq;
using Naninovel.Metadata;
using Xunit;

namespace Naninovel.Language.Test;

public class EndpointTest
{
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly EndpointRegistry registry;

    public EndpointTest ()
    {
        registry = new(docs.Object);
    }

    [Fact]
    public void ExistChecksBothUriAndLabel ()
    {
        docs.SetupScript("foo.nani", "# bar");
        registry.HandleDocumentAdded("foo.nani");
        Assert.False(registry.ScriptExist("baz"));
        Assert.False(registry.LabelExist("foo", "baz"));
        Assert.True(registry.ScriptExist("foo"));
        Assert.True(registry.LabelExist("foo", "bar"));
    }

    [Fact]
    public void ExistUpdatedWhenDocumentRemoved ()
    {
        docs.SetupScript("foo.nani", "# bar");
        registry.HandleDocumentAdded("foo.nani");
        registry.HandleDocumentRemoved("foo.nani");
        Assert.False(registry.ScriptExist("foo"));
        Assert.False(registry.LabelExist("foo", "bar"));
    }

    [Fact]
    public void ExistUpdatedWhenDocumentChanged ()
    {
        docs.SetupScript("foo.nani", "# bar");
        registry.HandleDocumentAdded("foo.nani");
        docs.SetupScript("foo.nani", "# baz");
        registry.HandleDocumentChanged("foo.nani", new(0, 0));
        Assert.False(registry.LabelExist("foo", "bar"));
        Assert.True(registry.LabelExist("foo", "baz"));
    }

    [Fact]
    public void ExistLabelTrueAfterRemovingDuplicateLabel ()
    {
        docs.SetupScript("foo.nani", "# bar", "# bar");
        registry.HandleDocumentAdded("foo.nani");
        docs.SetupScript("foo.nani", "# bar");
        registry.HandleDocumentChanged("foo.nani", new(0, 1));
        Assert.True(registry.LabelExist("foo", "bar"));
    }

    [Fact]
    public void UsedEndpointsAreDetectedCorrectly ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("foo.nani", "# label1", "# label2", "@goto .label1");
        registry.HandleDocumentAdded("foo.nani");
        Assert.False(registry.ScriptUsed("bar"));
        Assert.False(registry.LabelUsed("foo", "label2"));
        Assert.True(registry.ScriptUsed("foo"));
        Assert.True(registry.LabelUsed("foo", "label1"));
    }

    [Fact]
    public void UsedEndpointsUpdatedWhenDocumentRemoved ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("script1.nani", "# label", "@goto script2.label");
        registry.HandleDocumentAdded("script1.nani");
        Assert.False(registry.ScriptUsed("script1"));
        Assert.False(registry.LabelUsed("script1", "label"));
        Assert.True(registry.ScriptUsed("script2"));
        Assert.True(registry.LabelUsed("script2", "label"));
        docs.SetupScript("script2.nani", "# label", "@goto script1.label");
        registry.HandleDocumentAdded("script2.nani");
        Assert.True(registry.ScriptUsed("script1"));
        Assert.True(registry.LabelUsed("script1", "label"));
        Assert.True(registry.ScriptUsed("script2"));
        Assert.True(registry.LabelUsed("script2", "label"));
        registry.HandleDocumentRemoved("script2.nani");
        Assert.False(registry.ScriptUsed("script1"));
        Assert.False(registry.LabelUsed("script1", "label"));
        Assert.True(registry.ScriptUsed("script2"));
        Assert.True(registry.LabelUsed("script2", "label"));
        registry.HandleDocumentRemoved("script1.nani");
        Assert.False(registry.ScriptUsed("script1"));
        Assert.False(registry.LabelUsed("script1", "label"));
        Assert.False(registry.ScriptUsed("script2"));
        Assert.False(registry.LabelUsed("script2", "label"));
    }

    [Fact]
    public void UsedEndpointsUpdatedWhenDocumentChanged ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("script1.nani", "# label", "[goto script2.label]");
        registry.HandleDocumentAdded("script1.nani");
        docs.SetupScript("script2.nani", "# label", "[goto script1.label]");
        registry.HandleDocumentAdded("script2.nani");
        Assert.True(registry.ScriptUsed("script2"));
        Assert.True(registry.LabelUsed("script2", "label"));
        docs.SetupScript("script1.nani", "# label");
        registry.HandleDocumentChanged("script1.nani", new(1, 1));
        Assert.False(registry.ScriptUsed("script2"));
        Assert.False(registry.LabelUsed("script2", "label"));
        docs.SetupScript("script2.nani", "# label");
        registry.HandleDocumentChanged("script2.nani", new(1, 1));
        Assert.False(registry.ScriptUsed("script1"));
        Assert.False(registry.LabelUsed("script1", "label"));
    }

    [Fact]
    public void UsedEndpointsTrueAfterRemovingDuplicateEndpoint ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("foo.nani", "@goto foo", "@goto foo");
        registry.HandleDocumentAdded("foo.nani");
        docs.SetupScript("foo.nani", "@goto foo");
        registry.HandleDocumentChanged("foo.nani", new(1, 1));
        Assert.True(registry.ScriptUsed("foo"));
    }
}
