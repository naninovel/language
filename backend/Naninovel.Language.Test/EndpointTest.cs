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
    public void NothingExistByDefault ()
    {
        Assert.False(registry.ScriptExist("foo"));
        Assert.False(registry.LabelExist(new("foo", "bar")));
        Assert.False(registry.NavigatorExist(new("foo")));
        Assert.False(registry.NavigatorExist(new("foo", "bar")));
        Assert.Empty(registry.GetLabelLocations(new("foo", "bar")));
        Assert.Empty(registry.GetNavigatorLocations(new("foo")));
        Assert.Empty(registry.GetNavigatorLocations(new("foo", "bar")));
    }

    [Fact]
    public void ResolvesAfterDocumentAdded ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("script1.nani", "# label1", "# label2", "@goto script1.label1", "@goto .label1", "@goto script2");
        docs.SetupScript("script2.nani", "# label1", "# label1", "[goto script1]", "[goto script1]", "[goto script2.label1]");
        registry.HandleDocumentAdded("script1.nani");
        registry.HandleDocumentAdded("script2.nani");
        AssertLabelLocations("script1", "label1", new LineLocation("script1.nani", 0));
        AssertLabelLocations("script1", "label2", new LineLocation("script1.nani", 1));
        AssertLabelLocations("script2", "label1", new LineLocation("script2.nani", 0), new("script2.nani", 1));
        AssertNavigatorLocations(new("script1"), new LineLocation("script2.nani", 2), new("script2.nani", 3));
        AssertNavigatorLocations(new("script1", "label1"), new LineLocation("script1.nani", 2), new("script1.nani", 3));
        AssertNavigatorLocations(new("script2"), new LineLocation("script1.nani", 4));
        AssertNavigatorLocations(new("script2", "label1"), new LineLocation("script2.nani", 4));
    }

    [Fact]
    public void ResolvesAfterDocumentRemoved ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("script1.nani", "# label1", "# label2", "@goto script1.label1", "@goto .label1", "@goto script2");
        docs.SetupScript("script2.nani", "# label1", "# label1", "[goto script1]", "[goto script1]", "[goto script2.label1]");
        registry.HandleDocumentAdded("script1.nani");
        registry.HandleDocumentAdded("script2.nani");
        registry.HandleDocumentRemoved("script1.nani");
        Assert.False(registry.ScriptExist("script1"));
        AssertLabelDoesntExist("script1", "label1");
        AssertLabelDoesntExist("script1", "label2");
        AssertLabelLocations("script2", "label1", new LineLocation("script2.nani", 0), new("script2.nani", 1));
        AssertNavigatorLocations(new("script1"), new LineLocation("script2.nani", 2), new("script2.nani", 3));
        AssertNavigatorDoesntExist(new("script1", "label1"));
        AssertNavigatorDoesntExist(new("script2"));
        AssertNavigatorLocations(new("script2", "label1"), new LineLocation("script2.nani", 4));
    }

    [Fact]
    public void ResolvesAfterDocumentChanged ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("script1.nani", "# label1", "# label2", "@goto script1.label1", "@goto .label1", "@goto script2");
        docs.SetupScript("script2.nani", "# label1", "# label1", "[goto script1]", "[goto script1]", "[goto script2.label1]");
        registry.HandleDocumentAdded("script1.nani");
        registry.HandleDocumentAdded("script2.nani");
        registry.HandleDocumentChanging("script1.nani", new(0, 4));
        registry.HandleDocumentChanging("script2.nani", new(1, 4));
        docs.SetupScript("script1.nani", "@goto script2", "@goto .label1", "@goto script2");
        docs.SetupScript("script2.nani", "# label1", "[goto script1]", "[goto script2.label1]");
        registry.HandleDocumentChanged("script1.nani", new(0, 4));
        registry.HandleDocumentChanged("script2.nani", new(1, 4));
        AssertLabelDoesntExist("script1", "label1");
        AssertLabelDoesntExist("script1", "label2");
        AssertLabelLocations("script2", "label1", new LineLocation("script2.nani", 0));
        AssertNavigatorLocations(new("script1"), new LineLocation("script2.nani", 1));
        AssertNavigatorLocations(new("script1", "label1"), new LineLocation("script1.nani", 1));
        AssertNavigatorLocations(new("script2"), new LineLocation("script1.nani", 0), new("script1.nani", 2));
        AssertNavigatorLocations(new("script2", "label1"), new LineLocation("script2.nani", 2));
    }

    [Fact]
    public void ResolvesChangesInSameDocument ()
    {
        registry.HandleMetadataChanged(new Project().SetupCommandWithEndpoint("goto"));
        docs.SetupScript("script.nani", "# label", "@goto .label");
        registry.HandleDocumentAdded("script.nani");
        registry.HandleDocumentChanging("script.nani", new(1, 1));
        docs.SetupScript("script.nani", "# label", "@goto .foo");
        registry.HandleDocumentChanged("script.nani", new(1, 1));
        AssertNavigatorLocations(new("script", "foo"), new LineLocation("script.nani", 1));
        AssertNavigatorDoesntExist(new("script", "label"));
    }

    private void AssertLabelLocations (string scriptName, string label, params LineLocation[] locations)
    {
        Assert.True(registry.ScriptExist(scriptName));
        Assert.True(registry.LabelExist(new(scriptName, label)));
        Assert.Equal(locations, registry.GetLabelLocations(new(scriptName, label)));
    }

    private void AssertLabelDoesntExist (string scriptName, string label)
    {
        Assert.False(registry.LabelExist(new(scriptName, label)));
        Assert.Empty(registry.GetLabelLocations(new(scriptName, label)));
    }

    private void AssertNavigatorLocations (QualifiedEndpoint endpoint, params LineLocation[] locations)
    {
        Assert.True(registry.NavigatorExist(endpoint));
        Assert.Equal(locations, registry.GetNavigatorLocations(endpoint));
    }

    private void AssertNavigatorDoesntExist (QualifiedEndpoint endpoint)
    {
        Assert.False(registry.NavigatorExist(endpoint));
        Assert.Empty(registry.GetNavigatorLocations(endpoint));
    }
}
