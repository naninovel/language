using System.Collections.Immutable;
using static Naninovel.Language.QualifiedEndpoint;

namespace Naninovel.Language.Test;

public class NavigationDiagnoserTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseNavigation = true };

    [Fact]
    public void WhenUnusedLabelWarningIsDiagnosed ()
    {
        Endpoints.Setup(d => d.NavigatorExist(new("this", "label"))).Returns(false);
        var diags = Diagnose("# label");
        Assert.Single(diags);
        Assert.Equal(new(new(0, 2), new(0, 7)), diags[0].Range);
        Assert.Equal(DiagnosticSeverity.Information, diags[0].Severity);
        Assert.Equal("Unused label.", diags[0].Message);
        Assert.Equal([DiagnosticTag.Unnecessary], diags[0].Tags);
    }

    [Fact]
    public void WhenLabelIsUsedWarningIsNotDiagnosed ()
    {
        Endpoints.Setup(d => d.NavigatorExist(new("this", "label"))).Returns(true);
        Assert.Empty(Diagnose("# label"));
    }

    [Fact]
    public void WhenUnknownEndpointScriptWarningIsDiagnosed ()
    {
        Meta.SetupNavigationCommands();
        Endpoints.Setup(d => d.ScriptExist("other")).Returns(false);
        var diags = Diagnose("@goto other");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 11)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointWarningIsDiagnosed ()
    {
        Meta.SetupNavigationCommands();
        Endpoints.Setup(d => d.LabelExist(new("this", "label"))).Returns(false);
        var diags = Diagnose("@goto .label");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 12)), DiagnosticSeverity.Warning,
            "Unknown endpoint: .label."), diags[0]);
    }

    [Fact]
    public void WhenKnownEndpointWarningIsNotDiagnosed ()
    {
        Meta.SetupNavigationCommands();
        Docs.SetupScript("other.nani", "# label");
        Endpoints.Setup(d => d.ScriptExist("this")).Returns(true);
        Endpoints.Setup(d => d.LabelExist(new("this", "label"))).Returns(true);
        Endpoints.Setup(d => d.ScriptExist("other")).Returns(true);
        Endpoints.Setup(d => d.LabelExist(new("other", "label"))).Returns(true);
        Assert.Empty(Diagnose("@goto this"));
        Assert.Empty(Diagnose("@goto .label"));
        Assert.Empty(Diagnose("@goto this.label"));
        Assert.Empty(Diagnose("@goto other"));
        Assert.Empty(Diagnose("@goto other.label"));
    }

    [Fact]
    public void DiagnosticsAreClearedWhenCorrected ()
    {
        Meta.SetupNavigationCommands();
        Endpoints.Setup(d => d.LabelExist(new("this", "bar"))).Returns(true);
        Assert.NotEmpty(Diagnose("@goto .foo"));
        Assert.Empty(Diagnose("@goto .bar"));
    }

    [Fact]
    public void DiagnosticsAreClearedWhenDocumentAdded ()
    {
        Meta.SetupNavigationCommands();
        Endpoints.Setup(d => d.LabelExist(new("other", "foo"))).Returns(false);
        Assert.NotEmpty(Diagnose("@goto other.foo"));

        Docs.SetupScript("other.nani", "# foo");
        Endpoints.Setup(d => d.LabelExist(new("other", "foo"))).Returns(true);
        Handler.HandleDocumentAdded("other.nani");
        Assert.Empty(GetDiagnostics());
    }

    [Fact]
    public void DiagnosticsAreAddedWhenDocumentRemoved ()
    {
        Meta.SetupNavigationCommands();
        SetupHandler();
        Docs.SetupScript("script1.nani", "@goto script2.foo", "@goto script2");
        Docs.SetupScript("script2.nani", "# foo");
        Endpoints.Setup(d => d.ScriptExist("script2")).Returns(true);
        Endpoints.Setup(d => d.LabelExist(new("script2", "foo"))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("script2", NoLabel))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("script2", "foo"))).Returns(true);
        Endpoints.Setup(d => d.GetLabelLocations(new("script2", "foo"))).Returns(new HashSet<LineLocation> { new("script2.nani", 0) });
        Endpoints.Setup(d => d.GetNavigatorLocations(new("script2", "foo"))).Returns(new HashSet<LineLocation> { new("script1.nani", 0) });
        Endpoints.Setup(d => d.GetNavigatorLocations(new("script2", NoLabel))).Returns(new HashSet<LineLocation> { new("script1.nani", 1) });
        Handler.HandleDocumentAdded("script1.nani");
        Handler.HandleDocumentAdded("script2.nani");
        Assert.Empty(GetDiagnostics("script1.nani"));
        Assert.Empty(GetDiagnostics("script2.nani"));

        Endpoints.Setup(d => d.ScriptExist("script2")).Returns(false);
        Endpoints.Setup(d => d.LabelExist(new("script2", "foo"))).Returns(false);
        Endpoints.Setup(d => d.GetLabelLocations(new("script2", "foo"))).Returns(ImmutableHashSet<LineLocation>.Empty);
        Handler.HandleDocumentRemoved("script2.nani");
        Assert.Equal(2, GetDiagnostics("script1.nani").Count);
        Assert.Equal(new(new(new(1, 6), new(1, 13)), DiagnosticSeverity.Warning,
            "Unknown endpoint: script2."), GetDiagnostics("script1.nani")[0]);
        Assert.Equal(new(new(new(0, 6), new(0, 17)), DiagnosticSeverity.Warning,
            "Unknown endpoint: script2.foo."), GetDiagnostics("script1.nani")[1]);
    }

    [Fact]
    public void UnusedLabelIsDetectedAfterChange ()
    {
        Meta.SetupNavigationCommands();
        SetupHandler();
        Docs.SetupScript("foo.nani", "[goto bar.label]");
        Docs.SetupScript("bar.nani", "# label");
        Endpoints.Setup(d => d.LabelExist(new("bar", "label"))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("bar", "label"))).Returns(true);
        Endpoints.Setup(d => d.GetLabelLocations(new("bar", "label"))).Returns(new HashSet<LineLocation> { new("bar.nani", 0) });
        Endpoints.Setup(d => d.GetNavigatorLocations(new("bar", "label"))).Returns(new HashSet<LineLocation> { new("foo.nani", 0) });
        Handler.HandleDocumentAdded("foo.nani");
        Handler.HandleDocumentAdded("bar.nani");
        Assert.Empty(GetDiagnostics("foo.nani"));
        Assert.Empty(GetDiagnostics("bar.nani"));

        Endpoints.Setup(d => d.NavigatorExist(new("bar", "label"))).Returns(false);
        Endpoints.Setup(d => d.GetNavigatorLocations(new("bar", "label"))).Returns(ImmutableHashSet<LineLocation>.Empty);
        Handler.HandleDocumentChanging("foo.nani", new(0, 0));
        Docs.SetupScript("foo.nani", "@goto bar.baz");
        Handler.HandleDocumentChanged("foo.nani", new(0, 0));
        Assert.Contains(GetDiagnostics("bar.nani"), d => d.Message == "Unused label.");
    }

    [Fact]
    public void UnknownEndpointIsDetectedAfterChange ()
    {
        Meta.SetupNavigationCommands();
        SetupHandler();
        Docs.SetupScript("foo.nani", "@goto bar.label");
        Docs.SetupScript("bar.nani", "# label");
        Endpoints.Setup(d => d.LabelExist(new("bar", "label"))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("bar", "label"))).Returns(true);
        Endpoints.Setup(d => d.GetLabelLocations(new("bar", "label"))).Returns(new HashSet<LineLocation> { new("bar.nani", 0) });
        Endpoints.Setup(d => d.GetNavigatorLocations(new("bar", "label"))).Returns(new HashSet<LineLocation> { new("foo.nani", 0) });
        Handler.HandleDocumentAdded("foo.nani");
        Handler.HandleDocumentAdded("bar.nani");
        Assert.Empty(GetDiagnostics("foo.nani"));
        Assert.Empty(GetDiagnostics("bar.nani"));

        Endpoints.Setup(d => d.LabelExist(new("bar", "label"))).Returns(false);
        Endpoints.Setup(d => d.GetLabelLocations(new("bar", "label"))).Returns(ImmutableHashSet<LineLocation>.Empty);
        Handler.HandleDocumentChanging("bar.nani", new(0, 0));
        Docs.SetupScript("bar.nani", "# baz");
        Handler.HandleDocumentChanged("bar.nani", new(0, 0));
        Assert.Contains(GetDiagnostics("foo.nani"), d => d.Message == "Unknown endpoint: bar.label.");
    }

    [Fact]
    public void UnusedLabelIsClearedAfterChangeInSameScript ()
    {
        Meta.SetupNavigationCommands();
        SetupHandler();
        Docs.SetupScript("script.nani", "# label", "@goto .foo");
        Endpoints.Setup(d => d.LabelExist(new("script", "label"))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("script", "foo"))).Returns(true);
        Endpoints.Setup(d => d.GetLabelLocations(new("script", "label"))).Returns(new HashSet<LineLocation> { new("script.nani", 0) });
        Endpoints.Setup(d => d.GetNavigatorLocations(new("script", "foo"))).Returns(new HashSet<LineLocation> { new("script.nani", 1) });
        Handler.HandleDocumentAdded("script.nani");
        Assert.Contains(GetDiagnostics("script.nani"), d => d.Message == "Unknown endpoint: .foo.");

        Endpoints.Setup(d => d.NavigatorExist(new("script", "foo"))).Returns(false);
        Endpoints.Setup(d => d.NavigatorExist(new("script", "label"))).Returns(true);
        Endpoints.Setup(d => d.GetNavigatorLocations(new("script", "foo"))).Returns(ImmutableHashSet<LineLocation>.Empty);
        Endpoints.Setup(d => d.GetNavigatorLocations(new("script", "label"))).Returns(new HashSet<LineLocation> { new("script.nani", 1) });
        Handler.HandleDocumentChanging("script.nani", new(1, 1));
        Docs.SetupScript("script.nani", "# label", "@goto .label");
        Handler.HandleDocumentChanged("script.nani", new(1, 1));
        Assert.Empty(GetDiagnostics("script.nani"));
    }

    [Fact]
    public void UnknownEndpointIsClearedAfterChangeInSameScript ()
    {
        Meta.SetupNavigationCommands();
        SetupHandler();
        Docs.SetupScript("script.nani", "# foo", "@goto .label");
        Endpoints.Setup(d => d.LabelExist(new("script", "foo"))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("script", "label"))).Returns(true);
        Endpoints.Setup(d => d.GetLabelLocations(new("script", "foo"))).Returns(new HashSet<LineLocation> { new("script.nani", 0) });
        Endpoints.Setup(d => d.GetNavigatorLocations(new("script", "label"))).Returns(new HashSet<LineLocation> { new("script.nani", 1) });
        Handler.HandleDocumentAdded("script.nani");
        Assert.Contains(GetDiagnostics("script.nani"), d => d.Message == "Unused label.");

        Endpoints.Setup(d => d.LabelExist(new("script", "foo"))).Returns(false);
        Endpoints.Setup(d => d.LabelExist(new("script", "label"))).Returns(true);
        Endpoints.Setup(d => d.GetLabelLocations(new("script", "foo"))).Returns(ImmutableHashSet<LineLocation>.Empty);
        Endpoints.Setup(d => d.GetLabelLocations(new("script", "label"))).Returns(new HashSet<LineLocation> { new("script.nani", 0) });
        Handler.HandleDocumentChanging("script.nani", new(0, 0));
        Docs.SetupScript("script.nani", "# label", "@goto .label");
        Handler.HandleDocumentChanged("script.nani", new(0, 0));
        Assert.Empty(GetDiagnostics("script.nani"));
    }

    [Fact]
    public void CanDiagnoseWhenRemovingLines ()
    {
        Meta.SetupNavigationCommands();
        SetupHandler();
        Docs.SetupScript("foo.nani", "# bar", "@goto .bar", "[@goto .bar]");
        Endpoints.Setup(d => d.LabelExist(new("foo", "bar"))).Returns(true);
        Endpoints.Setup(d => d.NavigatorExist(new("foo", "bar"))).Returns(true);
        Handler.HandleDocumentAdded("foo.nani");
        Assert.Empty(GetDiagnostics("foo.nani"));

        Endpoints.Setup(d => d.LabelExist(new("foo", "bar"))).Returns(false);
        Handler.HandleDocumentChanging("foo.nani", new(0, 2));
        Docs.SetupScript("foo.nani", "@goto .bar");
        Handler.HandleDocumentChanged("foo.nani", new(0, 0));
        Assert.NotEmpty(GetDiagnostics("foo.nani"));
    }
}
