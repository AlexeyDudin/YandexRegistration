using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YandexRegistrationModel
{
    public class Query : INotifyPropertyChanged
    {
        private string _value;

        public string Value 
        { 
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}
