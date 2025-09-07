using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Contributors.UpdateContributor;

public record UpdateContributorRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    List<AdditionalDetailDto>? AdditionalDetails,
    List<ContributorRole>? Roles);

public record AdditionalDetailDto(string Name, string Value);
