namespace Naninovel.Language;

public interface IEndpointRegistry
{
    bool ScriptExist (string scriptName);
    bool LabelExist (string scriptName, string label);
    bool ScriptUsed (string scriptName);
    bool LabelUsed (string scriptName, string label);
}
