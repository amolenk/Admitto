using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.Coupon;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

namespace Amolenk.Admitto.Module.Registrations.Application;

public static class RegistrationsModule
{
    public const string Key = nameof(Registrations);

    public static RouteGroupBuilder MapRegistrationsAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamSlug}/events")
            .MapGetTicketedEvents();

        var eventGroup = group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}");

        eventGroup
            .MapGetTicketedEventDetails()
            .MapUpdateTicketedEventDetails()
            .MapCancelTicketedEvent()
            .MapArchiveTicketedEvent()
            .MapConfigureRegistrationPolicy()
            .MapConfigureCancellationPolicy()
            .MapConfigureReconfirmPolicy()
            .MapUpdateAdditionalDetailSchema()
            .MapAdminRegisterAttendee()
            .MapGetRegistrations()
            .MapCreateCoupon()
            .MapListCoupons()
            .MapGetCouponDetails()
            .MapRevokeCoupon();

        eventGroup
            .MapGroup("/ticket-types")
            .MapAddTicketType()
            .MapUpdateTicketType()
            .MapCancelTicketType()
            .MapGetTicketTypes();

        return group;
    }

    public static RouteGroupBuilder MapRegistrationsPublicEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}")
            .MapSelfRegisterAttendee()
            .MapRegisterWithCoupon();

        return group;
    }
}
