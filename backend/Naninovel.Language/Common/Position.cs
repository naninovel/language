namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#position

public readonly record struct Position (int Line, int Character)
{
    public static Position Empty { get; } = new(0, 0);
}
