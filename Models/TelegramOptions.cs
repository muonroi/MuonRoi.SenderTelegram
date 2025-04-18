﻿namespace MuonRoi.SenderTelegram.Models;
public class TelegramOptions
{
    public string ChannelId { get; set; } = string.Empty;
    public string ErrorChannelId { get; set; } = string.Empty;
    public Dictionary<string, string> Formats { get; set; } = [];
    public int MaxMessageLength { get; set; } = 4096;
    public string BotToken { get; set; } = string.Empty;

    public int MaxRetryAttempts { get; set; } = 3;
}
