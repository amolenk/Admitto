using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PublicApi;

public sealed class RegisterAttendeeSelfServiceValidator : AbstractValidator<RegisterAttendeeSelfServiceHttpRequest>
{
    public RegisterAttendeeSelfServiceValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.RegisterTicketTypeIds)
            .NotNull();

        RuleFor(x => x.WaitlistTicketTypeIds)
            .NotNull();

        RuleFor(x => x)
            .Must(x => (x.RegisterTicketTypeIds?.Length ?? 0) > 0 || (x.WaitlistTicketTypeIds?.Length ?? 0) > 0)
            .WithMessage("At least one registration or waitlist ticket type must be specified.");
    }
}
