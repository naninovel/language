namespace Naninovel.Language;

public readonly record struct LineRange(int Start, int End)
{
    public static implicit operator LineRange (in Range r) => new(r.Start.Line, r.End.Line);

    public bool Contains (int index) => index >= Start && index <= End;
}
