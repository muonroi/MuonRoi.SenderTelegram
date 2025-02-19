namespace MuonRoi.SenderTelegram.Interfaces;
public interface IMessageSplitter
{
    IEnumerable<string> Split(string message, int maxLength);
}