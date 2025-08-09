using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using YandexRegistrationModel;
using YandexRegistrationViewModel;

namespace YandexRegistration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(Dispatcher);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vModel)
            {
                vModel.RunTasks();
            }
        }

        private void NewTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vModel)
            {
                vModel.AddNewTask();
            }
        }

        private void CloneTasksButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vModel)
            {
                if (vModel.SelectedObject is YandexTask task)
                    vModel.CloneTask(task);
                else if (vModel.SelectedObject is GroupingYandexTaskViewModel gTasks)
                {
                    foreach (var t in gTasks.Tasks.ToList())
                    {
                        vModel.CloneTask(t);
                    }
                }
            }
        }

        private void RemoveTasksButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vModel)
            {
                if (vModel.SelectedObject is YandexTask)
                {
                    vModel.RemoveTask(vModel.SelectedTask);
                }
                else if (vModel.SelectedObject is GroupingYandexTaskViewModel gTasks)
                {
                    var messageBox = MessageBox.Show("Вы действительно хотите удалить группу заданий?", "Удалить?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    if (messageBox == MessageBoxResult.Yes)
                    {
                        while (gTasks != null && gTasks.Tasks.Count != 0)
                        {
                            var task = gTasks.Tasks[0];
                            vModel.RemoveTask(task);
                        }
                        vModel.SelectedObject = null;
                    }
                }
            }
        }

        private void OpenTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                DefaultExt = ".json|JSON"
            };
            bool? dialogResult;
            if ((dialogResult = openFileDialog.ShowDialog()).HasValue && dialogResult.Value && DataContext is MainViewModel vModel)
            {
                vModel.LoadFromFile(openFileDialog.FileName);
            }
        }

        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            SaveTasks();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveTasks();
        }

        private void SaveTasks()
        {
            var saveFileDialog = new SaveFileDialog()
            {
                CheckFileExists = false,
                DefaultExt = ".json"
            };
            bool? dialogResult;
            if ((dialogResult = saveFileDialog.ShowDialog()).HasValue && dialogResult.Value && DataContext is MainViewModel vModel)
            {
                vModel.SaveToFile(saveFileDialog.FileName);
            }
        }

        private void TreeViewTasks_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vModel)
            {
                vModel.SelectedObject = e.NewValue;
            }
        }
    }
}