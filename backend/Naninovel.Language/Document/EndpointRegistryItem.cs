namespace Naninovel.Language;

internal readonly struct EndpointRegistryItem
{
    public string Label { get; }
    public int LineIndex { get; }

    public EndpointRegistryItem (string label, int lineIndex)
    {
        Label = label;
        LineIndex = lineIndex;
    }
}
