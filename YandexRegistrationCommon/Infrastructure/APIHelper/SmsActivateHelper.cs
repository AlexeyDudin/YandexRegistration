using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Text.RegularExpressions;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.APIHelper
{
    public class SmsActivateHelper : ISmsActivator
    {
        private readonly string _token;
        private const string _service = "ya";
        private const ushort _country = 0;
        private readonly IWebDriver _webDriver;
        private Regex _phoneRegEx = new Regex("[0-9]+");
        private string _getPhoneNumberUrl => $"https://api.sms-activate.ae/stubs/handler_api.php?api_key={_token}&action=getNumber&service={_service}&country={_country}";

        public SmsActivateHelper()
        {
            _token = SettingsHelper.SmsActivateToken;
            _webDriver = new ChromeDriver(Path.Combine("Infrastructure", "Binary", "Chrome"));
        }

        public async Task<SmsActivateDto> GetNewPhoneNumber()
        {
            _webDriver.Navigate().GoToUrl(_getPhoneNumberUrl);
            if (_webDriver.PageSource.Contains("ACCESS_NUMBER"))
            {
                var splitedResultParams = _webDriver.PageSource.Split(':');
                if (splitedResultParams.Length == 3)
                {
                    var result = new SmsActivateDto();
                    result.Id = splitedResultParams[1];
                    result.Phone = _phoneRegEx.Match(splitedResultParams[2]).Value;
                    return result;
                }
            }

            throw new ApplicationException(_webDriver.FindElement(By.TagName("body")).Text);
        }

        public async Task SetActivationStatuAwaitSMS(string id)
        {
            await Task.Run(() => _webDriver.Navigate().GoToUrl($"https://api.sms-activate.ae/stubs/handler_api.php?api_key={_token}&action=setStatus&status=1&id={id}"));
        }

        public async Task SetSmsOk(string id)
        {
            await Task.Run(() => _webDriver.Navigate().GoToUrl($"https://api.sms-activate.ae/stubs/handler_api.php?api_key={_token}&action=setStatus&status=6&id={id}"));
        }

        public async Task SetSmsBad(string id)
        {
            await Task.Run(() => _webDriver.Navigate().GoToUrl($"https://api.sms-activate.ae/stubs/handler_api.php?api_key={_token}&action=setStatus&status=8&id={id}"));
        }

        public async Task<string> WaitForSms(SmsActivateDto smsActivateDto, uint timeoutSeconds = 120)
        {
            string _getSmsUrl = $"https://api.sms-activate.ae/stubs/handler_api.php?api_key={_token}&action=getStatus&id={smsActivateDto.Id}";
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                _webDriver.Navigate().GoToUrl(_getSmsUrl);
                var status = _webDriver.FindElement(By.TagName("body")).Text;
                if (status.StartsWith("STATUS_OK:"))
                {
                    // Возвращаем код SMS
                    return status.Split(':')[1];
                }
                else if (status.StartsWith("STATUS_WAIT_CODE") || status.StartsWith("STATUS_WAIT_RETRY"))
                {
                    await Task.Delay(5000);
                    continue;
                }
                else
                {
                    throw new Exception($"Unexpected status: {status}");
                }
            }
            throw new TimeoutException("SMS code not received in time.");
        }

        public void Dispose()
        {
            if (_webDriver is not null)
            {
                _webDriver.Quit();
                _webDriver.Dispose();
            }
        }
    }
}
