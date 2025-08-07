using YandexRegistrationCommon.Infrastructure.Models;

namespace YandexRegistrationCommon.Infrastructure.APIHelper
{
    public interface ISmsActivator : IDisposable
    {
        Task<SmsActivateDto> GetNewPhoneNumber();
        Task SetActivationStatuAwaitSMS(long id);
        Task SetSmsOk(long id);
        Task SetSmsBad(long id);
        Task<string> WaitForSms(SmsActivateDto smsActivateDto, uint timeoutSeconds = 120);
    }
}
