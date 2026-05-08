using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

internal sealed record UpdateAdditionalDetailSchemaCommand(
    Guid EventId,
    uint? ExpectedVersion,
    IReadOnlyList<UpdateAdditionalDetailSchemaCommand.FieldInput> Fields) : Command
{
    internal sealed record FieldInput(string Key, string Name, int MaxLength);
}
