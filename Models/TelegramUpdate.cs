

namespace MuonRoi.SenderTelegram.Models;


public record TelegramUpdate(
    [property: JsonProperty("update_id")] long UpdateId,
    [property: JsonProperty("callback_query")] TelegramCallbackQuery? CallbackQuery
);

public record TelegramCallbackQuery(
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("from")] TelegramUser From,
    [property: JsonProperty("message")] TelegramMessage? Message,
    [property: JsonProperty("chat_instance")] string ChatInstance,
    [property: JsonProperty("data")] string Data
);

public record TelegramUser(
    [property: JsonProperty("id")] long Id,
    [property: JsonProperty("is_bot")] bool IsBot,
    [property: JsonProperty("first_name")] string? FirstName,
    [property: JsonProperty("last_name")] string? LastName,
    [property: JsonProperty("username")] string? Username,
    [property: JsonProperty("language_code")] string? LanguageCode
);

public record TelegramMessage(
    [property: JsonProperty("message_id")] long MessageId,
    [property: JsonProperty("sender_chat")] TelegramChatInfo? SenderChat,
    [property: JsonProperty("chat")] TelegramChatInfo Chat,
    [property: JsonProperty("date")] long Date,
    [property: JsonProperty("text")] string? Text,
    [property: JsonProperty("reply_markup")] TelegramReplyMarkup? ReplyMarkup
);

public record TelegramChatInfo(
    [property: JsonProperty("id")] long Id,
    [property: JsonProperty("title")] string? Title,
    [property: JsonProperty("username")] string? Username,
    [property: JsonProperty("type")] string Type
);

public record TelegramReplyMarkup(
    [property: JsonProperty("inline_keyboard")] IReadOnlyList<IReadOnlyList<TelegramInlineKeyboardButton>> InlineKeyboard
);

public record TelegramInlineKeyboardButton(
    [property: JsonProperty("text")] string Text,
    [property: JsonProperty("callback_data")] string CallbackData
);
