

namespace MuonRoi.SenderTelegram.Interfaces;
public interface ITelegramBotClientWrapper
{
    Task SendMessageAsync(string chatId, string text, ParseMode parseMode, ReplyMarkup? replyMarkup, CancellationToken cancellationToken);
    Task EditMessageTextAsync(string chatId, int messageId, string text, ParseMode parseMode, CancellationToken cancellationToken);
    Task SendDocumentAsync(string chatId, InputFile document, string caption, CancellationToken cancellationToken);
    Task SendPhotoAsync(string chatId, InputFile photo, string caption, CancellationToken cancellationToken);
    Task SendVideoAsync(string chatId, InputFile video, string caption, CancellationToken cancellationToken);
    Task SendMediaGroupAsync(string chatId, IEnumerable<IAlbumInputMedia> media, CancellationToken cancellationToken);
    Task SetWebhookAsync(string url, CancellationToken cancellationToken);
    void StartReceiving(
    Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
    Func<ITelegramBotClient, Exception, CancellationToken, Task> pollingErrorHandler,
    ReceiverOptions receiverOptions,
    CancellationToken cancellationToken);
}
