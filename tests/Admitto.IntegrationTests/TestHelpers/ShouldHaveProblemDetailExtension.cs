using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

public static class ShouldHaveProblemDetailExtension
{
    public static Task ShouldBeBadRequestAsync(this HttpResponseMessage response, string detail)
    {
        return response.ShouldBeBadRequestAsync(problem =>
        {
            problem.Detail.ShouldBe(detail);
        });
    }

    public static Task ShouldBeBadRequestAsync(this HttpResponseMessage response,
        string expectedErrorKey, string expectedErrorValue)
    {
        return response.ShouldBeBadRequestAsync(problem =>
        {
            problem.Errors.ShouldContainError(expectedErrorKey, expectedErrorValue);
        });
    }
    
    public static async Task ShouldBeBadRequestAsync(this HttpResponseMessage response, Action<ValidationProblemDetails> condition)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        validationProblem.ShouldNotBeNull();
        
        condition(validationProblem);
    }
    
    [Obsolete("Use ShouldBeBadRequestAsync instead.")]
    public static async Task ShouldHaveProblemDetailAsync(this HttpResponseMessage response,
        HttpStatusCode? expectedStatusCode = null, string? expectedDetail = null,
        params Action<ValidationProblemDetails>[] conditions)
    {
        response.StatusCode.ShouldBe(expectedStatusCode ?? HttpStatusCode.BadRequest);
        
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        validationProblem.ShouldNotBeNull();

        if (expectedDetail is not null)
        {
            validationProblem.Detail.ShouldBe(expectedDetail);
        }

        foreach (var condition in conditions)
        {
            condition(validationProblem);
        }
    }
    
    [Obsolete("Use ShouldBeBadRequestAsync instead.")]
    public static void ShouldContainError(this ValidationProblemDetails validationProblem, string expectedKey,
        string expectedValue)
    {
        validationProblem.Errors.ShouldContainKey(expectedKey);
            
        if (validationProblem.Errors.TryGetValue(expectedKey, out var error))
        {
            error.ShouldContain(expectedValue);
        }
    }
    
    public static void ShouldContainError(this IDictionary<string, string[]> errors, string expectedKey,
        string expectedValue)
    {
        errors.ShouldContainKey(expectedKey);
            
        if (errors.TryGetValue(expectedKey, out var error))
        {
            error.ShouldContain(expectedValue);
        }
    }
}