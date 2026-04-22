using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee;

internal sealed class AdminRegisterAttendeeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<AdminRegisterAttendeeCommand, RegistrationId>
{
    public async ValueTask<RegistrationId> HandleAsync(
        AdminRegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken);

        if (ticketedEvent is null)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeHandler.Errors.EventNotFound);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeHandler.Errors.EventNotActive);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeHandler.Errors.NoTicketTypesConfigured);

        var ticketTypeMap = catalog.TicketTypes.ToDictionary(t => t.Id);
        SelfRegisterAttendeeHandler.ValidateTicketTypeSelection(command.TicketTypeSlugs, ticketTypeMap);

        var tickets = command.TicketTypeSlugs
            .Select(slug => new TicketTypeSnapshot(
                slug,
                ticketTypeMap[slug].TimeSlots.Select(ts => ts.Slug.Value).ToArray()))
            .ToList();

        try
        {
            // Admin-add bypasses capacity enforcement (same as coupons), but the active-status
            // safety net on the catalog still trips on a concurrent cancel/archive.
            catalog.Claim(command.TicketTypeSlugs, enforce: false);
        }
        catch (BusinessRuleViolationException ex)
            when (ex.Error.Code == TicketCatalog.Errors.EventNotActive.Code)
        {
            throw new BusinessRuleViolationException(SelfRegisterAttendeeHandler.Errors.EventNotActive);
        }

        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        var registration = Registration.Create(
            command.EventId,
            command.Email,
            tickets,
            additionalDetails);
        await writeStore.Registrations.AddAsync(registration, cancellationToken);

        return registration.Id;
    }
}
