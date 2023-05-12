using Naninovel.Metadata;

namespace Naninovel.Language;

internal class EndpointDiagnoser
{
    private readonly EndpointResolver resolver;

    public EndpointDiagnoser (MetadataProvider meta)
    {
        resolver = new(meta);
    }
}
