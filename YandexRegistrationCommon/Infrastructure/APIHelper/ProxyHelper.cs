using Newtonsoft.Json;
using OpenQA.Selenium;
using SmsActivate.API;
using System.Net;
using System.Net.Http.Headers;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.APIHelper
{
    public class ProxyHelper : IDisposable
    {
        private const uint _period = 7; // days
        private UriBuilder _uriBuilder = new UriBuilder("https://mobileproxy.space/api.html");
        private HttpClient _client = new HttpClient();
        private readonly string _token;

        public ProxyHelper()
        {
            _token = SettingsHelper.ProxyToken;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        public async void RegistrateProxy(YandexTask yandexTask)
        {
            var query = System.Web.HttpUtility.ParseQueryString("");

            var russianCountryId = await GetRussianCountryId();
            var cityId = GetRandomCityId();

            query["command"] = "buyproxy";
            //if (!string.IsNullOrEmpty(yandexTask.Proxy.ProxyId))
            //    query["proxy_id"] = yandexTask.Proxy.ProxyId;
            query["period"] = $"{_period}";
            query["id_country"] = $"{russianCountryId}";
            query["id_city"] = $"{cityId}";
            query["auto_renewal"] = "false";

            _uriBuilder.Query = query.ToString();

            var url = _uriBuilder.ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version11 // Принудительно HTTP/1.1
            };


            var response = await _client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();


        }

        private async Task<string> GetRussianCountryId()
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query["command"] = "get_id_country";
            query["only_avaliable"] = "1";

            _uriBuilder.Query = query.ToString();
            var url = _uriBuilder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version11 // Принудительно HTTP/1.1
            };

            request.Headers.Add("Accept-Language", "ru");

            var response = await _client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<ProxyCountryResponse>(body);
            if (data.status.ToLower() == "ok")
            {
                foreach (var country in data.id_country)
                {
                    if (country.name.Contains("Russia") || country.name.Contains("Росси"))
                        return country.id_country; // Предполагается, что это ID России
                }
            }
            throw new ArgumentException("Ошибка получения списка стран для прокси"); // Замените на реальный ID России из ответа
        }

        private async Task<string> GetRandomCityId()
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query["command"] = "get_id_city";

            _uriBuilder.Query = query.ToString();
            var url = _uriBuilder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version11 // Принудительно HTTP/1.1
            };

            request.Headers.Add("Accept-Language", "ru");

            var response = await _client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<ProxyCityResponse>(body);
            if (data.status.ToLower() == "ok")
            {
                foreach (var city in data.id_city)
                {
                    if (city.Value.Any(p => p.Contains("Russia") || p.Contains("Росси")))
                        return city.Key; // Предполагается, что это ID города в России
                }
            }
            throw new ArgumentException("Ошибка получения списка городов для прокси");
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }

    internal class ProxyCityResponse
    {
        public string status { get; set; }
        public Dictionary<string, List<string>> id_city { get; set; }
    }

    internal class ProxyCountryResponse
    {
        public string status { get; set; }
        public List<ProxyCountry> id_country { get; set; }
    }

    internal class ProxyCountry
    {
        public string id_country { get; set; }
        public string name { get; set; }
        public string iso { get; set; }
    }
}
