namespace Naninovel.Language.Test;

public class MultipleDiagnosersTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseSyntax = true, DiagnoseNavigation = true };

    [Fact]
    public void DoesntRemoveDiagnosticsOfOtherContexts ()
    {
        Docs.SetupScript("other.nani", "#");
        Meta.SetupNavigationCommands();
        Assert.Equal(2, Diagnose("@goto other.label p:").Count);
        Assert.Single(Diagnose("@goto other.label p:v"));
        Manager.HandleDocumentRemoved(DefaultUri);
        Manager.HandleDocumentRemoved("other.nani");
        Assert.Empty(GetDiagnostics());
    }
}
