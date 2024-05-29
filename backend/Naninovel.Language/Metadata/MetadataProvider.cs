using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class MetadataProvider : IMetadata, IMetadataObserver
{
    public IReadOnlyCollection<Actor> Actors => provider.Actors;
    public IReadOnlyCollection<Metadata.Command> Commands => provider.Commands;
    public IReadOnlyCollection<Constant> Constants => provider.Constants;
    public IReadOnlyCollection<Resource> Resources => provider.Resources;
    public IReadOnlyCollection<string> Variables => provider.Variables;
    public IReadOnlyCollection<Function> Functions => provider.Functions;
    public ISyntax Syntax => provider.Syntax;

    private readonly Naninovel.Metadata.MetadataProvider provider = new();

    public void HandleMetadataChanged (Project project)
    {
        provider.Update(project);
    }

    public Metadata.Command? FindCommand (string aliasOrId)
    {
        return provider.FindCommand(aliasOrId);
    }

    public Metadata.Parameter? FindParameter (string commandAliasOrId, string paramAliasOrId)
    {
        return provider.FindParameter(commandAliasOrId, paramAliasOrId);
    }

    public Function? FindFunction (string name)
    {
        return provider.FindFunction(name);
    }

    public FunctionParameter? FindFunctionParameter (string functionName, string paramName)
    {
        return provider.FindFunctionParameter(functionName, paramName);
    }

    public FunctionParameter? FindFunctionParameter (string functionName, int index)
    {
        return provider.FindFunctionParameter(functionName, index);
    }
}
