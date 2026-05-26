using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.JoinWaitlist.PublicApi;

public sealed class JoinWaitlistValidator : AbstractValidator<JoinWaitlistHttpRequest>
{
    public JoinWaitlistValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
