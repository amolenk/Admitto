using Amolenk.Admitto.Module.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey.AdminApi;

public sealed class CreateApiKeyValidator : AbstractValidator<CreateApiKeyHttpRequest>
{
    public CreateApiKeyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
