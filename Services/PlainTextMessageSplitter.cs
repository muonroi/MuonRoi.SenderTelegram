namespace MuonRoi.SenderTelegram.Services;
public class PlainTextMessageSplitter : IMessageSplitter
{
    public IEnumerable<string> Split(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
        {
            yield break;
        }

        if (message.Length <= maxLength)
        {
            yield return message;
            yield break;
        }

        int start = 0;
        while (start < message.Length)
        {
            int length = Math.Min(maxLength, message.Length - start);
            int breakIndex = FindBreakIndex(message, start, length);
            int chunkLength = breakIndex > start ? breakIndex - start : length;
            yield return message.Substring(start, chunkLength).Trim();
            start += chunkLength;
        }
    }

    private static int FindBreakIndex(string message, int start, int length)
    {
        int breakIndex = message.LastIndexOfAny(new[] { ' ', '\n' }, start + length - 1, length);
        return breakIndex > start ? breakIndex : start + length;
    }
}