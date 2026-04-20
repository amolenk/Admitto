namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy;

public sealed record CancellationPolicyDto(
    DateTimeOffset LateCancellationCutoff);
