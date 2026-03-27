using System.Reflection;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Shared.Application.Http;

public sealed record OrganizationScope(
    string TeamSlug,
    Guid TeamId,
    string? EventSlug,
    Guid? EventId)
{
    public static async ValueTask<OrganizationScope?> BindAsync(
        HttpContext context,
        ParameterInfo _)
    {
        var resolver = context.RequestServices.GetService<IOrganizationScopeResolver>();
        if (resolver is null)
        {
            return null;
        }

        return await resolver.ResolveAsync(context.RequestAborted);
    }
}
