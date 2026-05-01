using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Shared.Application.Http;

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
        return error.Type switch
        {
            ErrorType.Conflict => TypedResults.Problem(
                title: "Conflict",
                detail: error.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                }),

            ErrorType.NotFound => TypedResults.Problem(
                title: "Not found",
                detail: error.Message,
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                }),

            ErrorType.Unauthorized => TypedResults.Problem(
                title: "Unauthorized",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                }),

            ErrorType.Forbidden => TypedResults.Problem(
                title: "Forbidden",
                detail: error.Message,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                }),

            ErrorType.Validation => TypedResults.Problem(
                title: "Validation error",
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                }),

            _ => TypedResults.Problem(
                title: "Internal server error",
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                })
        };
    }
}