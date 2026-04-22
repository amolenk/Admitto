namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema.AdminApi;

public sealed record UpdateAdditionalDetailSchemaHttpRequest(
    IReadOnlyList<UpdateAdditionalDetailSchemaHttpRequest.FieldDto> Fields,
    uint? ExpectedVersion = null)
{
    public sealed record FieldDto(string Key, string Name, int MaxLength);
}
