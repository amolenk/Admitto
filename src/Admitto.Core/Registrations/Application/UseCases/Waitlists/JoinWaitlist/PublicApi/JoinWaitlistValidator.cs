using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.JoinWaitlist.PublicApi;

public sealed class JoinWaitlistValidator : AbstractValidator<JoinWaitlistHttpRequest>
{
    public JoinWaitlistValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
