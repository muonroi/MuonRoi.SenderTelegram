namespace MuonRoi.SenderTelegram.Helpers;
public static class RetryHelper
{
    public static async Task<bool> ExecuteWithRetryAsync(
        Func<CancellationToken, Task> action,
        AsyncRetryPolicy retryPolicy,
        ILogger logger,
        string successLogMessage,
        string errorLogMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await retryPolicy.ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("{SuccessLogMessage}", successLogMessage);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ErrorLogMessage}", errorLogMessage);
            return false;
        }
    }
}

