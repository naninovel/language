using System;

namespace Naninovel.Language;

public readonly struct EndpointLabel : IEquatable<EndpointLabel>
{
    public int Line { get; }
    public string Label { get; }

    public EndpointLabel (int line, string label)
    {
        Line = line;
        Label = label;
    }

    public static EndpointLabel CreateLineKey (int line)
    {
        return new(line, null!);
    }

    public static EndpointLabel CreateScriptKey ()
    {
        return new(-1, null!);
    }

    public static EndpointLabel CreateLabelKey (string label)
    {
        return new(int.MinValue, label);
    }

    public bool Equals (EndpointLabel other)
    {
        return (Line == int.MinValue || other.Line == int.MinValue || Line == other.Line) &&
               (Label == null! || other.Label == null! || Label == other.Label);
    }

    public override bool Equals (object? obj)
    {
        return obj is EndpointLabel other && Equals(other);
    }

    public override int GetHashCode ()
    {
        if (Line == int.MinValue) return HashCode.Combine(Label);
        if (Label == null) return HashCode.Combine(Line);
        return HashCode.Combine(Line, Label);
    }
}
