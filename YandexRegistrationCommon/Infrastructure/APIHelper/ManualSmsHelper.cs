using System.Windows.Threading;
using YandexRegistationAddon;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.APIHelper
{
    public class ManualSmsHelper : ISmsActivator
    {
        private string _phoneNumber;
        private readonly Dispatcher _dispatcher;

        public ManualSmsHelper(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => _phoneNumber = value;
        }

        public void Dispose()
        {
        }

        public async Task<SmsActivateDto> GetNewPhoneNumber()
        {
            return new SmsActivateDto() { Id = string.Empty, Phone = PhoneNumber };
        }

        public async Task SetActivationStatuAwaitSMS(string id)
        {
        }

        public async Task SetSmsBad(string id)
        { }

        public async Task SetSmsOk(string id)
        { }

        public async Task<string> WaitForSms(SmsActivateDto smsActivateDto, uint timeoutSeconds = 120)
        {
            return _dispatcher.Invoke(() =>
            {
                var smsRequester = new SmsRequester(smsActivateDto.Phone);
                smsRequester.StartStopwatch();
                bool? showDialogResult = smsRequester.ShowDialog();
                if (showDialogResult.HasValue && showDialogResult.Value)
                {
                    return smsRequester.Code;
                }

                return null;
            });
        }
    }
}
