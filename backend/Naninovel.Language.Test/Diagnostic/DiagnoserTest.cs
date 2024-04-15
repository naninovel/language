using System.Collections.Immutable;
using Moq;
using Naninovel.Metadata;

namespace Naninovel.Language.Test;

public abstract class DiagnoserTest
{
    protected const string DefaultUri = "this.nani";
    protected abstract Settings Settings { get; }
    protected DiagnosticHandler Handler { get; }
    protected Project Meta { get; } = new();
    protected Mock<IDocumentRegistry> Docs { get; } = new();
    protected Mock<IEndpointRegistry> Endpoints { get; } = new();
    protected Mock<IDiagnosticPublisher> Publisher { get; } = new();

    private readonly Dictionary<string, IReadOnlyList<Diagnostic>> published = new();

    protected DiagnoserTest ()
    {
        Handler = new(Docs.Object, Endpoints.Object, Publisher.Object);
        Docs.Setup(d => d.GetAllUris()).Returns(Array.Empty<string>());
        Endpoints.Setup(e => e.GetLabelLocations(It.Ref<QualifiedLabel>.IsAny)).Returns(ImmutableHashSet<LineLocation>.Empty);
        Endpoints.Setup(e => e.GetNavigatorLocations(It.Ref<QualifiedEndpoint>.IsAny)).Returns(ImmutableHashSet<LineLocation>.Empty);
        Publisher.Setup(p => p.PublishDiagnostics(It.IsAny<string>(), It.IsAny<IReadOnlyList<Diagnostic>>()))
            .Callback((string uri, IReadOnlyList<Diagnostic> diags) => published[uri] = diags);
    }

    [Fact]
    public void WhenEmptyDocumentResultIsEmpty ()
    {
        Assert.Empty(Diagnose(""));
    }

    [Fact] // Always start from beginning (not from range.Start) to handle nested.
    public void WhenChangingDocumentDiagnosesOnlyUpToLastChangedLine ()
    {
        var doc = new Mock<IDocument>();
        doc.Setup(d => d.LineCount).Returns(4);
        doc.SetupGet(d => d[It.IsAny<Index>()]).Returns(new DocumentFactory().CreateLine("@"));
        Docs.Setup(d => d.GetAllUris()).Returns(["foo.nani"]);
        Docs.Setup(d => d.Get("foo.nani")).Returns(doc.Object);
        Handler.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        Handler.HandleDocumentAdded("foo.nani");
        doc.Invocations.Clear();
        Handler.HandleDocumentChanged("foo.nani", new Range(new(1, 0), new(2, 0)));
        doc.VerifyGet(l => l[0], Times.Once);
        doc.VerifyGet(l => l[1], Times.Once);
        doc.VerifyGet(l => l[2], Times.Once);
        doc.VerifyGet(l => l[3], Times.Never);
    }

    protected IReadOnlyList<Diagnostic> GetDiagnostics (string uri = DefaultUri)
    {
        return published.TryGetValue(uri, out var diags) ? diags : Array.Empty<Diagnostic>();
    }

    protected void SetupHandler (Project meta = null)
    {
        Handler.HandleSettingsChanged(Settings);
        Handler.HandleMetadataChanged(meta ?? Meta);
    }

    protected IReadOnlyList<Diagnostic> Diagnose (params string[] lines)
    {
        SetupHandler();
        if (Docs.Object.GetAllUris().Contains(DefaultUri))
        {
            Handler.HandleDocumentChanging(DefaultUri, new(0, 0));
            Docs.SetupScript(DefaultUri, lines);
            Handler.HandleDocumentChanged(DefaultUri, new(0, 0));
        }
        else
        {
            Docs.SetupScript(DefaultUri, lines);
            Handler.HandleDocumentAdded(DefaultUri);
        }
        return GetDiagnostics();
    }
}
