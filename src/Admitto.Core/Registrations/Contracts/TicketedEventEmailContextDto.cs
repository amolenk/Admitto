namespace Amolenk.Admitto.Core.Registrations.Contracts;

public sealed record TicketedEventEmailContextDto(
    string Name,
    string WebsiteUrl,
    string RegisterLink,
    string QRCodeLink,
    string? FirstName = null,
    string? LastName = null);
