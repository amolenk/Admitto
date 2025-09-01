using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Contributors.GetContributors;

public record GetContributorsResponse(ContributorDto[] Contributors);

public record ContributorDto(
    Guid ContributorId,
    string Email,
    string FirstName,
    string LastName,
    ContributorRoleDto[] Roles,
    DateTimeOffset LastChangedAt);

public record ContributorRoleDto(string Name);
