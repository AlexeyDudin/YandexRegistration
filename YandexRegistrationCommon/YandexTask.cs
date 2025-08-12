using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YandexRegistrationModel.Enums;
using Newtonsoft.Json;
using YandexRegistrationCommon;

namespace YandexRegistrationModel
{
    public class YandexTask : IDataErrorInfo, INotifyPropertyChanged
    {
        private uint _id;
        private BrowserType _browserType = BrowserType.Chrome;
        private ObservableCollection<Query> _queries = new ObservableCollection<Query>();
        private ProxyParam _proxy = new ProxyParam();
        private YandexTaskStatus _status = YandexTaskStatus.NotStarted;
        private string _errorMessage = string.Empty;
        private string _userNameForRegistration = string.Empty;
        private string _secondNameForRegistration = string.Empty;
        private string _referalUrl = string.Empty;
        private bool _isMobileDevise = true;
        private User _registeredUser = null;
        private string _userAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 7 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.6478.122 Mobile Safari/537.36";
        private ISmsActivator _smsService;
        private byte[] _browserUserProfile = null;

        public YandexTask(uint id)
        {
            _id = id;
            UserNameForRegistration = NameHelper.GetRandomName();
            SecondNameForRegistration = NameHelper.GetRandomSecondName();
            (bool isMobile, string userAgent) newUserAgent = NameHelper.GetRandomUserAgent();
            IsMobileDevise = newUserAgent.isMobile;
            if (!IsMobileDevise)
                BrowserType = BrowserType.YandexBrowser;
            UserAgent = newUserAgent.userAgent;
        }

        public BrowserType BrowserType
        {
            get => _browserType;
            set
            {
                _browserType = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Query> Queries
        {
            get => _queries;
            set
            {
                _queries = value;
                OnPropertyChanged();
            }
        }

        public bool UseProxy
        {
            get => !(Proxy is null);
            set
            {
                if (value)
                    Proxy = new ProxyParam();
                else
                    Proxy = null;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool IsChromeBrowser
        {
            get => BrowserType == BrowserType.Chrome;
            set
            {
                BrowserType = BrowserType.Chrome;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BrowserType));
                OnPropertyChanged(nameof(IsYandexBrowser));
            }
        }

        [JsonIgnore]
        public bool IsYandexBrowser
        {
            get => BrowserType == BrowserType.YandexBrowser;
            set
            {
                BrowserType = BrowserType.YandexBrowser;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BrowserType));
                OnPropertyChanged(nameof(IsChromeBrowser));
            }
        }

        public uint Id => _id;

        [JsonIgnore]
        public string StringId => $"Задание {_id}";

        public YandexTaskStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public User RegisteredUser
        {
            get => _registeredUser;
            set
            {
                _registeredUser = value;
                OnPropertyChanged();
                if (NotifyChangeAction != null)
                    NotifyChangeAction(Id);
            }
        }

        [JsonIgnore]
        public bool IsUserRegistered => RegisteredUser is not null;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string UserNameForRegistration
        {
            get => _userNameForRegistration;
            set
            {
                _userNameForRegistration = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string SecondNameForRegistration
        {
            get => _secondNameForRegistration;
            set
            {
                _secondNameForRegistration = value;
                OnPropertyChanged();
            }
        }

        public string ReferalUrl
        {
            get => _referalUrl;
            set
            {
                _referalUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        [JsonConverter(typeof(YandexTaskConverter))]
        public ISmsActivator SmsService
        {
            get => _smsService;
            set
            {
                _smsService = value;
                OnPropertyChanged();
            }
        }

        public ProxyParam Proxy
        {
            get => _proxy;
            set
            {
                _proxy = value;
                OnPropertyChanged();
            }
        }

        public YandexTask Clone(uint newTaskId)
        {
            (bool isMobile, string userAgent) newUserAgent = NameHelper.GetRandomUserAgent();
            return new YandexTask(newTaskId)
            {
                BrowserType = this.BrowserType,
                Queries = new ObservableCollection<Query>(Queries.Select(q => new Query() { Value = q.Value }).ToList()),
                UseProxy = this.UseProxy,
                UserNameForRegistration = NameHelper.GetRandomName(),
                SecondNameForRegistration = NameHelper.GetRandomSecondName(),
                _referalUrl = this.ReferalUrl,
                SmsService = this.SmsService,
                Proxy = this.Proxy == null ? null : this.Proxy.Clone(),
                NotifyChangeAction = this.NotifyChangeAction,
                IsMobileDevise = newUserAgent.isMobile,
                UserAgent = newUserAgent.userAgent,
            };
        }

        [JsonIgnore]
        public Action<uint> NotifyChangeAction { get; set; }
        public string UserAgent
        {
            get => _userAgent;
            set
            {
                _userAgent = value;
                OnPropertyChanged();
            }
        }

        public bool IsMobileDevise
        {
            get => _isMobileDevise;
            set
            {
                _isMobileDevise = value;
                OnPropertyChanged();
            }
        }

        public byte[] BrowserUserProfile
        {
            get => _browserUserProfile;
            set
            {
                _browserUserProfile = value;
                OnPropertyChanged();
            }
        }

        public string Error => null;

        public bool HasErrors => string.IsNullOrEmpty(ReferalUrl);

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(this.ReferalUrl))
                    if (string.IsNullOrEmpty(ReferalUrl))
                        return "Ссылка не может быть пустой!";
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}
