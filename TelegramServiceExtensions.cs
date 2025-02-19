

namespace MuonRoi.SenderTelegram
{
    public static class TelegramServiceCollectionExtensions
    {
        public static IServiceCollection AddTelegramSender(this IServiceCollection services
            , IConfiguration configuration
            , Action<ITelegramSender>? registerCallbacks = null)
        {
            _ = services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));

            _ = services.AddSingleton(provider =>
            {
                string? token = configuration["Telegram:BotToken"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Missing BotToken configuration in appsettings.json");
                }
                return new TelegramBotClient(token);
            });

            _ = services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();

            _ = services.AddSingleton<ITelegramSender>(provider =>
            {
                ILogger<TelegramSender> logger = provider.GetRequiredService<ILogger<TelegramSender>>();
                ITelegramBotClientWrapper botClientWrapper = provider.GetRequiredService<ITelegramBotClientWrapper>();
                IOptions<TelegramOptions> options = provider.GetRequiredService<IOptions<TelegramOptions>>();
                IRetryPolicyProvider retryPolicyProvider = provider.GetRequiredService<IRetryPolicyProvider>();
                IMessageSplitter messageSplitter = provider.GetRequiredService<IMessageSplitter>();
                IHtmlMessageProcessor htmlMessageProcessor = provider.GetRequiredService<IHtmlMessageProcessor>();

                TelegramSender telegramSender = new(
                    botClientWrapper,
                    options,
                    retryPolicyProvider,
                    logger,
                    messageSplitter,
                    htmlMessageProcessor);

                registerCallbacks?.Invoke(telegramSender);

                return telegramSender;
            });

            return services;
        }

    }
}
