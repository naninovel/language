namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#range

public readonly record struct Range(Position Start, Position End)
{
    public static Range Empty { get; } = new(Position.Empty, Position.Empty);
}
