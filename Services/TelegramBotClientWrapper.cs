

namespace MuonRoi.SenderTelegram.Services;
public class TelegramBotClientWrapper(TelegramBotClient client) : ITelegramBotClientWrapper
{
    public Task SendMessageAsync(string chatId, string text, ParseMode parseMode, ReplyMarkup? replyMarkup, CancellationToken cancellationToken)
    {
        return client.SendMessage(chatId, text, parseMode, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
    }

    public Task EditMessageTextAsync(string chatId, int messageId, string text, ParseMode parseMode, CancellationToken cancellationToken)
    {
        return client.EditMessageText(chatId, messageId, text, parseMode, cancellationToken: cancellationToken);
    }

    public Task SendDocumentAsync(string chatId, InputFile document, string caption, CancellationToken cancellationToken)
    {
        return client.SendDocument(chatId, document, caption: caption, cancellationToken: cancellationToken);
    }

    public Task SendPhotoAsync(string chatId, InputFile photo, string caption, CancellationToken cancellationToken)
    {
        return client.SendPhoto(chatId, photo, caption: caption, cancellationToken: cancellationToken);
    }

    public Task SendVideoAsync(string chatId, InputFile video, string caption, CancellationToken cancellationToken)
    {
        return client.SendVideo(chatId, video, caption: caption, cancellationToken: cancellationToken);
    }

    public Task SendMediaGroupAsync(string chatId, IEnumerable<IAlbumInputMedia> media, CancellationToken cancellationToken)
    {
        return client.SendMediaGroup(chatId, media.ToList(), cancellationToken: cancellationToken);
    }
    public Task SetWebhookAsync(string url, CancellationToken cancellationToken)
    {
        return client.SetWebhook(url, cancellationToken: cancellationToken);
    }

    public void StartReceiving(
    Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
    Func<ITelegramBotClient, Exception, CancellationToken, Task> pollingErrorHandler,
    ReceiverOptions receiverOptions,
    CancellationToken cancellationToken)
    {
        client.StartReceiving(updateHandler, pollingErrorHandler, receiverOptions, cancellationToken);
    }

    public void OnMessage(Func<ITelegramBotClient, Message, UpdateType, CancellationToken, Task> messageHandler, CancellationToken cancellationToken)
    {
        client.OnMessage += async (sender, args) =>
        {
            await messageHandler(client, sender, args, cancellationToken);
        };
    }
}