using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed class SendTestEmailValidator : AbstractValidator<SendTestEmailHttpRequest>
{
    public SendTestEmailValidator()
    {
        RuleFor(x => x.Recipient)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}