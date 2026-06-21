using FluentValidation;
using Amolenk.Admitto.Core.Email.Application.Templating;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public sealed class CreateEmailTemplateValidator : AbstractValidator<CreateEmailTemplateHttpRequest>
{
    public CreateEmailTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .Must(name => BuiltInEmailTemplateCatalog.GetByName(name) is not { IsCustomizable: false })
            .WithMessage("Identity-email templates are internal and cannot be managed through admin template APIs.");
    }
}
