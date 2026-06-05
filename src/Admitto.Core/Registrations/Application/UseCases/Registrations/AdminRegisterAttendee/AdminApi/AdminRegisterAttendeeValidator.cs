using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee.AdminApi;

public sealed class AdminRegisterAttendeeValidator : AbstractValidator<AdminRegisterAttendeeHttpRequest>
{
    public AdminRegisterAttendeeValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeIds)
            .NotNull()
            .NotEmpty();
    }
}
