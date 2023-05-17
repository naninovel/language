using System.IO;

namespace Naninovel.Language;

internal static class Common
{
    public static string ToScriptName (string uriOrName)
    {
        return Path.GetFileNameWithoutExtension(uriOrName);
    }
}
