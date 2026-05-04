namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey.AdminApi;

public sealed record CreateApiKeyHttpRequest(string Name)
{
    internal CreateApiKeyCommand ToCommand(Guid teamId, string createdBy)
        => new(teamId, Name, createdBy);
}
