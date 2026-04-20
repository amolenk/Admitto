using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.RemoveCancellationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.GetReconfirmPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.RemoveReconfirmPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.SetReconfirmPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon.PublicApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee.PublicApi;
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
        var eventGroup = group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}");

        eventGroup
            .MapCreateCoupon()
            .MapListCoupons()
            .MapGetCouponDetails()
            .MapRevokeCoupon()
            .MapSetRegistrationPolicy()
            .MapGetRegistrationOpenStatus()
            .MapSetCancellationPolicy()
            .MapRemoveCancellationPolicy()
            .MapGetCancellationPolicy()
            .MapSetReconfirmPolicy()
            .MapRemoveReconfirmPolicy()
            .MapGetReconfirmPolicy();

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
