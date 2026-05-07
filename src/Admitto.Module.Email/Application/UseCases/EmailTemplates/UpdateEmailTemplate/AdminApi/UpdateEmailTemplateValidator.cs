using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

public sealed class UpdateEmailTemplateValidator : AbstractValidator<UpdateEmailTemplateHttpRequest>
{
    public UpdateEmailTemplateValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);
        });

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TextBody)
            .NotEmpty();
    }
}
