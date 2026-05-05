using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.CreateCustomBulkTemplate.AdminApi;

public sealed class CreateCustomBulkTemplateValidator : AbstractValidator<CreateCustomBulkTemplateHttpRequest>
{
    public CreateCustomBulkTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.TextBody)
            .NotEmpty();
    }
}
