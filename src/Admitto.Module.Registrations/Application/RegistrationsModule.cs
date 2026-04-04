using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon.PublicApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee.PublicApi;

namespace Amolenk.Admitto.Module.Registrations.Application;

public static class RegistrationsModule
{
    public const string Key = nameof(Registrations);

    public static RouteGroupBuilder MapRegistrationsAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}")
            .MapCreateCoupon()
            .MapListCoupons()
            .MapGetCouponDetails()
            .MapRevokeCoupon()
            .MapSetRegistrationPolicy();

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
