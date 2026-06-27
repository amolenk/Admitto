namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PartnerApi;

public sealed record SelfChangeTicketsHttpRequest(Guid[]? TicketTypeIds, Guid? WaitlistCouponCode = null);
