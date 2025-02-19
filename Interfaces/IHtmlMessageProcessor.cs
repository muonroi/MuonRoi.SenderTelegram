namespace MuonRoi.SenderTelegram.Interfaces;
public interface IHtmlMessageProcessor
{
    IEnumerable<string> Process(string htmlMessage, int maxLength);
}