using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class CreateCouponHandlerTests
{
    // SC-006: Rejected — cancelled event
    [TestMethod]
    public async Task SC006_CreateCoupon_CancelledEvent_ThrowsEventNotActiveError()
    {
        // Arrange
        var facade = new StubOrganizationFacade(isEventActive: false);
        var handler = new CreateCouponHandler(facade, writeStore: null!);

        var command = new CreateCouponCommand(
            TicketedEventId.New(),
            EmailAddress.From("speaker@example.com"),
            [TicketTypeId.New()],
            DateTimeOffset.UtcNow.AddDays(30),
            BypassRegistrationWindow: false);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await handler.HandleAsync(command, CancellationToken.None); });

        // Assert
        result.Error.ShouldMatch(CreateCouponHandler.Errors.EventNotActive);
    }

    private sealed class StubOrganizationFacade(bool isEventActive = true) : IOrganizationFacade
    {
        public ValueTask<Guid> GetTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Guid> GetTicketedEventIdAsync(Guid teamId, string eventSlug, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<TicketTypeDto[]> GetTicketTypesAsync(Guid eventId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Array.Empty<TicketTypeDto>());

        public ValueTask<bool> IsEventActiveAsync(Guid eventId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(isEventActive);
    }
}
