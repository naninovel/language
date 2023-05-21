using System.Diagnostics.CodeAnalysis;
using DotNetJS;
using static Naninovel.Bindings.Utilities;

[assembly: ExcludeFromCodeCoverage]
[assembly: JSNamespace(NamespacePattern, NamespaceReplacement)]

namespace Naninovel.Language;

public static class Program
{
    static Program () => ConfigureJson();

    public static void Main ()
    {
        // https://github.com/Elringus/DotNetJS/issues/23
        _ = typeof(Language).Assembly;
    }
}
