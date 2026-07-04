using FluentValidation;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

public sealed class PreviewBulkEmailValidator : AbstractValidator<PreviewBulkEmailHttpRequest>
{
    public PreviewBulkEmailValidator()
    {
        RuleFor(x => x.AttendeeFilter).NotNull();
    }
}
