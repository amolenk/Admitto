using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee.AdminApi;

public sealed class AdminRegisterAttendeeValidator : AbstractValidator<AdminRegisterAttendeeHttpRequest>
{
    public AdminRegisterAttendeeValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.TicketTypeSlugs)
            .MustBeParseable(Slug.TryFrom);
    }
}
