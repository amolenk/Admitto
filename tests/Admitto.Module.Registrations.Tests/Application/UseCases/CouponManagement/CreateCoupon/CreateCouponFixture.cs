using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed class CreateCouponFixture
{
    private bool _eventNotActive;
    private bool _hasCancelledTicketType;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";
    public string CancelledTicketTypeSlug { get; } = "vip-pass";
    public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();

    private CreateCouponFixture()
    {
    }

    public static CreateCouponFixture HappyFlow() => new();

    public static CreateCouponFixture CancelledEvent() => new()
    {
        _eventNotActive = true
    };

    public static CreateCouponFixture WithCancelledTicketType() => new()
    {
        _hasCancelledTicketType = true
    };

    public ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var ticketTypes = new List<TicketTypeDto>
        {
            new() { Slug = TicketTypeSlug, Name = "General Admission", IsCancelled = false }
        };

        if (_hasCancelledTicketType)
        {
            ticketTypes.Add(new TicketTypeDto
            {
                Slug = CancelledTicketTypeSlug,
                Name = "VIP Pass",
                IsCancelled = true
            });
        }

        OrganizationFacade
            .GetTicketTypesAsync(EventId.Value, Arg.Any<CancellationToken>())
            .Returns(ticketTypes.ToArray());

        OrganizationFacade
            .IsEventActiveAsync(EventId.Value, Arg.Any<CancellationToken>())
            .Returns(!_eventNotActive);

        return ValueTask.CompletedTask;
    }
}
