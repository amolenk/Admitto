using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Shared.Application.Http;

public static class ResultHttpExtensions
{
    public static Results<THttpResult, ProblemHttpResult> ToHttpResult<T, THttpResult>(
        this ValidationResult<T> result,
        Func<T, THttpResult> onSuccess)
        where THttpResult: IResult
    {
        return result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblemHttpResult();
    }
    
    public static ProblemHttpResult ToProblemHttpResult(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        if (error.Details is not null)
        {
            foreach (var detail in error.Details)
            {
                extensions[detail.Key] = detail.Value;
            }
        }

        return error.Type switch
        {
            ErrorType.Conflict => TypedResults.Problem(
                title: "Conflict",
                detail: error.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: extensions),

            ErrorType.NotFound => TypedResults.Problem(
                title: "Not found",
                detail: error.Message,
                statusCode: StatusCodes.Status404NotFound,
                extensions: extensions),

            ErrorType.Unauthorized => TypedResults.Problem(
                title: "Unauthorized",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: extensions),

            ErrorType.Forbidden => TypedResults.Problem(
                title: "Forbidden",
                detail: error.Message,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: extensions),

            ErrorType.TooManyRequests => TypedResults.Problem(
                title: "Too many requests",
                detail: error.Message,
                statusCode: StatusCodes.Status429TooManyRequests,
                extensions: extensions),

            ErrorType.Unprocessable => TypedResults.Problem(
                title: "Unprocessable entity",
                detail: error.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: extensions),

            ErrorType.Validation => TypedResults.Problem(
                title: "Validation error",
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions),

            _ => TypedResults.Problem(
                title: "Internal server error",
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: extensions)
        };
    }
}
