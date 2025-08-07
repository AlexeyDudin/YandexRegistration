using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YandexRegistrationModel.Enums;
using Newtonsoft.Json;

namespace YandexRegistrationModel
{
    public class YandexTask : INotifyPropertyChanged
    {
        private uint _id;
        private BrowserType _browserType = BrowserType.Chrome;
        private ObservableCollection<Query> _queries = new ObservableCollection<Query>();
        private bool _useProxy = true;
        private YandexTaskStatus _status = YandexTaskStatus.NotStarted;
        private string _errorMessage = string.Empty;
        private string _userNameForRegistration = string.Empty;
        private string _secondNameForRegistration = string.Empty;
        private User _registeredUser = null;

        public YandexTask(uint id) => _id = id;

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
            get => _useProxy;
            set
            {
                _useProxy = value;
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

        public YandexTask Clone(uint newTaskId)
        {
            return new YandexTask(newTaskId)
            {
                BrowserType = this.BrowserType,
                Queries = new ObservableCollection<Query>(Queries.Select(q => new Query() { Value = q.Value }).ToList()),
                UseProxy = this.UseProxy,
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}
