using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chrome.ChromeDriverExtensions;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumProxyAuth;
using System.IO.Compression;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.Factories
{
    public static class SeleniumDriverFactory
    {
        public static IWebDriver CreateDriver(YandexTask task, SeleniumProxyServer proxyServer)
        {
            var options = new ChromeOptions();
            options.AddArgument($"--user-agent={task.UserAgent}");
            switch (task.BrowserType)
            {
                case YandexRegistrationModel.Enums.BrowserType.YandexBrowser:
                    var ttt = "C:\\Users\\dudin\\AppData\\Local\\Yandex\\YandexBrowser\\Application\\browser.exe";
                    var yandexBrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yandex\\YandexBrowser\\Application\\browser.exe");
                    if (!File.Exists(yandexBrowserPath))
                        throw new FileNotFoundException("Яндекс браузер не установлен!");
                    options.BinaryLocation = yandexBrowserPath;
                    //if (task.UseProxy)
                    //{
                    //    proxyServer = new SeleniumProxyServer();
                    //    var localPort = proxyServer.AddEndpoint(new ProxyAuth(task.Proxy.Url, task.Proxy.Port, task.Proxy.Login, task.Proxy.Password));
                    //    options.Proxy = new Proxy() { HttpProxy = $"127.0.0.1:{localPort}", SslProxy = $"127.0.0.1:{localPort}" };
                    //}
                    return new ChromeDriver(Path.Combine("Infrastructure", "Binary", "Yandex"), options);
                case YandexRegistrationModel.Enums.BrowserType.Chrome:
                default:
                    string chromeProfilePath = CloneProfile(task);
                    options.AddArgument($"--user-data-dir={chromeProfilePath}"); // Путь к папке с профилем
                    options.AddArgument("--profile-directory=Profile 2");
                    options.AddArgument("--disable-blink-features=AutomationControlled");
                    options.AddArgument("ignore-certificate-errors");
                    if (task.UseProxy)
                    {
                        //options.AddHttpProxy(task.Proxy.Url, task.Proxy.Port, task.Proxy.Login, task.Proxy.Password);

                        var url = $"{task.Proxy.Url}:{task.Proxy.Port}";
                        options.AddArgument($"--proxy-server={url}");

                        //var localPort = proxyServer.AddEndpoint(new ProxyAuth(task.Proxy.Url, Convert.ToInt32(task.Proxy.Port), task.Proxy.Login, task.Proxy.Password));
                        //options.AddArgument($"--proxy-server=127.0.0.1:{localPort}");

                        //options.AddProxyAuthentication(task.Proxy.Login, task.Proxy.Password);
                        //    options.AddProxyAuthenticationExtension(new SeleniumProxyAuthentication.Proxy(
                        //ProxyProtocols.HTTPS,
                        //$"{task.Proxy.Url}:{task.Proxy.Port}:{task.Proxy.Login}:{task.Proxy.Password}"
                        //));

                        //proxyServer = new SeleniumProxyServer();
                        //var localPort = proxyServer.AddEndpoint(new ProxyAuth(task.Proxy.Url, task.Proxy.Port, task.Proxy.Login, task.Proxy.Password));
                        //options.Proxy = new Proxy() { HttpProxy = $"127.0.0.1:{localPort}", SslProxy = $"127.0.0.1:{localPort}" };
                    }
                    var driver = new ChromeDriver(Path.Combine("Infrastructure", "Binary", "Chrome"), options);
                    if (task.UseProxy)
                    {
                        //try
                        //{
                        //    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        //    IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());
                        //    alert.SetAuthenticationCredentials(task.Proxy.Login, task.Proxy.Password);
                        //}
                        //catch (Exception ex)
                        //{
                        //}
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
                    return driver;
            }
        }

        private static string CreateProxy(YandexTask task)
        {
            // Создаём папку для расширения
            string extensionPath = Path.Combine(Directory.GetCurrentDirectory(), "proxy_auth_extension");
            if (Directory.Exists(extensionPath))
                Directory.Delete(extensionPath, true);
            Directory.CreateDirectory(extensionPath);

            // Манифест расширения
            string manifestJson = @"{
            ""version"": ""1.0.0"",
            ""manifest_version"": 2,
            ""name"": ""Chrome Proxy"",
            ""permissions"": [
                ""proxy"",
                ""tabs"",
                ""unlimitedStorage"",
                ""storage"",
                ""<all_urls>"",
                ""webRequest"",
                ""webRequestBlocking""
            ],
            ""background"": {
                ""scripts"": [""background.js""]
            },
            ""minimum_chrome_version"":""22.0.0""
        }";

            File.WriteAllText(Path.Combine(extensionPath, "manifest.json"), manifestJson);

            // background.js - устанавливает прокси и логин/пароль
            string backgroundJs = $@"
var config = {{
    mode: 'fixed_servers',
    rules: {{
        singleProxy: {{
            scheme: 'http',
            host: '{task.Proxy.Url}',
            port: {task.Proxy.Port}
        }},
        bypassList: ['localhost']
    }}
}};

chrome.proxy.settings.set({{value: config, scope: 'regular'}}, function() {{}});

function callbackFn(details) {{
    return {{
        authCredentials: {{
            username: '{task.Proxy.Login}',
            password: '{task.Proxy.Password}'
        }}
    }};
}}

chrome.webRequest.onAuthRequired.addListener(
    callbackFn,
    {{urls: ['<all_urls>']}},
    ['blocking']
);
";

            File.WriteAllText(Path.Combine(extensionPath, "background.js"), backgroundJs);

            // Упаковываем расширение в .crx или загружаем как папку
            // Selenium ChromeOptions умеет загружать папки расширений

            var options = new ChromeOptions();
            options.AddArguments("start-maximized");
            options.AddArguments("disable-infobars");
            //options.AddExtension(extensionPath);

            // Но AddExtension работает с файлами .crx или .zip, поэтому нужно упаковать в zip
            string zipPath = Path.Combine(Directory.GetCurrentDirectory(), "proxy_auth_extension.zip");
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(extensionPath, zipPath);
            return zipPath;
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
