using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public sealed class SelfRegisterAttendeeValidator : AbstractValidator<SelfRegisterAttendeeHttpRequest>
{
    public SelfRegisterAttendeeValidator()
    {
        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty();
    }
}
