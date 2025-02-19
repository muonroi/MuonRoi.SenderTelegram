namespace MuonRoi.SenderTelegram.Interfaces;
public interface IRetryPolicyProvider
{
    AsyncRetryPolicy GetPolicy();
}
