using System.Reflection;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Shared.Application.Http;

public sealed record OrganizationScope(
    string TeamSlug,
    TeamId TeamId,
    string? EventSlug,
    TicketedEventId? EventId)
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
