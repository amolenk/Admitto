using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Api.Auth;

public abstract record RouteScope
{
    private RouteScope() { }

    public sealed record Global : RouteScope;

    public sealed record Team(TeamId TeamId) : RouteScope;

    public sealed record Event(TeamId TeamId, TicketedEventId EventId) : RouteScope;
}
