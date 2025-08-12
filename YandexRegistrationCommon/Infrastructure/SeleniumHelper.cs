using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumProxyAuth;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Xsl;
using Titanium.Web.Proxy;
using YandexRegistrationCommon.Infrastructure.APIHelper;
using YandexRegistrationCommon.Infrastructure.Factories;
using YandexRegistrationCommon.Infrastructure.SeleniumAddons;
using YandexRegistrationModel;
using YandexRegistrationModel.Enums;

namespace YandexRegistrationCommon.Infrastructure
{
    public class SeleniumHelper : IDisposable
    {
        private readonly YandexTask _task;
        private readonly RuCaptchaHelper _ruCaptchaHelper = new RuCaptchaHelper();
        private readonly Regex _yandexCodeRegEx = new Regex("[0-9]{3}-?[0-9]{3}");
        private const uint _countSmsRetryes = 3;
        private WebDriverWait _wait;
        private IWebDriver webDriver = null;

        public SeleniumHelper(YandexTask task)
        {
            _task = task;
        }

        public async Task Run(Dispatcher dispathcer, SeleniumProxyServer proxyServer, CancellationToken cancellationToken)
        {
            try
            {
                _task.Status = YandexTaskStatus.Started;
                _task.ErrorMessage = string.Empty;

                webDriver = SeleniumDriverFactory.CreateDriver(_task, proxyServer);

                // Настраиваем ожидание (таймаут 10 секунд)
                _wait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));

                GotoUrl(webDriver);

                if (!_task.IsUserRegistered)
                {
                    IWebElement element;
                    if (_task.IsChromeBrowser)
                    {
                        IWebElement button = null;
                        if ((button = ContainsElement(By.XPath("//button[.//span[contains(text(),'Далее')]]"), false)) != null)
                        {
                            button.Click();

                            Thread.Sleep(TimeSpan.FromSeconds(10));

                            if ((button = ContainsElement(By.XPath("//span[text()='Далее']"), false)) != null)
                            {
                                button.Click();
                            }
                        }

                        string query = "Яндекс";
                        string encodedQuery = Uri.EscapeDataString(query);
                        string url = $"https://yandex.ru/search/?text={encodedQuery}";
                        webDriver.Navigate().GoToUrl(url);
                        webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());

                        if (IsCaptchaView(webDriver))
                        {
                            await SolveCapthcha(_wait, webDriver);
                            webDriver.Navigate().GoToUrl(url);
                            webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());
                        }

                        try
                        {
                            // Ждем пока элемент с определенным селектором появится и станет кликабельным
                            element = _wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//a[.//span[contains(text(),'Получить бонус')]]")));
                            element.Click();
                        }
                        catch (Exception ex)
                        {
                            var result = MessageBox.Show("Не удалось найти кнопку 'Получить бонус'. Кнопка не отобразилась в браузере.\nСделайте так, чтобы отобразилась кнопка \"Получить бонус\" и нажмите её вручную!\nДля продолжения нажмите кнопку OK. Для прерывания задания нажмите кнопку Cancel", "Ошибка", MessageBoxButton.OKCancel, MessageBoxImage.Error, MessageBoxResult.Cancel);
                            if (result == MessageBoxResult.Cancel)
                                throw new ArgumentException("Задание прервано пользователем");
                        }

                        webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());
                    }
                    bool badRegistrationResult;
                    SmsActivateDto smsModelDto; 
                    do
                    {
                        smsModelDto = await _task.SmsService.GetNewPhoneNumber();

                        //Send phone number
                        element = webDriver.FindElement(By.XPath("//input"));
                        //element.Click();
                        element.SendKeys(smsModelDto.Phone.ToString());

                        //Click next button
                        element = _wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("phone-auth-section__submit-button")));
                        element.Click();

                        if ((element = ContainsElement(By.XPath("//div[.//h1[text()='Введите фразу с картинки']]"), false)) != null)
                        {
                            SolvePhoneCaptcha(_wait, webDriver);
                        }

                        //Иногда кнопка "Далее" бывает, иногда нет
                        if ((element = ContainsElement(By.XPath("//button[.//span[contains(text(), 'Далее')]]"), false)) != null)
                            element.Click();

                        bool isSmsIncome = false;
                        for (int i = 1; i < _countSmsRetryes; i++)
                        {
                            var sms = await _task.SmsService.WaitForSms(smsModelDto);
                            if (!string.IsNullOrEmpty(sms))
                            {
                                var code = _yandexCodeRegEx.Match(sms);

                                element = webDriver.FindElement(By.CssSelector("input[data-t='field:input-phoneCode']"));

                                // Вставляем нужный код
                                element.SendKeys(sms);

                                isSmsIncome = true;
                                break;
                            }

                            element = webDriver.FindElement(By.CssSelector("button[data-t='button:default:retry-to-request-code']"));
                            element.Click();
                        }
                        if (!isSmsIncome)
                        {
                            await _task.SmsService.SetSmsBad(smsModelDto.Id);
                            throw new NotFoundException($"СМС с кодом не пришла за {_countSmsRetryes} попытки(-ок)");
                        }
                        badRegistrationResult = webDriver.PageSource.Contains("Нашли один с этим телефоном");
                        if (badRegistrationResult)
                        {
                            _task.SmsService.SetSmsBad(smsModelDto.Id);
                            ContainsElement(By.XPath("//button[text()='Войти другим способом']")).Click();
                        }
                    }
                    while (badRegistrationResult);
                    await _task.SmsService.SetSmsOk(smsModelDto.Id);


                    if ((element = ContainsElement(By.Id("passp-field-firstname"), false)) != null)
                        element.SendKeys(_task.UserNameForRegistration);
                    if ((element = ContainsElement(By.Id("passp-field-lastname"), false)) != null)
                        element.SendKeys(_task.SecondNameForRegistration);

                    if ((element = ContainsElement(By.XPath("//button[.//span[text()='Далее']]"), false)) != null)
                        element.Click();

                    if ((element = ContainsElement(By.XPath("//button[.//span[text()='Создать аккаунт']]"), false)) != null)
                        element.Click();

                    if ((element = ContainsElement(By.ClassName("PermissionCheckbox"))) != null)
                        element.Click();

                    if ((element = ContainsElement(By.XPath("//button[.//span[contains(text(), 'Далее')]]"))) != null)
                        element.Click();

                    if ((element = ContainsElement(By.ClassName("RegSurveyPage-button"))) != null)
                        element.Click();

                    if ((element = webDriver.FindElement(By.XPath("//button[text()='Подтвердить участие']"))) != null)
                        element.Click();

                    if ((element = ContainsElement(By.XPath("//a[text()='На главную']"), false)) != null)
                        element.Click();

                    CloseQeustionTab(webDriver);

                    _task.RegisteredUser = new User()
                    {
                        FirstName = _task.UserNameForRegistration,
                        LastName = _task.SecondNameForRegistration,
                        Phone = smsModelDto?.Phone?.ToString(),
                        RegistrationDate = DateTime.Now
                    };
                }

                webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());
                await Task.Delay(TimeSpan.FromSeconds(10));
                foreach (var request in _task.Queries)
                {
                    IWebElement element;
                    if ((element = ContainsElement(By.ClassName("search3__input-inner-container"), false, false)) == null)
                    {
                        if ((element = ContainsElement(By.ClassName("HeaderForm-InputContainer"), false, false)) == null)
                        {
                            if ((element = ContainsElement(By.XPath("//div[.input]"), false, false)) == null)
                            {
                                element = ContainsElement(By.ClassName("mini-suggest__label"), useWait: false);
                                element.Click();
                                element = ContainsElement(By.ClassName("mini-suggest__row"));
                                element = element.FindElement(By.TagName("input"));
                            }
                        }
                        else
                        {
                            element = element.FindElement(By.TagName("input"));
                        }
                    }
                    else
                    {
                        element = element.FindElement(By.TagName("input"));
                    }
                    element.Clear();
                    element.SendKeys(request.Value);
                    element.SendKeys(Keys.Enter);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                _task.Status = YandexTaskStatus.Ok;
            }
            catch (Exception ex)
            {
                _task.Status = YandexTaskStatus.Error;
                _task.ErrorMessage = CompileException(ex);
            }
            finally
            {
                if (webDriver != null)
                {
                    webDriver.Quit();
                    webDriver.Dispose();
                    GC.Collect();
                }
                if (_task.BrowserUserProfile == null && _task.Status == YandexTaskStatus.Ok)
                    _task.ZipProfile();
            }
        }

        private void CloseQeustionTab(IWebDriver webDriver)
        {
            if (webDriver.WindowHandles.Count < 2)
                webDriver.SwitchTo().NewWindow(WindowType.Tab);
            foreach (var window in webDriver.WindowHandles.ToList())
            {
                webDriver.SwitchTo().Window(window);
                if (webDriver.Title.Contains("опрос"))
                    webDriver.Close();
            }
        }

        private string CompileException(Exception ex)
        {
            var result = new List<string>();
            var stackTrace = new StackTrace(ex);
            result.Add(stackTrace.ToString());
            var currentException = ex;
            while (currentException != null)
            {
                result.Add(currentException.Message);
                currentException = currentException.InnerException;
            }
            return string.Join("\n", result);
        }

        private IWebElement? ContainsElement(By searchResult, bool throwExceptionIfElementNotFound = true, bool useWait = true)
        {
            if (_wait == null)
                throw new NotImplementedException("Не установлен метод ожидания поиска элементов на странице. Обратитесь к разработчику ПО");
            try
            {
                if (useWait)
                    return _wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(searchResult));

                return webDriver.FindElement(searchResult);
            }
            catch (Exception ex)
            {
                if (throwExceptionIfElementNotFound)
                    throw ex;
                return null;
            }
        }

        private bool IsCaptchaView(IWebDriver webDriver) => webDriver.Url.Contains("showcaptcha");

        private async Task SolveCapthcha(WebDriverWait wait, IWebDriver webDriver)
        {
            string currentUrl = webDriver.Url;

            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("CheckboxCaptcha-Inner"))).Click();
            bool isCaptureSolve = false;
            try
            {
                wait.Until(d => d.Url != currentUrl);
                isCaptureSolve = true;
            }
            catch (Exception ex)
            {
                isCaptureSolve = false;
            }
            if (!isCaptureSolve)
            {
                await _ruCaptchaHelper.SolveCapcha(wait, webDriver);
            }
        }

        private async Task SolvePhoneCaptcha(WebDriverWait wait, IWebDriver webDriver)
        {
            await _ruCaptchaHelper.SolvePhoneCapcha(wait, webDriver);
        }

        private void GotoUrl(IWebDriver webDriver)
        {
            webDriver.Navigate().GoToUrl(_task.ReferalUrl);
            //switch (_task.BrowserType)
            //{
            //    case BrowserType.YandexBrowser:
            //        webDriver.Navigate().GoToUrl("https://yandex.ru/portal/set/default_search?retpath=https%3A%2F%2Fyandex.ru%2Fportal%2Fdefsearchpromo%2Fcode&source=6Fh0sP41TKKNn40849&utm_term=---autotargeting&banerid=1200006600&utm_campaign=rsya_promocodes_feb|118152200&utm_medium=rsya&partner_string=mkBRX5h1TnuF632835&from=direct_rsya&yclid=12933870030752841727&utm_content=5545661305|16870771503&utm_source=yandex");
            //        break;
            //    case BrowserType.Chrome:
            //    default:
            //        webDriver.Navigate().GoToUrl("https://yandex.ru/portal/defsearchpromo/landing/ru_mobile400?partner=fYM1bbd1U7yNZ47082&offer_type=dXKT5C51U7yNt47078&utm_source=promocodes_ru&utm_medium=tbank400tel&utm_campaign=200&utm_content=250620250&clckid=6a234ddb");
            //        break;
            //}
        }

        public void Dispose()
        {
            var tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Selenium", $"{_task.Id}"));
            if (tempDirectory.Exists)
                tempDirectory.Delete(true);

            _task.SmsService.Dispose();
        }
    }
}
