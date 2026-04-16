namespace Amolenk.Admitto.Module.Shared.Application.Http;

public sealed record OrganizationScope(
    string TeamSlug,
    Guid TeamId,
    string? EventSlug,
    Guid? EventId);
