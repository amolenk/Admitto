using FluentValidation;
using Amolenk.Admitto.Core.Email.Application.Templating;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

public sealed class UpdateEmailTemplateValidator : AbstractValidator<UpdateEmailTemplateHttpRequest>
{
    public UpdateEmailTemplateValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200)
                .Must(name => BuiltInEmailTemplateCatalog.GetByName(name!) is not { IsCustomizable: false })
                .WithMessage("Identity-email templates are internal and cannot be managed through admin template APIs.");
        });

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TextBody)
            .NotEmpty();
    }
}
