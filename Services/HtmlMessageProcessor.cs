namespace MuonRoi.SenderTelegram.Services;
public class HtmlMessageProcessor(string wrapperTag = "div") : IHtmlMessageProcessor
{
    private readonly string wrapperTag = wrapperTag;

    public IEnumerable<string> Process(string htmlMessage, int maxLength)
    {
        if (string.IsNullOrEmpty(htmlMessage))
        {
            yield break;
        }

        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(htmlMessage);

        StringBuilder currentChunk = new();

        foreach (HtmlNode node in htmlDoc.DocumentNode.ChildNodes)
        {
            foreach (string part in ProcessHtmlNode(node, maxLength))
            {
                if (currentChunk.Length + part.Length > maxLength && currentChunk.Length > 0)
                {
                    yield return WrapHtml(currentChunk.ToString());
                    currentChunk.Clear();
                }

                if (part.Length > maxLength)
                {
                    foreach (string subPart in SplitLongHtmlPart(part, maxLength))
                    {
                        if (currentChunk.Length + subPart.Length > maxLength)
                        {
                            yield return WrapHtml(currentChunk.ToString());
                            currentChunk.Clear();
                        }
                        currentChunk.Append(subPart);
                    }
                }
                else
                {
                    currentChunk.Append(part);
                }
            }
        }

        if (currentChunk.Length > 0)
        {
            yield return WrapHtml(currentChunk.ToString());
        }
    }

    private static IEnumerable<string> ProcessHtmlNode(HtmlNode node, int maxLength)
    {
        string nodeHtml = node.OuterHtml;
        if (nodeHtml.Length <= maxLength)
        {
            yield return nodeHtml;
        }
        else if (node.NodeType == HtmlNodeType.Element)
        {
            foreach (string chunk in SplitText(node.InnerText, maxLength))
            {
                yield return $"<{node.Name}>{WebUtility.HtmlEncode(chunk)}</{node.Name}>";
            }
        }
        else
        {
            yield return nodeHtml;
        }
    }

    private static IEnumerable<string> SplitLongHtmlPart(string htmlPart, int maxLength)
    {
        int start = 0;
        while (start < htmlPart.Length)
        {
            int length = Math.Min(maxLength, htmlPart.Length - start);
            int breakIndex = FindBreakIndex(htmlPart, start, length);
            int chunkLength = breakIndex > start ? breakIndex - start : length;
            yield return htmlPart.Substring(start, chunkLength).Trim();
            start += chunkLength;
        }
    }

    private static IEnumerable<string> SplitText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxLength, text.Length - start);
            int breakIndex = text.LastIndexOfAny([' ', '\n'], start + length - 1, length);
            int chunkLength = breakIndex > start ? breakIndex - start : length;
            yield return text.Substring(start, chunkLength).Trim();
            start += chunkLength;
        }
    }

    private static int FindBreakIndex(string message, int start, int length)
    {
        int breakIndex = message.LastIndexOfAny([' ', '\n'], start + length - 1, length);
        return breakIndex > start ? breakIndex : start + length;
    }

    private string WrapHtml(string innerHtml)
    {
        return $"<{wrapperTag}>{innerHtml}</{wrapperTag}>";
    }
}