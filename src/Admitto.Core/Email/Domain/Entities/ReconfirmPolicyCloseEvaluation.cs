using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Domain.Entities;

/// <summary>
/// Durable idempotency marker for the terminal evaluation of one projected
/// reconfirm policy window.
/// </summary>
public sealed class ReconfirmPolicyCloseEvaluation
{
    private ReconfirmPolicyCloseEvaluation()
    {
    }

    private ReconfirmPolicyCloseEvaluation(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt,
        DateTimeOffset evaluatedAt)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        ClosesAt = closesAt;
        EvaluatedAt = evaluatedAt;
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId TicketedEventId { get; private set; }
    public DateTimeOffset ClosesAt { get; private set; }
    public DateTimeOffset EvaluatedAt { get; private set; }

    public static ReconfirmPolicyCloseEvaluation Create(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt,
        DateTimeOffset evaluatedAt) =>
        new(teamId, ticketedEventId, closesAt, evaluatedAt);
}
