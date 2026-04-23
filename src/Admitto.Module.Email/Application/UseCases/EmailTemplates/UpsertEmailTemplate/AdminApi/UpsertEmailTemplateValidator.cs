using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpsertEmailTemplate.AdminApi;

public sealed class UpsertEmailTemplateValidator : AbstractValidator<UpsertEmailTemplateHttpRequest>
{
    public UpsertEmailTemplateValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TextBody)
            .NotEmpty();

        RuleFor(x => x.HtmlBody)
            .NotEmpty();
    }
}
