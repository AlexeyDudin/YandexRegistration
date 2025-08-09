namespace YandexRegistrationModel
{
    public interface ISmsActivator : IDisposable
    {
        Task<SmsActivateDto> GetNewPhoneNumber();
        Task SetActivationStatuAwaitSMS(string id);
        Task SetSmsOk(string id);
        Task SetSmsBad(string id);
        Task<string> WaitForSms(SmsActivateDto smsActivateDto, uint timeoutSeconds = 120);
    }
}
