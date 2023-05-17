using System;

namespace Naninovel.Language;

public readonly struct EndpointNavigator : IEquatable<EndpointNavigator>
{
    public int Line { get; }
    public string Script { get; }
    public string Label { get; }

    public EndpointNavigator (int line, string script, string label = "")
    {
        Line = line;
        Script = script;
        Label = label;
    }

    public static EndpointNavigator CreateLineKey (int line)
    {
        return new(line, null!, null!);
    }

    public static EndpointNavigator CreateScriptKey (string script)
    {
        return new(int.MinValue, script, null!);
    }

    public static EndpointNavigator CreateScriptAndLabelKey (string script, string label)
    {
        return new(int.MinValue, script, label);
    }

    public bool Equals (EndpointNavigator other)
    {
        return (Line == int.MinValue || other.Line == int.MinValue || Line == other.Line) &&
               (Script == null! || other.Script == null! || Script == other.Script) &&
               (Label == null! || other.Label == null! || Label == other.Label);
    }

    public override bool Equals (object? obj)
    {
        return obj is EndpointNavigator other && Equals(other);
    }

    public override int GetHashCode ()
    {
        if (Line != int.MinValue && Script != null && Label != null) return HashCode.Combine(Line, Script, Line);
        if (Script == null && Label == null) return HashCode.Combine(Line);
        if (Script != null && Label != null) return HashCode.Combine(Script, Line);
        return HashCode.Combine(Script);
    }
}
