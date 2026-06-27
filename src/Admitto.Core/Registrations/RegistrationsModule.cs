using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.CreateCoupon.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetCouponDetails.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.ListCoupons.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.RevokeCoupon.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetQRCode.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ExportRegistrationsCsv.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ArchiveTicketedEvent.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureReconfirmPolicy.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePublicEventLink.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateAdditionalDetailSchema.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventTimeZone.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetPublicTicketTypes.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails.AdminApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.JoinWaitlist.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.LeaveWaitlist.PublicApi;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.RemoveWaitlistEntry.AdminApi;

namespace Amolenk.Admitto.Core.Registrations;

public static class RegistrationsModule
{
    public const string Key = nameof(Registrations);
    public const string NamespacePrefix = "Amolenk.Admitto.Core.Registrations";

    public static RouteGroupBuilder MapRegistrationsAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamId:guid}/events")
            .WithTags("Admin - Events")
            .MapGetTicketedEvents();

        var eventGroup = group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}")
            .WithTags("Admin - Events");

        eventGroup
            .MapGetTicketedEventDetails()
            .MapUpdateTicketedEventDetails()
            .MapArchiveTicketedEvent()
            .MapConfigureRegistrationPolicy()
            .MapConfigureReconfirmPolicy()
            .MapUpdateTicketedEventTimeZone()
            .MapUpdateAdditionalDetailSchema();

        eventGroup.MapGroup("/registrations")
            .WithTags("Admin - Registrations")
            .MapAdminRegisterAttendee()
            .MapGetRegistrations()
            .MapExportRegistrationsCsv()
            .MapGetRegistrationDetails()
            .MapCancelRegistration()
            .MapChangeAttendeeTickets();

        eventGroup.MapGroup("/coupons")
            .WithTags("Admin - Coupons")
            .MapCreateCoupon()
            .MapListCoupons()
            .MapGetCouponDetails()
            .MapRevokeCoupon();

        eventGroup
            .MapGroup("/ticket-types")
            .WithTags("Admin - Ticket Types")
            .MapAddTicketType()
            .MapUpdateTicketType()
            .MapGetTicketTypes();

        eventGroup
            .MapGroup("/ticket-types/{ticketTypeId:guid}")
            .WithTags("Admin - Waitlist")
            .MapRemoveWaitlistEntry()
            .MapGetWaitlistDetails();

        return group;
    }

    public static RouteGroupBuilder MapRegistrationsPublicEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/events/{eventId:guid}")
            .WithTags("Public")
            .MapRequestOtp()
            .MapVerifyOtp()
            .MapRegisterAttendeeSelfService()
            .MapRegisterAttendeeWithCoupon()
            .MapGetQRCode()
            .MapSelfCancelRegistration()
            .MapSelfChangeTickets()
            .MapGetPublicTicketTypes()
            .MapJoinWaitlist()
            .MapLeaveWaitlist()
            .MapGetPublicCouponDetails();

        return group;
    }

    public static RouteGroupBuilder MapRegistrationsPublicEventLinkEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/e")
            .WithTags("Public Event Links")
            .MapResolvePublicEventLink();

        return group;
    }
}
