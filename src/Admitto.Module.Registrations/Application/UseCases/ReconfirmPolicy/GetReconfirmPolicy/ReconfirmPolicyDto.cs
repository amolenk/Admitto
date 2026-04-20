namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.GetReconfirmPolicy;

public sealed record ReconfirmPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int CadenceDays);
