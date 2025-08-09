using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YandexRegistrationModel
{
    public class ProxyParam : INotifyPropertyChanged
    {
        private string _url = string.Empty;
        private ushort _port = 8000;
        private string _login = string.Empty;
        private string _password = string.Empty;

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }
        public ushort Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged();
            }
        }
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public ProxyParam Clone()
        {
            return new ProxyParam()
            {
                Url = this.Url,
                Port = this.Port,
                Login = this.Login,
                Password = this.Password
            };
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
