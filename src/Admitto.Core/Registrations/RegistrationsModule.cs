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
            .MapGetTicketedEvents();

        var eventGroup = group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}");

        eventGroup
            .MapGetTicketedEventDetails()
            .MapUpdateTicketedEventDetails()
            .MapArchiveTicketedEvent()
            .MapConfigureRegistrationPolicy()
            .MapConfigureReconfirmPolicy()
            .MapUpdateTicketedEventTimeZone()
            .MapUpdateAdditionalDetailSchema()
            .MapAdminRegisterAttendee()
            .MapGetRegistrations()
            .MapGetRegistrationDetails()
            .MapCancelRegistration()
            .MapChangeAttendeeTickets()
            .MapCreateCoupon()
            .MapListCoupons()
            .MapGetCouponDetails()
            .MapRevokeCoupon();

        eventGroup
            .MapGroup("/ticket-types")
            .MapAddTicketType()
            .MapUpdateTicketType()
            .MapGetTicketTypes();

        eventGroup
            .MapGroup("/ticket-types/{ticketTypeId:guid}")
            .MapRemoveWaitlistEntry()
            .MapGetWaitlistDetails();

        return group;
    }

    public static RouteGroupBuilder MapRegistrationsPublicEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}")
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
}
