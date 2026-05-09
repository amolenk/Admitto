using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public sealed class TestSendEmailTemplateValidator : AbstractValidator<TestSendEmailTemplateHttpRequest>
{
    public TestSendEmailTemplateValidator()
    {
        RuleFor(x => x.Recipient)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
