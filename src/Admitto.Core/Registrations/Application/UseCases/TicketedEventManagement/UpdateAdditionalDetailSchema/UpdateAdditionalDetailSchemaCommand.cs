using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

internal sealed record UpdateAdditionalDetailSchemaCommand(
    Guid EventId,
    uint? ExpectedVersion,
    IReadOnlyList<UpdateAdditionalDetailSchemaCommand.FieldInput> Fields) : Command
{
    internal sealed record FieldInput(string Key, string Name, int MaxLength);
}
