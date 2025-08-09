using Newtonsoft.Json;
using YandexRegistrationCommon.Infrastructure.Models;

namespace YandexRegistrationCommon.Infrastructure
{
    public static class SettingsHelper
    {
        private const string _settingFilePath = "settings.json";
        private static Setting _setting = null;

        private static void LoadSetting()
        {
            try
            {
                if (File.Exists(_settingFilePath))
                {
                    var readedText = File.ReadAllText(_settingFilePath);
                    _setting = JsonConvert.DeserializeObject<Setting>(readedText);
                }
                else throw new FileNotFoundException();
            }
            catch (Exception ex)
            {
                _setting = new Setting();
                File.WriteAllText(_settingFilePath, JsonConvert.SerializeObject(_setting));
            }
        }

        public static string SmsActivateToken 
        {
            get
            {
                if (_setting == null)
                    LoadSetting();
                return _setting.SmsActivateToken;
            }
        }

        public static string ProxyToken
        {
            get 
            {
                if (_setting == null)
                    LoadSetting();
                return _setting.ProxyToken;
            }
        }

        public static string RuCaptchaToken
        {
            get
            {
                if (_setting == null)
                    LoadSetting();
                return _setting.RuCaptchaToken;
            }
        }

        public static string VacSmsToken
        {
            get
            {
                if (_setting == null)
                    LoadSetting();
                return _setting.VacSmsToken;
            }
        }
    }
}
