using Naninovel.Metadata;

namespace Naninovel.Language.Bindings;

internal class EndpointResolver : Naninovel.Metadata.EndpointResolver, IEndpointResolver
{
    public EndpointResolver (MetadataProvider provider) : base(provider) { }
}
