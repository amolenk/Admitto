using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class RegisterAttendeeOutboxTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Successful registration puts AttendeeRegisteredIntegrationEvent in outbox
    [TestMethod]
    public async ValueTask SC001_RegisterAttendee_Success_OutboxesAttendeeRegistered()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = new RegisterAttendeeCommand(
            fixture.EventId,
            EmailAddress.From("dave@example.com"),
            FirstName.From("Dave"),
            LastName.From("Doe"),
            [fixture.TicketTypeSlug],
            RegistrationMode.AdminAdd,
            CouponCode: null,
            EmailVerificationToken: null);

        var sut = new RegisterAttendeeHandler(
            Environment.Database.Context,
            TimeProvider.System,
            new StubEmailVerificationTokenValidator());

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var outbox = await ctx.OutboxMessages
                .Where(m => m.Type == "integration.registrations.attendee-registered-integration-event")
                .ToListAsync(testContext.CancellationToken);

            outbox.ShouldHaveSingleItem();
        });
    }
}
