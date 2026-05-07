using FluentValidation;
using Amolenk.Admitto.Module.Email.Application.Templating;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public sealed class CreateEmailTemplateValidator : AbstractValidator<CreateEmailTemplateHttpRequest>
{
    public CreateEmailTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        // Subject and TextBody are only required for custom templates.
        // Built-in templates (reserved names) fall back to catalog defaults.
        When(x => !BuiltInEmailTemplateNames.IsReserved(x.Name ?? ""), () =>
        {
            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.TextBody)
                .NotEmpty();
        });
    }
}
