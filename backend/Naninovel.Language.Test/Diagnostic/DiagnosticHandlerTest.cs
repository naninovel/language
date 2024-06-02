using Moq;
using static Naninovel.Language.DiagnosticContext;

namespace Naninovel.Language.Test;

public class DiagnosticHandlerTest
{
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IEndpointRegistry> endpoints = new();
    private readonly Mock<IDiagnosticPublisher> publisher = new();
    private readonly Mock<IDiagnoserFactory> factory = new();
    private readonly Dictionary<DiagnosticContext, Mock<IDiagnoser>> diagnosers = new();
    private readonly DiagnosticHandler handler;

    public DiagnosticHandlerTest ()
    {
        docs.Setup(d => d.GetAllUris()).Returns(Array.Empty<string>());
        factory.Setup(f => f.Create(It.IsAny<DiagnosticContext>()))
            .Callback((DiagnosticContext c) => diagnosers[c] = new Mock<IDiagnoser>())
            .Returns((DiagnosticContext c) => diagnosers[c].Object);
        handler = new(new MetadataMock(), docs.Object, endpoints.Object, publisher.Object, factory.Object);
    }

    [Fact]
    public void WhenSettingsChangedAddsDiagnosersOfEnabledContexts ()
    {
        handler.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        factory.Verify(f => f.Create(Syntax));
        factory.VerifyNoOtherCalls();
        handler.HandleSettingsChanged(new() { DiagnoseSemantics = true });
        factory.Verify(f => f.Create(Semantic));
        factory.VerifyNoOtherCalls();
        handler.HandleSettingsChanged(new() { DiagnoseNavigation = true });
        factory.Verify(f => f.Create(Navigation));
        factory.VerifyNoOtherCalls();
    }

    [Fact]
    public void WhenSettingsChangedAllDocumentsAreReDiagnosed ()
    {
        docs.SetupScript("this.nani", "#", "");
        handler.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        diagnosers[Syntax].Verify(d => d.HandleDocumentRemoved("this.nani"));
        diagnosers[Syntax].Verify(d => d.HandleDocumentAdded("this.nani"));
    }

    [Fact]
    public void WhenSettingsChangedClearsPreviouslyEnabledDiagnosers ()
    {
        docs.SetupScript("this.nani", "#");
        handler.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        diagnosers[Syntax].Invocations.Clear();
        handler.HandleSettingsChanged(new());
        diagnosers[Syntax].VerifyNoOtherCalls();
    }

    [Fact]
    public void DiagnosticsArePublishedOnEachChange ()
    {
        docs.SetupScript("this.nani", "#");
        handler.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        handler.HandleMetadataChanged(new());
        handler.HandleDocumentAdded("this.nani");
        handler.HandleDocumentRemoved("this.nani");
        handler.HandleDocumentChanged("this.nani", new());
        publisher.Verify(p => p.PublishDiagnostics("this.nani", It.IsAny<IReadOnlyList<Diagnostic>>()), Times.Exactly(5));
    }

    [Fact]
    public async Task WhenDebounceEnabledDelaysPublishing ()
    {
        docs.SetupScript("this.nani", "#");
        handler.HandleSettingsChanged(new() { DebounceDelay = 1, DiagnoseSyntax = true });
        publisher.VerifyNoOtherCalls();
        await Task.Delay(2);
        publisher.Verify(p => p.PublishDiagnostics("this.nani", It.IsAny<IReadOnlyList<Diagnostic>>()), Times.Once);
    }

    [Fact]
    public async Task MultiplePublishesAccumulatesUnderDebounce ()
    {
        docs.SetupScript("this.nani", "#");
        handler.HandleSettingsChanged(new() { DebounceDelay = 10, DiagnoseSyntax = true });
        await Task.Delay(2);
        handler.HandleDocumentChanged("this.nani", new(0, 0));
        await Task.Delay(2);
        handler.HandleDocumentChanged("this.nani", new(0, 0));
        await Task.Delay(2);
        publisher.VerifyNoOtherCalls();
        await Task.Delay(10);
        publisher.Verify(p => p.PublishDiagnostics("this.nani", It.IsAny<IReadOnlyList<Diagnostic>>()), Times.Once);
    }
}
