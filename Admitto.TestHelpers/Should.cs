using System.Diagnostics;
using Shouldly;

namespace Amolenk.Admitto.TestHelpers;

public static class Should
{   
    public static async ValueTask Eventually(Func<ValueTask> task, TimeSpan timeout, TimeSpan? retryEvery = null,
        string? customMessage = null)
    {
        retryEvery ??= TimeSpan.FromMilliseconds(200);
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                await task();
                return; // Success, exit the method
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(retryEvery.Value);
            }
        }

        // If we get here, the operation didn't succeed within the timeout
        var message = customMessage ?? 
                      $"Operation did not succeed within timeout of {timeout.TotalSeconds} seconds";
    
        throw new ShouldAssertException(message, lastException);
    }
}

