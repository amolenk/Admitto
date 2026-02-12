using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Shouldly;

namespace Amolenk.Admitto.Testing.Infrastructure.Assertions;

public class ErrorResult(Error error)
{
    public Error Error => error;

    public static ErrorResult Capture(Action action)
    {
        try
        {
            action();
        }
        catch (BusinessRuleViolationException ex)
        {
            return new ErrorResult(ex.Error);
        }
        
        throw new ShouldAssertException(
            "Expected action to throw BusinessRuleViolationException, but no exception was thrown.");
    }
    
    public static async ValueTask<ErrorResult> CaptureAsync(Func<ValueTask> func)
    {
        try
        {
            await func();
        }
        catch (BusinessRuleViolationException ex)
        {
            return new ErrorResult(ex.Error);
        }
        
        throw new ShouldAssertException(
            "Expected function to throw BusinessRuleViolationException, but no exception was thrown.");
    }
}