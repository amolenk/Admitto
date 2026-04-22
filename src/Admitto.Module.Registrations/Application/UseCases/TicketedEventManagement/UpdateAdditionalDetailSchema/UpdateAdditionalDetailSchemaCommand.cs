using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

internal sealed record UpdateAdditionalDetailSchemaCommand(
    TicketedEventId EventId,
    uint? ExpectedVersion,
    IReadOnlyList<UpdateAdditionalDetailSchemaCommand.FieldInput> Fields) : Command
{
    internal sealed record FieldInput(string Key, string Name, int MaxLength);
}
