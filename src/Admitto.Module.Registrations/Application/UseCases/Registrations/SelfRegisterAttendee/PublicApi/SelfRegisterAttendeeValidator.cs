using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee.PublicApi;

public sealed class SelfRegisterAttendeeValidator : AbstractValidator<SelfRegisterAttendeeHttpRequest>
{
    public SelfRegisterAttendeeValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty();
    }
}
