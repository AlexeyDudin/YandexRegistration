using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YandexRegistrationModel;

namespace YandexRegistrationViewModel
{
    public class GroupingYandexTaskViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<YandexTask> _tasks = new ObservableCollection<YandexTask>();
        private DateTime? _date;
        public GroupingYandexTaskViewModel(DateTime? date)
        {
            Date = date;
        }

        public DateTime? Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<YandexTask> Tasks
        {
            get => _tasks;
            set
            {
                _tasks = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
