using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn.AdminApi;

public sealed class CheckInValidator : AbstractValidator<CheckInHttpRequest>
{
    public CheckInValidator()
    {
        RuleFor(x => x.Credential).NotNull();
    }
}
