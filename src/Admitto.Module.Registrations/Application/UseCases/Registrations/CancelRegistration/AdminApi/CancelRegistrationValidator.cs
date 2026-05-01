using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.CancelRegistration.AdminApi;

public sealed class CancelRegistrationValidator : AbstractValidator<CancelRegistrationHttpRequest>
{
    private static readonly HashSet<string> AllowedReasons =
    [
        nameof(CancellationReason.AttendeeRequest),
        nameof(CancellationReason.VisaLetterDenied)
    ];

    public CancelRegistrationValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("'Reason' must not be empty.")
            .Must(r => AllowedReasons.Contains(r!))
            .WithMessage($"'Reason' must be one of: {string.Join(", ", AllowedReasons)}.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
