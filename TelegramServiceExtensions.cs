namespace MuonRoi.SenderTelegram;

public static class TelegramServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
        services.AddSingleton<ITelegramSender>(provider =>
        {
            ILogger<TelegramSender> logger = provider.GetRequiredService<ILogger<TelegramSender>>();
            ITelegramBotClientWrapper botClientWrapper = provider.GetRequiredService<ITelegramBotClientWrapper>();
            IOptions<TelegramOptions> options = provider.GetRequiredService<IOptions<TelegramOptions>>();
            IRetryPolicyProvider retryPolicyProvider = provider.GetRequiredService<IRetryPolicyProvider>();
            IMessageSplitter messageSplitter = provider.GetRequiredService<IMessageSplitter>();
            IHtmlMessageProcessor htmlMessageProcessor = provider.GetRequiredService<IHtmlMessageProcessor>();

            return new TelegramSender(
                botClientWrapper,
                options,
                retryPolicyProvider,
                logger,
                messageSplitter,
                htmlMessageProcessor);
        });

        return services;
    }
}
