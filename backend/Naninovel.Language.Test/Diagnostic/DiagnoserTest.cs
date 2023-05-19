using System;
using System.Collections.Generic;
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
        Publisher.Setup(p => p.PublishDiagnostics(It.IsAny<string>(), It.IsAny<IReadOnlyList<Diagnostic>>()))
            .Callback((string uri, IReadOnlyList<Diagnostic> diags) => published[uri] = diags);
    }

    protected IReadOnlyList<Diagnostic> GetDiagnostics (string uri = DefaultUri)
    {
        return published.TryGetValue(uri, out var diags) ? diags : Array.Empty<Diagnostic>();
    }

    protected void SetupHandler (Project meta)
    {
        Handler.HandleSettingsChanged(Settings);
        Handler.HandleMetadataChanged(meta);
    }

    protected IReadOnlyList<Diagnostic> Diagnose (string line)
    {
        SetupHandler(Meta);
        var changing = Docs.Object.GetAllUris().Contains(DefaultUri);
        Docs.SetupScript(DefaultUri, line);
        if (changing) Handler.HandleDocumentChanged(DefaultUri, new(0, 0));
        else Handler.HandleDocumentAdded(DefaultUri);
        return GetDiagnostics();
    }
}
