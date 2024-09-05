using Moq;

namespace Naninovel.Language.Test;

public class EndpointTest
{
    private readonly MetadataMock meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly EndpointRegistry registry;

    public EndpointTest ()
    {
        registry = new(meta, docs.Object);
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
        Assert.Empty(registry.GetAllScriptPaths());
        Assert.Empty(registry.GetLabelsInScript("foo"));
    }

    [Fact]
    public void ResolvesAfterDocumentAdded ()
    {
        meta.SetupNavigationCommands();
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
        Assert.Equal(["script1", "script2"], registry.GetAllScriptPaths());
        Assert.Equal(["label1", "label2"], registry.GetLabelsInScript("script1"));
        Assert.Equal(["label1"], registry.GetLabelsInScript("script2"));
    }

    [Fact]
    public void ResolvesAfterDocumentRemoved ()
    {
        meta.SetupNavigationCommands();
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
        Assert.Equal(["script2"], registry.GetAllScriptPaths());
        Assert.Empty(registry.GetLabelsInScript("script1"));
        Assert.Equal(["label1"], registry.GetLabelsInScript("script2"));
    }

    [Fact]
    public void ResolvesAfterDocumentChanged ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("script1.nani", "# label1", "# label2", "@goto script1.label1", "@goto .label1", "@goto script2");
        docs.SetupScript("script2.nani", "# label1", "# label1", "[goto script1]", "[goto script1]", "[goto script2.label1]");
        registry.HandleDocumentAdded("script1.nani");
        registry.HandleDocumentAdded("script2.nani");
        registry.HandleDocumentChanging("script1.nani", new(0, 4));
        registry.HandleDocumentChanging("script2.nani", new(1, 4));
        docs.SetupScript("script1.nani", "@goto script2", "@goto .label1", "@goto script2");
        docs.SetupScript("script2.nani", "# label1", "[goto script1]", "[goto script2.label1]");
        registry.HandleDocumentChanged("script1.nani", new(0, 2));
        registry.HandleDocumentChanged("script2.nani", new(1, 2));
        AssertLabelDoesntExist("script1", "label1");
        AssertLabelDoesntExist("script1", "label2");
        AssertLabelLocations("script2", "label1", new LineLocation("script2.nani", 0));
        AssertNavigatorLocations(new("script1"), new LineLocation("script2.nani", 1));
        AssertNavigatorLocations(new("script1", "label1"), new LineLocation("script1.nani", 1));
        AssertNavigatorLocations(new("script2"), new LineLocation("script1.nani", 0), new("script1.nani", 2));
        AssertNavigatorLocations(new("script2", "label1"), new LineLocation("script2.nani", 2));
        Assert.Equal(["script1", "script2"], registry.GetAllScriptPaths());
        Assert.Empty(registry.GetLabelsInScript("script1"));
        Assert.Equal(["label1"], registry.GetLabelsInScript("script2"));
    }

    [Fact]
    public void ResolvesChangesInSameDocument ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("script.nani", "# label", "@goto .label");
        registry.HandleDocumentAdded("script.nani");
        registry.HandleDocumentChanging("script.nani", new(1, 1));
        docs.SetupScript("script.nani", "# label", "@goto .foo");
        registry.HandleDocumentChanged("script.nani", new(1, 1));
        AssertNavigatorLocations(new("script", "foo"), new LineLocation("script.nani", 1));
        AssertNavigatorDoesntExist(new("script", "label"));
    }

    [Fact]
    public void UpdatesLocationsAfterLinesAdded ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("script.nani", "# label", "@goto .label");
        registry.HandleDocumentAdded("script.nani");
        registry.HandleDocumentChanging("script.nani", new(0, 3));
        docs.SetupScript("script.nani", "", "", "# label", "@goto .label");
        registry.HandleDocumentChanged("script.nani", new(0, 3));
        AssertLabelLocations("script", "label", new LineLocation("script.nani", 2));
        AssertNavigatorLocations(new("script", "label"), new LineLocation("script.nani", 3));
    }

    [Fact]
    public void UpdatesLocationsAfterLinesRemoved ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("script.nani", "", "", "# label", "@goto .label");
        registry.HandleDocumentAdded("script.nani");
        registry.HandleDocumentChanging("script.nani", new(0, 3));
        docs.SetupScript("script.nani", "# label", "@goto .label");
        registry.HandleDocumentChanged("script.nani", new(0, 1));
        AssertLabelLocations("script", "label", new LineLocation("script.nani", 0));
        AssertNavigatorLocations(new("script", "label"), new LineLocation("script.nani", 1));
    }

    private void AssertLabelLocations (string scriptPath, string label, params LineLocation[] locations)
    {
        Assert.True(registry.ScriptExist(scriptPath));
        Assert.True(registry.LabelExist(new(scriptPath, label)));
        Assert.Equal(locations, registry.GetLabelLocations(new(scriptPath, label)));
    }

    private void AssertLabelDoesntExist (string scriptPath, string label)
    {
        Assert.False(registry.LabelExist(new(scriptPath, label)));
        Assert.Empty(registry.GetLabelLocations(new(scriptPath, label)));
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
