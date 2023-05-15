namespace Naninovel.Language;

public readonly record struct LineRange(int Start, int End)
{
    public static implicit operator LineRange (Range r) => new(r.Start.Line, r.End.Line);
}
