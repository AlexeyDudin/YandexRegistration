using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using YandexRegistrationCommon.Infrastructure;
using YandexRegistrationModel;

namespace YandexRegistrationViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<YandexTask> _yandexTasks = new ObservableCollection<YandexTask>();
        private YandexTask _selectedTask = null;
        private uint _countThreads = (uint)TaskHelper.ProcessorThreadsAvailable;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isTaskStarted = false;

        public MainViewModel()
        {
            YandexTasks.CollectionChanged += YandexTasks_CollectionChanged;
        }

        private void YandexTasks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SortedTasks));
        }

        #region Properties
        public ObservableCollection<YandexTask> YandexTasks
        {
            get => _yandexTasks;
            set
            {
                _yandexTasks = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SortedTasks));
            }
        }

        public ObservableCollection<IGrouping<DateTime?, YandexTask>> SortedTasks => new ObservableCollection<IGrouping<DateTime?, YandexTask>>(YandexTasks.GroupBy(t => t?.RegisteredUser?.RegistrationDate));
        public YandexTask SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged();
            }
        }

        public uint CountThreads
        {
            get => _countThreads;
            set
            {
                _countThreads = value;
                OnPropertyChanged();
            }
        }

        public bool IsTaskStarted
        {
            get => _isTaskStarted;
            set
            {
                _isTaskStarted = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Functions
        public async void RunTasks()
        {
            await TaskHelper.RunTasks(YandexTasks, CountThreads, _cancellationTokenSource.Token);
        }

        public void AddNewTask()
        {
            var taskId = YandexTasks.Count == 0 ? 1 : YandexTasks.OrderBy(t => t.Id).Last().Id + 1;
            YandexTasks.Add(new YandexTask(taskId));
        }

        public void CloneTask(YandexTask task)
        {
            var taskId = YandexTasks.OrderBy(t => t.Id).Last().Id + 1;
            YandexTasks.Add(task.Clone(taskId));
        }

        public void RemoveTask(YandexTask task)
        {
            YandexTasks.Remove(YandexTasks.First(t => t.Id == task.Id));
        }

        public void StopTasks()
        {
            _cancellationTokenSource.Cancel();
        }

        public void LoadFromFile(string fileName)
        {
            YandexTasks = TaskHelper.ReadTasksFromFile(fileName);
        }

        public void SaveToFile(string fileName)
        {
            TaskHelper.SaveTasksToFile(fileName, YandexTasks);
        }
        #endregion

        #region INotifyPropertyChangedRegion
        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}
