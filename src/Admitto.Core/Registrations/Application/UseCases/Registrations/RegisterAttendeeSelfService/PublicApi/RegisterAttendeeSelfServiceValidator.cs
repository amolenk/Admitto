using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PublicApi;

public sealed class RegisterAttendeeSelfServiceValidator : AbstractValidator<RegisterAttendeeSelfServiceHttpRequest>
{
    public RegisterAttendeeSelfServiceValidator()
    {
        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeIds)
            .NotNull()
            .NotEmpty();
    }
}
