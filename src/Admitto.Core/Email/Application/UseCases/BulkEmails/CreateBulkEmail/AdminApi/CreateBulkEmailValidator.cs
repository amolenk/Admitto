using FluentValidation;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

public sealed class CreateBulkEmailValidator : AbstractValidator<CreateBulkEmailHttpRequest>
{
    public CreateBulkEmailValidator()
    {
        RuleFor(x => x.EmailType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.TextBody)
            .NotEmpty();

        RuleFor(x => x.HtmlBody)
            .NotEmpty();

        RuleFor(x => x.AttendeeFilter).NotNull();
    }
}
