using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YandexRegistrationModel
{
    public class User : INotifyPropertyChanged
    {
        private string _phone = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private DateTime _registrationDate;

        public User()
        {
            RegistrationDate = DateTime.Now;
        }

        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
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

        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }
        public string LastName 
        { 
            get => _lastName;
            set
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }

        public DateTime RegistrationDate 
        {
            get => _registrationDate;
            set
            {
                _registrationDate = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}
