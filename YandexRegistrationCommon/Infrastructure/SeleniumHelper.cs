using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumProxyAuth;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Titanium.Web.Proxy;
using YandexRegistrationCommon.Infrastructure.APIHelper;
using YandexRegistrationCommon.Infrastructure.Factories;
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

        public SeleniumHelper(YandexTask task)
        {
            _task = task;
        }

        public async Task Run(Dispatcher dispathcer, SeleniumProxyServer proxyServer, CancellationToken cancellationToken)
        {
            IWebDriver webDriver = null;
            try
            {
                _task.Status = YandexTaskStatus.Started;
                _task.ErrorMessage = string.Empty;

                webDriver = SeleniumDriverFactory.CreateDriver(_task, proxyServer);

                // Настраиваем ожидание (таймаут 10 секунд)
                WebDriverWait wait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));

                GotoUrl(webDriver);

                if (!_task.IsUserRegistered)
                {
                    IWebElement element;
                    if (_task.IsChromeBrowser)
                    {
                        string query = "Яндекс";
                        string encodedQuery = Uri.EscapeDataString(query);
                        string url = $"https://yandex.ru/search/?text={encodedQuery}";
                        webDriver.Navigate().GoToUrl(url);

                        if (IsCaptchaView(webDriver))
                        {
                            await SolveCapthcha(wait, webDriver);
                            webDriver.Navigate().GoToUrl(url);
                        }

                        try
                        {
                            // Ждем пока элемент с определенным селектором появится и станет кликабельным
                            element = webDriver.FindElement(By.XPath("//a[.//span[contains(text(),'Получить бонус')]]"));
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
                    var smsModelDto = await _task.SmsService.GetNewPhoneNumber();

                    //Send phone number
                    element = webDriver.FindElement(By.XPath("//input"));
                    //element.Click();
                    element.SendKeys(smsModelDto.Phone.ToString());

                    //Click next button
                    element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("phone-auth-section__submit-button")));
                    element.Click();

                    try
                    {
                        //Иногда кнопка "Далее" бывает, иногда нет
                        //Click next button
                        element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//button[.//span[contains(text(), 'Далее')]]")));
                        element.Click();
                    }
                    catch (Exception ex)
                    { 
                    }

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
                    await _task.SmsService.SetSmsOk(smsModelDto.Id);

                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("passp-field-firstname"))).SendKeys(_task.UserNameForRegistration);
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("passp-field-lastname"))).SendKeys(_task.SecondNameForRegistration);

                    element = webDriver.FindElement(By.CssSelector("button[data-t='button:action']"));
                    element.Click();

                    element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("PermissionCheckbox")));
                    element.Click();

                    element = webDriver.FindElement(By.CssSelector("button[data-t='button:action']"));
                    element.Click();

                    element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("RegSurveyPage-button")));
                    element.Click();

                    element = webDriver.FindElement(By.XPath("//button[text()='Подтвердить участие']"));
                    element.Click();

                    element = webDriver.FindElement(By.XPath("//a[text()='На главную']"));
                    element.Click();

                    _task.RegisteredUser = new User()
                    {
                        FirstName = _task.UserNameForRegistration,
                        LastName = _task.SecondNameForRegistration,
                        Phone = smsModelDto?.Phone?.ToString(),
                        RegistrationDate = DateTime.Now
                    };
                }

                webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());
                foreach (var request in _task.Queries)
                {
                    IWebElement element;
                    try
                    {
                        element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("search3__input-inner-container")));
                        element = element.FindElement(By.TagName("input"));
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("HeaderForm-InputContainer")));
                            element = element.FindElement(By.TagName("input"));
                        }
                        catch (Exception exc)
                        {
                            element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//div[.input]")));
                        }
                    }
                    element.Clear();
                    element.SendKeys(request.Value);
                    element.SendKeys(Keys.Enter);
                }

                _task.Status = YandexTaskStatus.Ok;
            }
            catch (Exception ex)
            {
                _task.Status = YandexTaskStatus.Error;
                _task.ErrorMessage = ex.Message;
            }
            finally
            {
                if (webDriver != null)
                {
                    webDriver.Close();
                    webDriver.Dispose();
                    GC.Collect();
                }
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
            if(tempDirectory.Exists)
                tempDirectory.Delete(true);

            _task.SmsService.Dispose();
        }
    }
}
