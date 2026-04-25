using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public sealed class SelfRegisterAttendeeValidator : AbstractValidator<SelfRegisterAttendeeHttpRequest>
{
    public SelfRegisterAttendeeValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty();

        // EmailVerificationToken is intentionally NOT required at the FluentValidation
        // layer so the handler can produce the canonical `email.verification_required`
        // error and so token shape/signature checks remain the validator service's
        // responsibility.
    }
}
