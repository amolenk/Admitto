using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;

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
            .MapRevokeCoupon();
        
        return group;
    }
}