

namespace MuonRoi.SenderTelegram.Services;
public class DefaultRetryPolicyProvider(IOptions<TelegramOptions> options, ILogger<DefaultRetryPolicyProvider> logger) : IRetryPolicyProvider
{
    private readonly int _maxRetryAttempts = options.Value.MaxRetryAttempts;

    public AsyncRetryPolicy GetPolicy()
    {
        return Policy
            .Handle<Exception>(IsTransient)
            .WaitAndRetryAsync(
                _maxRetryAttempts,
                retryAttempt =>
                {
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                        + TimeSpan.FromMilliseconds(new Random().Next(0, 1000));
                    return delay;
                },
                (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(exception,
                        "⚠️ Retry {RetryCount}/{MaxRetryAttempts} after {Delay} seconds due to transient error",
                        retryCount, _maxRetryAttempts, timeSpan.TotalSeconds);
                });
    }

    private bool IsTransient(Exception ex)
    {
        return ex is not OperationCanceledException;
    }
}