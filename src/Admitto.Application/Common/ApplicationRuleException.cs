namespace Amolenk.Admitto.Application.Common;

/// <summary>
/// Represents an application rule exception.
/// </summary>
public class ApplicationRuleException(ApplicationRuleError error) : Exception(error.MessageText)
{
    public string ErrorCode { get; } = error.Code;
    
    public ProblemDetails ToProblemDetails(HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Application rule violated",
            Detail = Message,
            Instance = httpContext.Request.Path
        };
        
        problemDetails.Extensions.Add("errorCode", ErrorCode);

        return problemDetails;
    }
}