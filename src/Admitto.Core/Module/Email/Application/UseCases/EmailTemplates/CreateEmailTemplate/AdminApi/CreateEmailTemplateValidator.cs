using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public sealed class CreateEmailTemplateValidator : AbstractValidator<CreateEmailTemplateHttpRequest>
{
    public CreateEmailTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
