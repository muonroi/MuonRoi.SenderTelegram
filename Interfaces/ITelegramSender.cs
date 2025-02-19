namespace MuonRoi.SenderTelegram.Interfaces;

public interface ITelegramSender
{
    Task<bool> SendMessageAsync(
        string message,
        string? chatId = null,
        string formatType = "default",
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendErrorMessageAsync(string message,
        CancellationToken cancellationToken = default);

    Task<bool> SendDocumentAsync(
        Stream fileStream,
        string fileName,
        string? chatId = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendPhotoAsync(
        Stream photoStream,
        string? chatId = null,
        CancellationToken cancellationToken = default);

    Task<bool> EditMessageAsync(
        string chatId,
        int messageId,
        string newMessage,
        string formatType = "default",
        CancellationToken cancellationToken = default);

    Task<bool> SendVideoAsync(
        Stream videoStream,
        string fileName,
        string? chatId = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendMediaGroupAsync(
        IEnumerable<IAlbumInputMedia> media,
        string? chatId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ScheduleMessageAsync(
        string message,
        TimeSpan delay,
        string? chatId = null,
        string formatType = "default",
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    void RegisterCallbackHandlers(string commandPrefix, Func<TelegramCallbackQuery, Task> handler);

    Task HandleCallbackQueryAsync(TelegramCallbackQuery callbackQuery);
}
