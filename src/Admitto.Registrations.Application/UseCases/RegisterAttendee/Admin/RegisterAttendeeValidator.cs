using Amolenk.Admitto.Registrations.Domain.Validation;
using Amolenk.Admitto.Shared.Application.Validation;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee.Admin;

public class RegisterAttendeeValidator : AbstractValidator<RegisterAttendeeHttpRequest>
{
    public RegisterAttendeeValidator()
    {
        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryNormalize);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryNormalize);

        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        // TODO Verify
        RuleForEach(x => x.TicketTypeIds)
            .NotNull()
            .NotEmpty();
    }
}
