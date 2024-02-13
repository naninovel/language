namespace Naninovel.Language;

/// <summary>
/// Provides access to the known endpoints and navigators in the solution.
/// </summary>
/// <remarks>
/// Endpoint is a scenario script location; it can either be a script URI or
/// script URI + label inside that script. When label is missing, it is assumed
/// the location is pointing to the start of the script.
/// Navigator is a command, that navigates to an endpoint, eg @goto or @gosub.
/// </remarks>
public interface IEndpointRegistry
{
    bool ScriptExist (string scriptName);
    bool LabelExist (in QualifiedLabel label);
    bool NavigatorExist (in QualifiedEndpoint endpoint);
    IReadOnlySet<LineLocation> GetLabelLocations (in QualifiedLabel label);
    IReadOnlySet<LineLocation> GetNavigatorLocations (in QualifiedEndpoint endpoint);
    IReadOnlySet<string> GetAllScriptNames ();
    IReadOnlySet<string> GetLabelsInScript (string scriptName);
}
