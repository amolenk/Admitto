using FluentValidation;

namespace Amolenk.Admitto.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v =>
                    v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }
        
        return await next();
    }
}

// using Admitto.Application.Behaviors;
// using FluentValidation;
// using MediatR;
// using Moq;
// using Xunit;
//
// public class ValidationBehaviorTests
// {
//     [Fact]
//     public async Task Should_ThrowValidationException_WhenValidationFails()
//     {
//         // Arrange
//         var validators = new List<IValidator<TestRequest>>
//         {
//             new TestRequestValidator()
//         };
//
//         var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
//
//         var request = new TestRequest { Name = string.Empty }; // Invalid input
//
//         // Act & Assert
//         await Assert.ThrowsAsync<ValidationException>(
//             () => behavior.Handle(request, () => Task.FromResult(new TestResponse()), default));
//     }
//
//     public class TestRequest
//     {
//         public string Name { get; set; } = string.Empty;
//     }
//
//     public class TestResponse { }
//
//     public class TestRequestValidator : AbstractValidator<TestRequest>
//     {
//         public TestRequestValidator()
//         {
//             RuleFor(x => x.Name).NotEmpty();
//         }
//     }
// }