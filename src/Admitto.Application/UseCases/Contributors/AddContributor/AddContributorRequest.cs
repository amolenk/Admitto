using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Contributors.AddContributor;

public record AddContributorRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<ContributorRoleDto> Roles);

public record AdditionalDetailDto(string Name, string Value);

public record ContributorRoleDto(string Name);
