namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PublicApi;

public sealed record SelfChangeTicketsHttpRequest(Guid[]? TicketTypeIds, Guid? WaitlistCouponCode = null);
