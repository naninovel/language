namespace Naninovel.Language;

public interface IEndpointResolver
{
    bool TryResolve (Parsing.Command command, out string? script, out string? label);
}
