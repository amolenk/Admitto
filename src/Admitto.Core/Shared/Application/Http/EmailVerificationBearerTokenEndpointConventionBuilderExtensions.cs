namespace Amolenk.Admitto.Core.Shared.Application.Http;

public static class EmailVerificationBearerTokenEndpointConventionBuilderExtensions
{
    public static TBuilder RequireEmailVerificationBearerToken<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
            endpointBuilder.Metadata.Add(EmailVerificationBearerTokenRequiredMetadata.Instance));

        return builder;
    }
}
