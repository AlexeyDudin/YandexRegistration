using Newtonsoft.Json;
using System.Collections.ObjectModel;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure
{
    public static class TaskHelper
    {
#if !DEBUG
        public static int ProcessorThreadsAvailable => Environment.ProcessorCount;
#else
        public static int ProcessorThreadsAvailable = 1;
#endif

        public static Task RunTasks(ObservableCollection<YandexTask> tasks, uint threadCount, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                Parallel.ForEach(tasks, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount, CancellationToken = cancellationToken }, async (task) =>
                {
                    using (var seleniumHelper = new SeleniumHelper(task))
                    {
                        await seleniumHelper.Run(cancellationToken);
                    }
                });
            }, cancellationToken);
        }

        public static ObservableCollection<YandexTask> ReadTasksFromFile(string path)
        {
            var text = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ObservableCollection<YandexTask>>(text);
        }

        public static void SaveTasksToFile(string fileName, ObservableCollection<YandexTask> yandexTasks)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(yandexTasks));
        }
    }
}
