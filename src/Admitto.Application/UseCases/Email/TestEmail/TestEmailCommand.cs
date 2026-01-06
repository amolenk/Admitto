using Amolenk.Admitto.Application.Common.Messaging;

namespace Amolenk.Admitto.Application.UseCases.Email.TestEmail;

/// <summary>
/// Represents a command to test a single email.
/// </summary>
public record TestEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string Recipient,
    string EmailType,
    List<AdditionalDetailDto> AdditionalDetails,
    List<TicketSelectionDto> Tickets)
    : Command;
