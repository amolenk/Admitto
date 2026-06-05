using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.CreateApiKey.AdminApi;

public sealed class CreateApiKeyValidator : AbstractValidator<CreateApiKeyHttpRequest>
{
    public CreateApiKeyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
