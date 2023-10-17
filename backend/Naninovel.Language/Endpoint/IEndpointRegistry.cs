namespace Naninovel.Language;

public interface IEndpointRegistry
{
    bool ScriptExist (string scriptName);
    bool LabelExist (in QualifiedLabel label);
    bool NavigatorExist (in QualifiedEndpoint endpoint);
    IReadOnlySet<LineLocation> GetLabelLocations (in QualifiedLabel label);
    IReadOnlySet<LineLocation> GetNavigatorLocations (in QualifiedEndpoint endpoint);
}
