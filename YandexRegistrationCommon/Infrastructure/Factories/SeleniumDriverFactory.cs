using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.Factories
{
    public static class SeleniumDriverFactory
    {
        public static IWebDriver CreateDriver(YandexTask task)
        {
            var options = new ChromeOptions();
            switch (task.BrowserType)
            {
                case YandexRegistrationModel.Enums.BrowserType.YandexBrowser:
                    var ttt = "C:\\Users\\dudin\\AppData\\Local\\Yandex\\YandexBrowser\\Application\\browser.exe";
                    var yandexBrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yandex\\YandexBrowser\\Application\\browser.exe");
                    if (!File.Exists(yandexBrowserPath))
                        throw new FileNotFoundException("Яндекс браузер не установлен!");
                    options.BinaryLocation = yandexBrowserPath;
                    return new ChromeDriver(options);
                case YandexRegistrationModel.Enums.BrowserType.Chrome:
                default:
                    string chromeProfilePath = CloneProfile(task);
                    options.AddArgument($"--user-data-dir={chromeProfilePath}"); // Путь к папке с профилем
                    options.AddArgument("--profile-directory=Profile 2");
                    options.AddArgument("--disable-blink-features=AutomationControlled");
                    return new ChromeDriver(options);
            }
        }

        private static string CloneProfile(YandexTask task)
        {
            var sourceDirectory = new DirectoryInfo(Path.GetFullPath(Path.Combine("Infrastructure", "BrowserProfile")));
            var targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Selenium", $"{task.Id}"));
            if (targetDirectory.Exists)
                targetDirectory.Delete(true);
            targetDirectory.Create();
            foreach (var subDirectory in sourceDirectory.EnumerateDirectories("*.*", SearchOption.AllDirectories))
            {
                targetDirectory.CreateSubdirectory(subDirectory.FullName.Replace(sourceDirectory.FullName + Path.DirectorySeparatorChar, ""));
            }

            foreach (var file in sourceDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories))
            {
                file.CopyTo(file.FullName.Replace(sourceDirectory.FullName, targetDirectory.FullName));
            }

            return targetDirectory.FullName;
        }

        private static void IncludeExtensions(ChromeOptions options)
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Infrastructure", "BrowserExtension"));
            if (!directoryInfo.Exists)
                return;
            options.AddArgument($"--load-extension={directoryInfo.FullName}");
            foreach (var crxFile in directoryInfo.EnumerateFiles("*.crx", SearchOption.AllDirectories))
            {
                options.AddExtension(crxFile.FullName);
            }
        }
    }
}
