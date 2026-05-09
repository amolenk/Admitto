namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeams;

/// <summary>
/// Lightweight team summary returned by the list-teams endpoint.
/// </summary>
internal sealed record TeamListItemDto(Guid TeamId, string Name, string EmailAddress, uint Version);
