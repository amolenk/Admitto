using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Services;

internal static class RegistrationTicketClassifier
{
    public static void EnsureNoDuplicateRequestedActions(
        IReadOnlyList<TicketTypeId> registerTicketTypeIds,
        IReadOnlyList<TicketTypeId> waitlistTicketTypeIds)
    {
        var duplicateIds = registerTicketTypeIds
            .Concat(waitlistTicketTypeIds)
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.Value)
            .ToArray();

        if (duplicateIds.Length > 0)
            throw new BusinessRuleViolationException(TicketCatalog.Errors.DuplicateTicketTypes(duplicateIds));
    }

    public static void ValidateWaitlistRequests(
        TicketCatalog catalog,
        IReadOnlyList<TicketTypeId> waitlistTicketTypeIds)
    {
        foreach (var ticketTypeId in waitlistTicketTypeIds)
        {
            var ticketType = catalog.GetTicketType(ticketTypeId);
            if (ticketType is null)
                throw new BusinessRuleViolationException(TicketCatalog.Errors.UnknownTicketTypes([ticketTypeId.Value]));

            if (!ticketType.SelfServiceEnabled)
                throw new BusinessRuleViolationException(
                    TicketCatalog.Errors.TicketTypesNotSelfService([ticketTypeId.Value]));

            if (!ticketType.WaitlistEnabled)
                throw new BusinessRuleViolationException(Errors.WaitlistNotEnabled(ticketTypeId));

            if (!ticketType.WaitlistMode)
                throw new BusinessRuleViolationException(Errors.StaleTicketState(ticketTypeId));
        }
    }

    public static void EnsureRequestedTicketStatesMatch(
        TicketCatalog catalog,
        IReadOnlyList<TicketTypeId> registerTicketTypeIds,
        IReadOnlyList<TicketTypeId> waitlistTicketTypeIds)
    {
        var states = ClassifyRequestedTicketStates(catalog, registerTicketTypeIds, waitlistTicketTypeIds);

        var hasRegistrationMismatch = registerTicketTypeIds.Any(id => !states.RegisterableTicketTypeIds.Contains(id.Value));
        var hasWaitlistMismatch = waitlistTicketTypeIds.Any(id => !states.WaitlistableTicketTypeIds.Contains(id.Value));
        if (hasRegistrationMismatch || hasWaitlistMismatch)
            throw new BusinessRuleViolationException(Errors.TicketStateConflict(states));
    }

    private static TicketStateConflict ClassifyRequestedTicketStates(
        TicketCatalog catalog,
        IReadOnlyList<TicketTypeId> registerTicketTypeIds,
        IReadOnlyList<TicketTypeId> waitlistTicketTypeIds)
    {
        List<Guid> registerable = [];
        List<Guid> waitlistable = [];
        List<Guid> unavailable = [];
        List<Guid> unknown = [];
        List<Guid> invalidForRequestedAction = [];

        foreach (var ticketTypeId in registerTicketTypeIds)
            ClassifyRequestedTicket(catalog, ticketTypeId, registerable, waitlistable, unavailable, unknown);

        foreach (var ticketTypeId in waitlistTicketTypeIds)
        {
            var beforeRegisterableCount = registerable.Count;
            var beforeWaitlistableCount = waitlistable.Count;
            var beforeUnavailableCount = unavailable.Count;
            var beforeUnknownCount = unknown.Count;

            ClassifyRequestedTicket(catalog, ticketTypeId, registerable, waitlistable, unavailable, unknown);

            if (registerable.Count == beforeRegisterableCount
                && waitlistable.Count == beforeWaitlistableCount
                && unavailable.Count == beforeUnavailableCount
                && unknown.Count == beforeUnknownCount)
            {
                invalidForRequestedAction.Add(ticketTypeId.Value);
            }
        }

        return new TicketStateConflict(
            registerable.ToArray(),
            waitlistable.ToArray(),
            unavailable.ToArray(),
            unknown.ToArray(),
            invalidForRequestedAction.ToArray());
    }

    private static void ClassifyRequestedTicket(
        TicketCatalog catalog,
        TicketTypeId ticketTypeId,
        List<Guid> registerable,
        List<Guid> waitlistable,
        List<Guid> unavailable,
        List<Guid> unknown)
    {
        var ticketType = catalog.GetTicketType(ticketTypeId);
        if (ticketType is null)
        {
            unknown.Add(ticketTypeId.Value);
            return;
        }

        if (!ticketType.SelfServiceEnabled)
        {
            unavailable.Add(ticketTypeId.Value);
            return;
        }

        if (ticketType.WaitlistEnabled && ticketType.WaitlistMode)
        {
            waitlistable.Add(ticketTypeId.Value);
            return;
        }

        if (!ticketType.WaitlistMode && !ticketType.IsSoldOut)
        {
            registerable.Add(ticketTypeId.Value);
            return;
        }

        unavailable.Add(ticketTypeId.Value);
    }

    internal sealed record TicketStateConflict(
        Guid[] RegisterableTicketTypeIds,
        Guid[] WaitlistableTicketTypeIds,
        Guid[] UnavailableTicketTypeIds,
        Guid[] UnknownTicketTypeIds,
        Guid[] InvalidForRequestedActionTicketTypeIds);

    internal static class Errors
    {
        public static Error TicketStateConflict(TicketStateConflict conflict) => new(
            "registration.ticket_state_conflict",
            "The requested ticket type state no longer matches the submitted action.",
            Type: ErrorType.Conflict,
            Details: new Dictionary<string, object?>
            {
                ["registerableTicketTypeIds"] = conflict.RegisterableTicketTypeIds,
                ["waitlistableTicketTypeIds"] = conflict.WaitlistableTicketTypeIds,
                ["unavailableTicketTypeIds"] = conflict.UnavailableTicketTypeIds,
                ["unknownTicketTypeIds"] = conflict.UnknownTicketTypeIds,
                ["invalidForRequestedActionTicketTypeIds"] = conflict.InvalidForRequestedActionTicketTypeIds
            });

        public static Error WaitlistNotEnabled(TicketTypeId id) => new(
            "registration.waitlist_not_enabled",
            "The waitlist is not enabled for this ticket type.",
            Type: ErrorType.Validation,
            Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error StaleTicketState(TicketTypeId id) => new(
            "registration.stale_ticket_state",
            "The requested ticket type state no longer matches the submitted action.",
            Type: ErrorType.Conflict,
            Details: new Dictionary<string, object?> { ["id"] = id.Value });
    }
}
