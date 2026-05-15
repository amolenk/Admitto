using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.AdminApi;

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
