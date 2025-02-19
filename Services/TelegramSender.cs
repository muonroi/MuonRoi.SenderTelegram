namespace MuonRoi.SenderTelegram.Services;

/// <summary>
/// Service for sending messages, documents, photos, videos, and other media types to Telegram via a Telegram bot client.
/// Supports sending text messages (including HTML), handling message splitting, retry logic, and scheduling message delivery.
/// </summary>
public partial class TelegramSender : ITelegramSender
{
    private readonly ITelegramBotClientWrapper botClientWrapper;
    private readonly IMessageSplitter messageSplitter;
    private readonly IHtmlMessageProcessor htmlMessageProcessor;
    private readonly ILogger<TelegramSender> logger;
    private readonly Dictionary<string, string> messageFormats;
    private readonly AsyncRetryPolicy retryPolicy;
    private readonly string channelId;
    private readonly string errorChannelId;
    private readonly int maxMessageLength;

    /// <summary>
    /// Regular expression used to check if a message contains HTML.
    /// </summary>
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlRegex();

    /// <summary>
    /// Initializes an instance of <see cref="TelegramSender"/>.
    /// </summary>
    /// <param name="botClientWrapper">Wrapper for the Telegram bot client used to send requests to Telegram.</param>
    /// <param name="options">Configuration related to Telegram.</param>
    /// <param name="retryPolicyProvider">Provider supplying the retry policy for message-sending operations.</param>
    /// <param name="logger">Logger for recording events and errors.</param>
    /// <param name="messageSplitter">Service for splitting messages that exceed the length limit.</param>
    /// <param name="htmlMessageProcessor">Service for processing messages containing HTML.</param>
    /// <exception cref="ArgumentNullException">Thrown when one of the dependencies is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the configuration is missing ChannelId or ErrorChannelId.</exception>
    public TelegramSender(
        ITelegramBotClientWrapper botClientWrapper,
        IOptions<TelegramOptions> options,
        IRetryPolicyProvider retryPolicyProvider,
        ILogger<TelegramSender> logger,
        IMessageSplitter messageSplitter,
        IHtmlMessageProcessor htmlMessageProcessor)
    {
        this.botClientWrapper = botClientWrapper ?? throw new ArgumentNullException(nameof(botClientWrapper));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.messageSplitter = messageSplitter ?? throw new ArgumentNullException(nameof(messageSplitter));
        this.htmlMessageProcessor = htmlMessageProcessor ?? throw new ArgumentNullException(nameof(htmlMessageProcessor));

        TelegramOptions config = options.Value;

        if (string.IsNullOrWhiteSpace(config.ChannelId))
        {
            throw new InvalidOperationException("Missing ChannelId configuration in appsettings.json");
        }
        if (string.IsNullOrWhiteSpace(config.ErrorChannelId))
        {
            throw new InvalidOperationException("Missing ErrorChannelId configuration in appsettings.json");
        }

        channelId = config.ChannelId;
        errorChannelId = config.ErrorChannelId;
        messageFormats = config.Formats ?? [];
        if (!messageFormats.ContainsKey("default"))
        {
            messageFormats["default"] = "{0}";
        }
        maxMessageLength = config.MaxMessageLength;
        retryPolicy = retryPolicyProvider.GetPolicy();

        logger.LogInformation("TelegramSender initialized successfully!");
    }

    /// <summary>
    /// Sends a text message to the specified Telegram chat.
    /// If the message exceeds the allowed length, it is automatically split and sent in parts.
    /// Supports HTML formatting.
    /// </summary>
    /// <param name="message">The message content to be sent.</param>
    /// <param name="chatId">The destination chat ID. If null, the default channel is used.</param>
    /// <param name="formatType">The message format type, default is "default".</param>
    /// <param name="replyMarkup">Optional reply markup to include with the message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the message-sending operation.</param>
    /// <returns>True if all parts of the message were successfully sent, otherwise false.</returns>
    public async Task<bool> SendMessageAsync(
        string message,
        string? chatId = null,
        string formatType = "default",
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        string validChatId = ValidateChatId(chatId);
        string formattedMessage = FormatMessage(message, formatType);

        IEnumerable<string> parts = IsHtml(formattedMessage)
            ? htmlMessageProcessor.Process(formattedMessage, maxMessageLength)
            : messageSplitter.Split(formattedMessage, maxMessageLength);

        List<string> partList = parts.ToList();
        if (partList.Count == 0)
        {
            logger.LogWarning("No message parts were created for channel: {ChatId}", validChatId);
            return true;
        }

        List<int> failedParts = [];
        int partIndex = 0;

        foreach (string part in partList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            partIndex++;

            bool partSent = await SendMessagePartAsync(part, partIndex, validChatId, replyMarkup, cancellationToken).ConfigureAwait(false);
            if (!partSent)
            {
                failedParts.Add(partIndex);
            }
        }

        if (failedParts.Count != 0)
        {
            logger.LogError("Failed to send {FailedCount} out of {TotalCount} parts for channel: {ChatId}. Failed parts: {FailedParts}",
                failedParts.Count, partList.Count, validChatId, string.Join(", ", failedParts));
            return false;
        }

        logger.LogInformation("Successfully sent all message parts to channel: {ChatId}", validChatId);
        return true;
    }

    /// <summary>
    /// Helper method to send a single message part.
    /// If the part exceeds the maximum length and is not HTML, it is further split into sub-parts.
    /// </summary>
    /// <param name="part">The message part text.</param>
    /// <param name="partIndex">Index of the message part.</param>
    /// <param name="validChatId">Validated chat ID.</param>
    /// <param name="replyMarkup">Optional reply markup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the part (and its sub-parts, if any) were sent successfully; otherwise false.</returns>
    private async Task<bool> SendMessagePartAsync(string part, int partIndex, string validChatId, ReplyMarkup? replyMarkup, CancellationToken cancellationToken)
    {
        if (part.Length > maxMessageLength)
        {
            if (!IsHtml(part))
            {
                logger.LogWarning("Message part {PartIndex} exceeds the maximum allowed length for channel: {ChatId}. Splitting further.", partIndex, validChatId);
                List<string> subParts = messageSplitter.Split(part, maxMessageLength).ToList();
                int subIndex = 0;
                bool allSubPartsSent = true;
                foreach (string subPart in subParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    subIndex++;
                    bool subPartSent = await ExecuteWithRetryAsync(
                        ct => botClientWrapper.SendMessageAsync(
                                chatId: validChatId,
                                text: subPart,
                                parseMode: ParseMode.Html,
                                replyMarkup: replyMarkup,
                                cancellationToken: ct),
                        $"Message part {partIndex}.{subIndex} sent to channel: {validChatId}",
                        "Error sending sub-message part to Telegram.",
                        cancellationToken).ConfigureAwait(false);
                    if (!subPartSent)
                    {
                        allSubPartsSent = false;
                    }
                }
                return allSubPartsSent;
            }
            else
            {
                logger.LogWarning("HTML message part {PartIndex} exceeds the maximum allowed length for channel: {ChatId}", partIndex, validChatId);
            }
        }

        return await ExecuteWithRetryAsync(
            ct => botClientWrapper.SendMessageAsync(
                    chatId: validChatId,
                    text: part,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyMarkup,
                    cancellationToken: ct),
            $"Message part {partIndex} sent to channel: {validChatId}",
            "Error sending message part to Telegram.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the input contains HTML.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if the input contains HTML, otherwise false.</returns>
    private static bool IsHtml(string input)
    {
        return HtmlRegex().IsMatch(input);
    }

    /// <summary>
    /// Sends an error message to the configured error channel.
    /// </summary>
    /// <param name="message">The content of the error message to be sent.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the message-sending operation.</param>
    /// <returns>True if the message was sent successfully, otherwise false.</returns>
    public async Task<bool> SendErrorMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        return await SendMessageAsync(message, errorChannelId, "error", replyMarkup: null, cancellationToken: cancellationToken)
                     .ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a document file to a Telegram chat.
    /// </summary>
    /// <param name="fileStream">The file stream to be sent.</param>
    /// <param name="fileName">The name of the file being sent.</param>
    /// <param name="chatId">The destination chat ID. If null, the default channel is used.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the file-sending operation.</param>
    /// <returns>True if the file was sent successfully, otherwise false.</returns>
    public async Task<bool> SendDocumentAsync(Stream fileStream, string fileName, string? chatId = null, CancellationToken cancellationToken = default)
    {
        return await SendMediaAsync(fileStream, fileName, chatId,
            (inputFile, validChatId, ct) => botClientWrapper.SendDocumentAsync(
                chatId: validChatId,
                document: inputFile,
                caption: "Attached file from the system",
                cancellationToken: ct),
            "File", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a photo to a Telegram chat.
    /// </summary>
    /// <param name="photoStream">The stream of the photo to be sent.</param>
    /// <param name="chatId">The destination chat ID. If null, the default channel is used.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the photo-sending operation.</param>
    /// <returns>True if the photo was sent successfully, otherwise false.</returns>
    public async Task<bool> SendPhotoAsync(Stream photoStream, string? chatId = null, CancellationToken cancellationToken = default)
    {
        return await SendMediaAsync(photoStream, null, chatId,
            (inputFile, validChatId, ct) => botClientWrapper.SendPhotoAsync(
                chatId: validChatId,
                photo: inputFile,
                caption: "Attached photo from the system",
                cancellationToken: ct),
            "Photo", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Edits a sent message in a Telegram chat.
    /// </summary>
    /// <param name="chatId">The ID of the chat containing the message to be edited.</param>
    /// <param name="messageId">The ID of the message to be edited.</param>
    /// <param name="newMessage">The new content for the message.</param>
    /// <param name="formatType">The message format type, default is "default".</param>
    /// <param name="cancellationToken">Cancellation token to cancel the edit operation.</param>
    /// <returns>True if the message was edited successfully, otherwise false.</returns>
    public async Task<bool> EditMessageAsync(string chatId, int messageId, string newMessage, string formatType = "default", CancellationToken cancellationToken = default)
    {
        string validChatId = ValidateChatId(chatId);
        string formattedMessage = FormatMessage(newMessage, formatType);

        return await ExecuteWithRetryAsync(
            ct => botClientWrapper.EditMessageTextAsync(
                    chatId: validChatId,
                    messageId: messageId,
                    text: formattedMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct),
            $"Message edited in channel: {validChatId}",
            "Error editing message in Telegram.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a video to a Telegram chat.
    /// </summary>
    /// <param name="videoStream">The stream of the video to be sent.</param>
    /// <param name="fileName">The name of the video file being sent.</param>
    /// <param name="chatId">The destination chat ID. If null, the default channel is used.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the video-sending operation.</param>
    /// <returns>True if the video was sent successfully, otherwise false.</returns>
    public async Task<bool> SendVideoAsync(Stream videoStream, string fileName, string? chatId = null, CancellationToken cancellationToken = default)
    {
        return await SendMediaAsync(videoStream, fileName, chatId,
            (inputFile, validChatId, ct) => botClientWrapper.SendVideoAsync(
                chatId: validChatId,
                video: inputFile,
                caption: "Attached video from the system",
                cancellationToken: ct),
            "Video", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a media group (album) to a Telegram chat.
    /// </summary>
    /// <param name="media">The list of media to be sent.</param>
    /// <param name="chatId">The destination chat ID. If null, the default channel is used.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the media-sending operation.</param>
    /// <returns>True if the media group was sent successfully, otherwise false.</returns>
    public async Task<bool> SendMediaGroupAsync(IEnumerable<IAlbumInputMedia> media, string? chatId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);
        string validChatId = ValidateChatId(chatId);

        return await ExecuteWithRetryAsync(
            ct => botClientWrapper.SendMediaGroupAsync(
                    chatId: validChatId,
                    media: media,
                    cancellationToken: ct),
            $"Media group sent to channel: {validChatId}",
            "Error sending media group to Telegram.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules a message to be sent after a specified delay.
    /// </summary>
    /// <param name="message">The content of the message to be sent.</param>
    /// <param name="delay">The delay duration before sending the message.</param>
    /// <param name="chatId">The destination chat ID. If null, the default channel is used.</param>
    /// <param name="formatType">The message format type, default is "default".</param>
    /// <param name="replyMarkup">Optional reply markup to include with the message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the scheduled message.</param>
    /// <returns>True if the message was successfully sent after the delay, otherwise false.</returns>
    public async Task<bool> ScheduleMessageAsync(string message, TimeSpan delay, string? chatId = null, string formatType = "default", ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            return await SendMessageAsync(message, chatId, formatType, replyMarkup, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scheduling message to Telegram.");
            return false;
        }
    }

    /// <summary>
    /// Formats the message based on the configured template.
    /// If no matching format is found, returns the original message.
    /// </summary>
    /// <param name="message">The content of the message to format.</param>
    /// <param name="formatType">The type of message formatting.</param>
    /// <returns>The formatted message string.</returns>
    private string FormatMessage(string message, string formatType)
    {
        if (messageFormats.TryGetValue(formatType, out string? format))
        {
            return string.Format(format, message, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        return message;
    }

    /// <summary>
    /// Validates the chatId. If chatId is null or empty, uses the default configured channel.
    /// </summary>
    /// <param name="chatId">The chat ID to validate.</param>
    /// <returns>A valid chat ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a valid channel ID is not found.</exception>
    private string ValidateChatId(string? chatId)
    {
        chatId ??= channelId;
        if (string.IsNullOrWhiteSpace(chatId))
        {
            throw new InvalidOperationException("Channel ID not found!");
        }
        return chatId;
    }

    /// <summary>
    /// Checks if the stream is readable and resets its position to 0 if it is seekable.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not readable.</exception>
    private static void PrepareStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new InvalidOperationException("Stream is not readable.");
        }
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    /// <summary>
    /// Helper method to abstract retry logic for sending or editing messages.
    /// Executes the operation with the configured retry policy.
    /// </summary>
    /// <param name="operation">Asynchronous operation to execute.</param>
    /// <param name="successMessage">Log message on success.</param>
    /// <param name="errorMessage">Log message on failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    private async Task<bool> ExecuteWithRetryAsync(Func<CancellationToken, Task> operation, string successMessage, string errorMessage, CancellationToken cancellationToken)
    {
        return await RetryHelper.ExecuteWithRetryAsync(operation, retryPolicy, logger, successMessage, errorMessage, cancellationToken)
                                .ConfigureAwait(false);
    }

    /// <summary>
    /// Helper method to abstract media sending logic for documents, photos, and videos.
    /// Prepares the stream, validates the chat ID, creates an InputFileStream, and executes the send action with retry logic.
    /// </summary>
    /// <param name="stream">The media stream to send.</param>
    /// <param name="fileName">Optional file name for the media.</param>
    /// <param name="chatId">The destination chat ID.</param>
    /// <param name="sendAction">The delegate that sends the media using the InputFileStream and validated chat ID.</param>
    /// <param name="mediaType">A string representing the type of media (e.g., "File", "Photo", "Video").</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the media is sent successfully, otherwise false.</returns>
    private async Task<bool> SendMediaAsync(Stream stream, string? fileName, string? chatId, Func<InputFileStream, string, CancellationToken, Task> sendAction,
        string mediaType, CancellationToken cancellationToken)
    {
        PrepareStream(stream);
        string validChatId = ValidateChatId(chatId);
        InputFileStream inputFile = fileName is null ? new InputFileStream(stream) : new InputFileStream(stream, fileName);
        return await ExecuteWithRetryAsync(
            ct => sendAction(inputFile, validChatId, ct),
            $"{mediaType} sent to channel: {validChatId}",
            $"Error sending {mediaType} to Telegram.",
            cancellationToken).ConfigureAwait(false);
    }
}
