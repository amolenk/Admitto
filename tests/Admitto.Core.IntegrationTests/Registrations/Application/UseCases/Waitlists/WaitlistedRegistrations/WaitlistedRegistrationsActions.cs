using Amolenk.Admitto.Core.Registrations.Application.Jobs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistedRegistrations;

/// <summary>
/// Runs each action on its own <see cref="DispatchingRegistrationsContext"/> at <see cref="Clock"/>'s time, so the
/// cascade through domain event handlers (e.g. a cancellation offering the freed seat) happens in the same save, as in
/// production. Each action returns the integration events published by that save.
/// </summary>
internal sealed class WaitlistedRegistrationsActions(
    WaitlistedRegistrationsFixture fixture,
    IntegrationTestEnvironment environment,
    CancellationToken cancellationToken)
{
    public FakeTimeProvider Clock { get; } = new(fixture.Start);

    public ValueTask<IReadOnlyList<IIntegrationEvent>> CancelAsync(RegistrationId registrationId) =>
        RunAsync(context => new CancelRegistrationHandler(context, Clock).HandleAsync(
            new CancelRegistrationCommand(
                registrationId.Value, fixture.EventId.Value, fixture.TeamId.Value, CancellationReason.AttendeeRequest),
            cancellationToken));

    public ValueTask<IReadOnlyList<IIntegrationEvent>> RegisterWithCouponAsync(
        EmailAddress email,
        Guid couponCode,
        params TicketTypeId[] ticketTypeIds) =>
        RunAsync(async context => await new RegisterAttendeeHandler(context, Clock).HandleAsync(
            new RegisterAttendeeCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                email.Value,
                "Coupon",
                 "Holder",
                 ticketTypeIds.Select(id => id.Value).ToArray(),
                 [],
                 CouponCode: couponCode),
            cancellationToken));

    public ValueTask<IReadOnlyList<IIntegrationEvent>> AdminRegisterAsync(
        EmailAddress email,
        params TicketTypeId[] ticketTypeIds) =>
        RunAsync(async context => await new AdminRegisterAttendeeHandler(context, Clock).HandleAsync(
            new AdminRegisterAttendeeCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                email.Value,
                "Admin",
                "Guest",
                ticketTypeIds.Select(id => id.Value).ToArray()),
            cancellationToken));

    public ValueTask<IReadOnlyList<IIntegrationEvent>> UpdateAsync(
        RegistrationId registrationId,
        TicketTypeId[] registerTicketTypeIds,
        TicketTypeId[] waitlistTicketTypeIds,
        Guid? couponCode = null) =>
        RunAsync(context => new UpdatePartnerRegistrationHandler(context, Clock).HandleAsync(
            new UpdatePartnerRegistrationCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                registrationId.Value,
                "Waiting",
                "Attendee",
                registerTicketTypeIds.Select(id => id.Value).ToList(),
                waitlistTicketTypeIds.Select(id => id.Value).ToList(),
                CouponCode: couponCode),
            cancellationToken));

    /// <summary>
    /// Promotes the attendee at the given Conference Pass queue position as a VIP and returns their coupon code.
    /// </summary>
    public async ValueTask<Guid> PromoteAsync(int position)
    {
        var waitlist = await environment.RegistrationsDatabase.Context.Waitlists.AsNoTracking()
            .SingleAsync(w => w.Id == fixture.ConferencePassId, cancellationToken);
        var entry = waitlist.Entries.Single(e => e.Status == WaitlistEntryStatus.Active && e.Position == position);

        await RunAsync(async context => await new PromoteWaitlistEntryHandler(context, Clock).HandleAsync(
            new PromoteWaitlistEntryCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.ConferencePassId.Value, entry.Id.Value),
            cancellationToken));

        return await GetCouponCodeAsync(entry.Email);
    }

    public ValueTask<IReadOnlyList<IIntegrationEvent>> UpdatePublicCapacityAsync(int publicCapacity) =>
        RunAsync(context => new UpdateTicketTypeHandler(context).HandleAsync(
            new UpdateTicketTypeCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.ConferencePassId.Value, null, publicCapacity),
            cancellationToken));

    /// <summary>
    /// Removes the Conference Pass's capacity limit, which switches its waitlist off (as the Admin UI sends it).
    /// </summary>
    public ValueTask<IReadOnlyList<IIntegrationEvent>> RemoveCapacityLimitAsync() =>
        RunAsync(context => new UpdateTicketTypeHandler(context).HandleAsync(
            new UpdateTicketTypeCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                fixture.ConferencePassId.Value,
                null,
                PublicCapacity: null,
                WaitlistEnabled: false),
            cancellationToken));

    public ValueTask<IReadOnlyList<IIntegrationEvent>> MoveClosesAtAsync(DateTimeOffset closesAt) =>
        RunAsync(context => new ConfigureRegistrationPolicyHandler(context).HandleAsync(
            new ConfigureRegistrationPolicyCommand(
                fixture.EventId.Value, fixture.TeamId.Value, null, fixture.Start.AddDays(-1), closesAt, null),
            cancellationToken));

    public async ValueTask<IReadOnlyList<IIntegrationEvent>> RunExpiredWaitlistCouponsJobAsync()
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(environment, Clock);
        var job = new ProcessExpiredWaitlistCouponsJob(
            dispatch.Context, dispatch.ScopeFactory, Clock, NullLogger<ProcessExpiredWaitlistCouponsJob>.Instance);
        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(cancellationToken);

        await job.Execute(jobContext);

        return dispatch.PublishedIntegrationEvents.ToList();
    }

    public async ValueTask<Guid> GetCouponCodeAsync(EmailAddress email) =>
        (await environment.RegistrationsDatabase.Context.Coupons.AsNoTracking()
            .SingleAsync(c => c.Email == email, cancellationToken)).Code.Value;

    public ValueTask<TicketType> GetConferencePassAsync() =>
        GetTicketTypeAsync(environment.RegistrationsDatabase.Context, fixture.ConferencePassId);

    public async ValueTask<TicketType> GetTicketTypeAsync(RegistrationsDbContext dbContext, TicketTypeId ticketTypeId)
    {
        var catalog = await dbContext.TicketCatalogs.AsNoTracking()
            .SingleAsync(c => c.Id == fixture.EventId, cancellationToken);
        return catalog.FindTicketType(ticketTypeId);
    }

    private async ValueTask<IReadOnlyList<IIntegrationEvent>> RunAsync(Func<RegistrationsDbContext, ValueTask> action)
    {
        await using var dispatch = DispatchingRegistrationsContext.Create(environment, Clock);
        await action(dispatch.Context);
        await dispatch.SaveChangesAsync(cancellationToken);
        return dispatch.PublishedIntegrationEvents.ToList();
    }
}
