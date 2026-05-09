using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

public sealed class SendTestEmailValidator : AbstractValidator<SendTestEmailHttpRequest>
{
    public SendTestEmailValidator()
    {
        RuleFor(x => x.Recipient)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
