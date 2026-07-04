namespace Amolenk.Admitto.Core.Shared.Application.Http;

public sealed class EmailVerificationBearerTokenRequiredMetadata
{
    private EmailVerificationBearerTokenRequiredMetadata()
    {
    }

    public static EmailVerificationBearerTokenRequiredMetadata Instance { get; } = new();
}
