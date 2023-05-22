using Xunit;

namespace Naninovel.Language.Test;

public class MultipleDiagnosersTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseSyntax = true, DiagnoseNavigation = true };

    [Fact]
    public void DoesntRemoveDiagnosticsOfOtherContexts ()
    {
        Docs.SetupScript("other.nani", "#");
        Meta.SetupCommandWithEndpoint("goto");
        Assert.Equal(2, Diagnose("@goto other.label p:").Count);
        Assert.Equal(1, Diagnose("@goto other.label p:v").Count);
        Handler.HandleDocumentRemoved(DefaultUri);
        Handler.HandleDocumentRemoved("other.nani");
        Assert.Empty(GetDiagnostics());
    }
}
