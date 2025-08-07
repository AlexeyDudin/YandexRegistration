namespace YandexRegistrationCommon.Infrastructure.Models
{
    public class Setting
    {
        public string SmsActivateToken { get; set; } = string.Empty;
        public string ProxyToken { get; set; } = string.Empty;
        public string RuCaptchaToken { get; set; } = string.Empty;
    }
}
