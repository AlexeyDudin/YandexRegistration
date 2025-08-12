using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.DevTools;
using SeleniumProxyAuth;
using System.IO.Compression;
using System.Windows.Media.Media3D;
using YandexRegistrationCommon.Infrastructure.SeleniumAddons;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.Factories
{
    public static class SeleniumDriverFactory
    {
        public static IWebDriver CreateDriver(YandexTask task, SeleniumProxyServer proxyServer)
        {
            var options = new ChromeOptions();
            bool isSettingsSet = false;
            var deviceSettings = new ChromiumMobileEmulationDeviceSettings()
            {
            };
            if (task.IsMobileDevise)
            {
                deviceSettings.Width = 576;
                deviceSettings.Height = 1024;
                deviceSettings.PixelRatio = 3.0;
                isSettingsSet = true;
            }
            if (!string.IsNullOrEmpty(task.UserAgent))
            {
                deviceSettings.UserAgent = task.UserAgent;
                isSettingsSet = true;
            }
            if (isSettingsSet)
                options.EnableMobileEmulation(deviceSettings);

            if (!string.IsNullOrEmpty(task.UserAgent))
            {
                options.AddArgument($"--user-agent={task.UserAgent}");
            }
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            switch (task.BrowserType)
            {
                case YandexRegistrationModel.Enums.BrowserType.YandexBrowser:
                    var ttt = "C:\\Users\\dudin\\AppData\\Local\\Yandex\\YandexBrowser\\Application\\browser.exe";
                    var yandexBrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yandex\\YandexBrowser\\Application\\browser.exe");
                    if (!File.Exists(yandexBrowserPath))
                        throw new FileNotFoundException("Яндекс браузер не установлен!");
                    options.BinaryLocation = yandexBrowserPath;
                    string yandexProfilePath = string.Empty;
                    if (task.BrowserUserProfile != null)
                        yandexProfilePath = task.ExtractProfile();
                    else
                        yandexProfilePath = CloneProfile(task);
                    options.AddArgument($"--user-data-dir={yandexProfilePath}"); // Путь к папке с профилем
                    options.AddArgument("--profile-directory=Default");
                    if (task.UseProxy)
                    {
                        var url = $"{task.Proxy.Url}:{task.Proxy.Port}";
                        options.AddArgument($"--proxy-server={url}");
                    }
                    var yandexDriver = new ChromeDriver(Path.Combine("Infrastructure", "Binary", "Yandex"), options);
                    if (task.UseProxy)
                    {
                        try
                        {
                            yandexDriver.SwitchTo().NewWindow(WindowType.Tab);
                            yandexDriver.SwitchTo().Window(yandexDriver.WindowHandles.First());
                            yandexDriver.Close();
                            yandexDriver.SwitchTo().Window(yandexDriver.WindowHandles.First());
                            var networkAuthenticationHandler = new NetworkAuthenticationHandler
                            {
                                UriMatcher = uri => true,
                                Credentials = new PasswordCredentials(task.Proxy.Login, task.Proxy.Password)
                            };
                            var networkInterceptor = yandexDriver?.Manage()?.Network;
                            networkInterceptor.AddAuthenticationHandler(networkAuthenticationHandler);
                            networkInterceptor.StartMonitoring();
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    if (task.BrowserUserProfile == null)
                        yandexDriver.Manage().Cookies.DeleteAllCookies();
                    return yandexDriver;
                case YandexRegistrationModel.Enums.BrowserType.Chrome:
                default:
                    string chromeProfilePath = string.Empty;
                    if (task.BrowserUserProfile != null)
                        chromeProfilePath = task.ExtractProfile();
                    else
                        chromeProfilePath = CloneProfile(task);
                    options.AddArgument($"--user-data-dir={chromeProfilePath}"); // Путь к папке с профилем
                    options.AddArgument("--profile-directory=Profile 2");
                    options.AddArgument("--disable-blink-features=AutomationControlled");
                    options.AddArgument("ignore-certificate-errors");
                    if (task.UseProxy)
                    {
                        var url = $"{task.Proxy.Url}:{task.Proxy.Port}";
                        options.AddArgument($"--proxy-server={url}");
                    }
                    var driver = new ChromeDriver(Path.Combine("Infrastructure", "Binary", "Chrome"), options);
                    if (task.UseProxy)
                    {
                        try
                        {
                            driver.SwitchTo().NewWindow(WindowType.Tab);
                            driver.SwitchTo().Window(driver.WindowHandles.First());
                            driver.Close();
                            driver.SwitchTo().Window(driver.WindowHandles.First());
                            var networkAuthenticationHandler = new NetworkAuthenticationHandler
                            {
                                UriMatcher = uri => true,
                                Credentials = new PasswordCredentials(task.Proxy.Login, task.Proxy.Password)
                            };
                            var networkInterceptor = driver?.Manage()?.Network;
                            networkInterceptor.AddAuthenticationHandler(networkAuthenticationHandler);
                            networkInterceptor.StartMonitoring();
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    if (task.BrowserUserProfile == null)
                        driver.Manage().Cookies.DeleteAllCookies();
                    return driver;
            }
        }

        private static string CloneProfile(YandexTask task)
        {
            var sourceDirectory = new DirectoryInfo(Path.GetFullPath(Path.Combine("Profiles", task.IsChromeBrowser ? "G" : "Y")));
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
    }
}
