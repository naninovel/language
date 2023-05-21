using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

    protected IReadOnlyList<Diagnostic> GetDiagnostics (string uri = DefaultUri)
    {
        return published.TryGetValue(uri, out var diags) ? diags : Array.Empty<Diagnostic>();
    }

    protected void SetupHandler (Project meta = null)
    {
        Handler.HandleSettingsChanged(Settings);
        Handler.HandleMetadataChanged(meta ?? Meta);
    }

    protected IReadOnlyList<Diagnostic> Diagnose (string line)
    {
        SetupHandler();
        if (Docs.Object.GetAllUris().Contains(DefaultUri))
        {
            Handler.HandleDocumentChanging(DefaultUri, new(0, 0));
            Docs.SetupScript(DefaultUri, line);
            Handler.HandleDocumentChanged(DefaultUri, new(0, 0));
        }
        else
        {
            Docs.SetupScript(DefaultUri, line);
            Handler.HandleDocumentAdded(DefaultUri);
        }
        return GetDiagnostics();
    }
}
