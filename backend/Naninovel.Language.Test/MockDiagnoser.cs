using System.Collections.Generic;

namespace Naninovel.Language.Test;

public class MockDiagnoser : IDiagnoser
{
    public List<string> DiagnoseRequests { get; } = new();

    public void Diagnose (string documentUri)
    {
        DiagnoseRequests.Add(documentUri);
    }
}
