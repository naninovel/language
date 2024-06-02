global using static Naninovel.Language.Global;

namespace Naninovel.Language;

internal static class Global
{
    public static string ToScriptName (string uriOrName)
    {
        return Path.GetFileNameWithoutExtension(uriOrName);
    }
}
