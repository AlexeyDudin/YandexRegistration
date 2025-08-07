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
            DataContext = new MainViewModel();
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
                vModel.CloneTask((YandexTask)TreeViewTasks.SelectedItem);
                //foreach (var dataGridItem in DataGridTasks.Items.<DataGrid>())
                //{
                //    foreach (var task in DataGridTasks.SelectedItems.Cast<YandexTask>().OrderBy(t => t.Id))
                //    {
                //        vModel.CloneTask(task);
                //    }
                //}
            }
        }

        private void RemoveTasksButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vModel)
            {
                vModel.RemoveTask((YandexTask)TreeViewTasks.SelectedItem);
                //foreach (var task in TreeViewTasks.Selected.SelectedItems.Cast<YandexTask>().OrderBy(t => t.Id))
                //{
                //    vModel.RemoveTask(task);
                //}
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
                if (e.NewValue is YandexTask task)
                    vModel.SelectedTask = task;
                else
                    vModel.SelectedTask = null;
            }
        }
    }
}