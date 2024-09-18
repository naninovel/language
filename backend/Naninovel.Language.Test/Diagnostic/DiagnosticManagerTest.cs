using Moq;
using static Naninovel.Language.DiagnosticContext;

namespace Naninovel.Language.Test;

public class DiagnosticManagerTest
{
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IEndpointRegistry> endpoints = new();
    private readonly Mock<IDiagnosticPublisher> publisher = new();
    private readonly Mock<IDiagnoserFactory> factory = new();
    private readonly Dictionary<DiagnosticContext, Mock<IDiagnoser>> diagnosers = new();
    private readonly DiagnosticManager manager;

    public DiagnosticManagerTest ()
    {
        docs.Setup(d => d.GetAllUris()).Returns(Array.Empty<string>());
        factory.Setup(f => f.Create(It.IsAny<DiagnosticContext>()))
            .Callback((DiagnosticContext c) => diagnosers[c] = new Mock<IDiagnoser>())
            .Returns((DiagnosticContext c) => diagnosers[c].Object);
        manager = new(new MetadataMock(), docs.Object, endpoints.Object, publisher.Object, factory.Object);
    }

    [Fact]
    public void WhenSettingsChangedAddsDiagnosersOfEnabledContexts ()
    {
        manager.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        factory.Verify(f => f.Create(Syntax));
        factory.VerifyNoOtherCalls();
        manager.HandleSettingsChanged(new() { DiagnoseSemantics = true });
        factory.Verify(f => f.Create(Semantic));
        factory.VerifyNoOtherCalls();
        manager.HandleSettingsChanged(new() { DiagnoseNavigation = true });
        factory.Verify(f => f.Create(Navigation));
        factory.VerifyNoOtherCalls();
    }

    [Fact]
    public void WhenSettingsChangedAllDocumentsAreReDiagnosed ()
    {
        docs.SetupScript("this.nani", "#", "");
        manager.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        diagnosers[Syntax].Verify(d => d.HandleDocumentRemoved("this.nani"));
        diagnosers[Syntax].Verify(d => d.HandleDocumentAdded("this.nani"));
    }

    [Fact]
    public void WhenSettingsChangedClearsPreviouslyEnabledDiagnosers ()
    {
        docs.SetupScript("this.nani", "#");
        manager.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        diagnosers[Syntax].Invocations.Clear();
        manager.HandleSettingsChanged(new());
        diagnosers[Syntax].VerifyNoOtherCalls();
    }

    [Fact]
    public void DiagnosticsArePublishedOnEachChange ()
    {
        docs.SetupScript("this.nani", "#");
        manager.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        manager.HandleMetadataChanged(new());
        manager.HandleDocumentAdded("this.nani");
        manager.HandleDocumentRemoved("this.nani");
        manager.HandleDocumentChanged("this.nani", new());
        publisher.Verify(p => p.PublishDiagnostics("this.nani", It.IsAny<IReadOnlyList<Diagnostic>>()), Times.Exactly(5));
    }

    [Fact]
    public async Task WhenDebounceEnabledDelaysPublishing ()
    {
        docs.SetupScript("this.nani", "#");
        manager.HandleSettingsChanged(new() { DebounceDelay = 1, DiagnoseSyntax = true });
        publisher.VerifyNoOtherCalls();
        await Task.Delay(100);
        publisher.Verify(p => p.PublishDiagnostics("this.nani", It.IsAny<IReadOnlyList<Diagnostic>>()), Times.Once);
    }

    [Fact]
    public async Task MultiplePublishesAccumulatesUnderDebounce ()
    {
        docs.SetupScript("this.nani", "#");
        manager.HandleSettingsChanged(new() { DebounceDelay = 10, DiagnoseSyntax = true });
        await Task.Delay(2);
        manager.HandleDocumentChanged("this.nani", new(0, 0));
        await Task.Delay(2);
        manager.HandleDocumentChanged("this.nani", new(0, 0));
        await Task.Delay(2);
        publisher.VerifyNoOtherCalls();
        await Task.Delay(10);
        publisher.Verify(p => p.PublishDiagnostics("this.nani", It.IsAny<IReadOnlyList<Diagnostic>>()), Times.Once);
    }
}
