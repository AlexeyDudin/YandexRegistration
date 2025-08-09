using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.APIHelper
{
    public class VacSMSHelper : ISmsActivator
    {
        //private readonly IWebDriver _webDriver = new ChromeDriver();
        private string _mainUrl;
        private readonly string _token;
        private readonly HttpClient _client;

        private const string _service = "ya";
        private const string _country = "ru";

        public string MainUrl
        {
            get => _mainUrl;
            set => _mainUrl = value;
        }

        public VacSMSHelper()
        {
            _token = SettingsHelper.VacSmsToken;
            _client = new HttpClient();
        }

        public void Dispose()
        {
            //if (_webDriver != null)
            //{
            //    _webDriver.Quit();
            //    _webDriver.Dispose();
            //}
            if (_client != null)
            {
                _client.Dispose();
            }
        }

        public async Task<SmsActivateDto> GetNewPhoneNumber()
        {
            var requestUrl = string.Empty;
            if (MainUrl.EndsWith("/"))
                requestUrl = $"{MainUrl}api/getNumber/?apiKey={_token}&service={_service}&country={_country}";
            else
                requestUrl = $"{MainUrl}/api/getNumber/?apiKey={_token}&service={_service}&country={_country}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl)
            {
                Version = HttpVersion.Version11 // указываем HTTP/1.1
            };

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            var parsedResponse = JsonConvert.DeserializeObject<ServiceResponse>(body);
            if (parsedResponse != null)
            {
                return new SmsActivateDto() { Id = parsedResponse.idNum, Phone = parsedResponse.tel };
            }
            return null;
        }

        public async Task SetActivationStatuAwaitSMS(string id)
        {
        }

        public async Task SetSmsBad(string id)
        {
        }

        public async Task SetSmsOk(string id)
        {
        }

        public async Task<string> WaitForSms(SmsActivateDto smsActivateDto, uint timeoutSeconds = 120)
        {
            var requestUrl = string.Empty;
            if (MainUrl.EndsWith("/"))
                requestUrl = $"{MainUrl}api/getSmsCode/?apiKey={_token}&idNum={smsActivateDto.Id}&all";
            else
                requestUrl = $"{MainUrl}/api/getSmsCode/?apiKey={_token}&idNum={smsActivateDto.Id}&all";

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl)
                {
                    Version = HttpVersion.Version11 // указываем HTTP/1.1
                };
                var response = await _client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                var parsedResponse = JsonConvert.DeserializeObject<SmsCodeResponse>(body);
                if (parsedResponse.smsCode != null)
                {
                    return parsedResponse.smsCode.Last();
                }
                await Task.Delay(1000);
            }
            throw new TimeoutException("SMS code not received in time.");
        }
    }

    internal class SmsCodeResponse
    {
        public List<string> smsCode { get; set; }
    }

    internal class ServiceResponse
    {
        public string tel { get; set; }
        public string idNum { get; set; }
    }
}
