using Newtonsoft.Json;
using SeleniumProxyAuth;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using YandexRegistrationCommon.Infrastructure.APIHelper;
using YandexRegistrationModel;
using YandexRegistrationModel.Enums;

namespace YandexRegistrationCommon.Infrastructure
{
    public static class TaskHelper
    {
#if !DEBUG
        public static int ProcessorThreadsAvailable => Environment.ProcessorCount;
#else
        public static int ProcessorThreadsAvailable = 1;
#endif

        public static Task RunTasks(ObservableCollection<YandexTask> tasks, uint threadCount, Dispatcher dispatcher, CancellationToken cancellationToken)
        {
            var proxyServer = new SeleniumProxyServer();
            return Task.Run(() =>
            {
                Parallel.ForEach(tasks, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount, CancellationToken = cancellationToken }, async (task) =>
                {
                    using (var seleniumHelper = new SeleniumHelper(task))
                    {
                        await seleniumHelper.Run(dispatcher, proxyServer, cancellationToken);
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
