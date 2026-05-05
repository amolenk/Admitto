using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.UpdateCustomBulkTemplate.AdminApi;

public sealed class UpdateCustomBulkTemplateValidator : AbstractValidator<UpdateCustomBulkTemplateHttpRequest>
{
    public UpdateCustomBulkTemplateValidator()
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
