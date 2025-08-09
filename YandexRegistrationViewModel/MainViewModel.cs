using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using YandexRegistrationCommon.Infrastructure;
using YandexRegistrationCommon.Infrastructure.APIHelper;
using YandexRegistrationModel;

namespace YandexRegistrationViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<YandexTask> _yandexTasks = new ObservableCollection<YandexTask>();
        private ObservableCollection<GroupingYandexTaskViewModel> _sortedTasks = new ObservableCollection<GroupingYandexTaskViewModel>();
        private YandexTask _selectedTask = null;
        private object _selectedObject = null;
        private uint _maxCountThreads = (uint)TaskHelper.ProcessorThreadsAvailable;
        private uint _countThreads = (uint)TaskHelper.ProcessorThreadsAvailable;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isTaskStarted = false;
        private readonly Dispatcher _dispatcher;

        public MainViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            YandexTasks.CollectionChanged += YandexTasks_CollectionChanged;
        }

        private void YandexTasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (YandexTask task in e.NewItems)
                    {
                        var st = SortedTasks.FirstOrDefault(t => t.Date == task.RegisteredUser?.RegistrationDate.Date);
                        if (st == null)
                        {
                            st = new GroupingYandexTaskViewModel(task.RegisteredUser?.RegistrationDate.Date);
                            SortedTasks.Add(st);
                        }    
                        st.Tasks.Add(task);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (YandexTask task in e.OldItems)
                    {
                        var st = _sortedTasks.FirstOrDefault(t => t.Date == task.RegisteredUser?.RegistrationDate.Date);
                        if (st != null)
                        {
                            st.Tasks.Remove(task);
                            if (st.Tasks.Count == 0)
                            {
                                SortedTasks.Remove(st);
                            }
                        }
                    }
                    break;
            }
            OnPropertyChanged(nameof(SortedTasks));
        }

        #region Properties
        public ObservableCollection<YandexTask> YandexTasks
        {
            get => _yandexTasks;
            set
            {
                _yandexTasks = value;
                YandexTasks.CollectionChanged += YandexTasks_CollectionChanged;
                OnPropertyChanged();
                SortedTasks = new ObservableCollection<GroupingYandexTaskViewModel>();
                if (_yandexTasks != null)
                {
                    foreach (YandexTask task in _yandexTasks)
                    {
                        var st = SortedTasks.FirstOrDefault(t => t.Date == task.RegisteredUser?.RegistrationDate.Date);
                        if (st == null)
                        {
                            st = new GroupingYandexTaskViewModel(task.RegisteredUser?.RegistrationDate);
                            SortedTasks.Add(st);
                        }
                        st.Tasks.Add(task);
                    }
                }
            }
        }

        public ObservableCollection<GroupingYandexTaskViewModel> SortedTasks
        {
            get => _sortedTasks;
            set
            {
                _sortedTasks = value;
                OnPropertyChanged();
            }
        }

        public YandexTask SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SmsServiceUrl));
                OnPropertyChanged(nameof(ManualPhoneNumber));
                OnPropertyChanged(nameof(IsManualSmsService));
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
                OnPropertyChanged(nameof(IsTaskNotStarted));
            }
        }

        public bool IsTaskNotStarted => !IsTaskStarted;

        public string SmsServiceUrl
        {
            get
            {
                if (SelectedTask?.SmsService is VacSMSHelper vacSMSHelper)
                    return vacSMSHelper.MainUrl;
                return null;
            }
            set
            {
                if (SelectedTask?.SmsService is VacSMSHelper vacSMSHelper)
                {
                    vacSMSHelper.MainUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ManualPhoneNumber
        {
            get
            {
                if (SelectedTask?.SmsService is ManualSmsHelper manualSmsHelper)
                    return manualSmsHelper.PhoneNumber;
                return null;
            }
            set
            {
                if (SelectedTask?.SmsService is ManualSmsHelper manualSmsHelper)
                {
                    manualSmsHelper.PhoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsManualSmsService
        {
            get => SelectedTask?.SmsService is ManualSmsHelper;
            set
            {
                if (SelectedTask == null)
                    return;
                if (value)
                {
                    SelectedTask.SmsService = new ManualSmsHelper(_dispatcher);
                }
                else
                {
                    SelectedTask.SmsService = new VacSMSHelper();
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(ManualPhoneNumber));
                OnPropertyChanged(nameof(SmsServiceUrl));
            }
        }

        public object SelectedObject 
        {
            get => _selectedObject;
            set
            {
                _selectedObject = value;
                if (SelectedObject is YandexTask yt)
                    SelectedTask = yt;
                else
                    SelectedTask = null;
            }
        }

        public uint MaxCountThreads => _maxCountThreads;
        #endregion

        #region Functions
        public async void RunTasks()
        {
            IsTaskStarted = true;
            foreach (var task in YandexTasks)
            {
                if (task.SmsService is VacSMSHelper vacSmsHelper)
                {
                    vacSmsHelper.MainUrl = SmsServiceUrl;
                }
            }
            await TaskHelper.RunTasks(YandexTasks, CountThreads, _dispatcher, _cancellationTokenSource.Token);
            IsTaskStarted = false;
        }

        private void UpdateSortedTask(uint taskId)
        {
            YandexTask foundedTask = null;
            foreach (var group in SortedTasks)
            {
                if (group.Tasks.Any(t => t.Id == taskId))
                {
                    foundedTask = group.Tasks.First(t => t.Id == taskId);
                    _dispatcher.Invoke(() =>
                    {
                        group.Tasks.Remove(foundedTask);
                        if (group.Tasks.Count == 0)
                            SortedTasks.Remove(group);
                    });
                    break;
                }
            }
            var gr = SortedTasks.FirstOrDefault(t => t.Date == foundedTask?.RegisteredUser?.RegistrationDate.Date);
            if (gr == null)
            {
                gr = new GroupingYandexTaskViewModel(foundedTask?.RegisteredUser?.RegistrationDate.Date);
                _dispatcher.Invoke(() => SortedTasks.Add(gr));
            }
            _dispatcher.Invoke(() => gr.Tasks.Add(foundedTask));
        }

        public void AddNewTask()
        {
            var taskId = YandexTasks.Count == 0 ? 1 : YandexTasks.OrderBy(t => t.Id).Last().Id + 1;
            YandexTasks.Add(new YandexTask(taskId) { SmsService = new VacSMSHelper(), NotifyChangeAction = UpdateSortedTask });
        }

        public void CloneTask(YandexTask task)
        {
            var taskId = YandexTasks.OrderBy(t => t.Id).Last().Id + 1;
            YandexTasks.Add(task.Clone(taskId));
        }

        public void RemoveTask(YandexTask task)
        {
            YandexTasks.Remove(YandexTasks.First(t => t.Id == task.Id));
            OnPropertyChanged(nameof(SelectedObject));
            OnPropertyChanged(nameof(SelectedTask));
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
