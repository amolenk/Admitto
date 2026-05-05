using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public sealed class TestSendEmailTemplateValidator : AbstractValidator<TestSendEmailTemplateHttpRequest>
{
    public TestSendEmailTemplateValidator()
    {
        RuleFor(x => x.Recipient)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
