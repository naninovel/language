using System.Diagnostics.CodeAnalysis;
using static Naninovel.Bindings.Utilities;

[assembly: ExcludeFromCodeCoverage]

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
