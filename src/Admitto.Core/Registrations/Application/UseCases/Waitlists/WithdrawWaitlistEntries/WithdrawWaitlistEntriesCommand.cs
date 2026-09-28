using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;

internal sealed record WithdrawWaitlistEntriesCommand(
    Guid EventId,
    Guid TeamId,
    string Email) : Command;
