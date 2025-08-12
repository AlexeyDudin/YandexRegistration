using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using TwoCaptcha.Captcha;

namespace YandexRegistrationCommon.Infrastructure.APIHelper
{
    public class RuCaptchaHelper
    {
        private readonly string _apiKey;
        private TwoCaptcha.TwoCaptcha _solver;
        private Regex _coordsRegex = new Regex("x=[0-9]+,y=[0-9]+");
        private Regex _numberRegex = new Regex("[0-9]+");

        public RuCaptchaHelper()
        {
            _apiKey = SettingsHelper.RuCaptchaToken;
            _solver = new TwoCaptcha.TwoCaptcha(_apiKey);
        }

        public async Task SolveCapcha(WebDriverWait wait, IWebDriver driver)
        {
            if (!await SolveImageCoords(wait, driver))
                if (!await SolveText(wait, driver))
                    return;

        }

        private async Task<bool> SolveText(WebDriverWait wait, IWebDriver driver)
        {
            try
            {
                var element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.AdvancedCaptcha-View")));
                var urlMainImage = element.FindElement(By.TagName("img")).GetAttribute("src");
                var description = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("AdvancedCaptcha-FormLabel"))).Text;

                Normal text = new Normal(urlMainImage);
                text.SetLang("ru");
                text.SetHintText(description);
                await _solver.Solve(text);

                var result = await _solver.GetResult(text.Code);

                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("span.Textinput.Textinput_hasClear.Textinput_view_captcha.Textinput_size_m")));
                element.SendKeys(result);

                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("CaptchaButton-SubmitContent")));
                element.Click();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> SolveImageCoords(WebDriverWait wait, IWebDriver driver)
        {
            try
            {
                var element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.AdvancedCaptcha-ImageWrapper")));
                var urlMainImage = element.FindElement(By.TagName("img")).GetAttribute("src");
                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.AdvancedCaptcha-SilhouetteTask")));
                var urlAdditionalImage = element.FindElement(By.TagName("img")).GetAttribute("src");
                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("AdvancedCaptcha-DescriptionText")));
                var description = element.Text;

                var file = new FileInfo(Path.GetTempFileName());
                using var fs = file.OpenWrite();
                var bytes = await DownloadFromUrl(urlMainImage);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();

                decimal xDelimiter = 1;
                decimal yDelimiter = 1;

                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.AdvancedCaptcha-ImageWrapper"))).FindElement(By.TagName("img"));

                using (var img = Image.FromFile(file.FullName))
                {
                    xDelimiter = (decimal)(element.Size.Width) / (decimal)img.Width;
                    yDelimiter = (decimal)(element.Size.Height) / (decimal)img.Height;
                }

                var coords = new Coordinates();
                coords.SetFile(file.FullName);
                coords.SetHintImg(await DownloadImageAsBase64(urlAdditionalImage));
                coords.SetHintText(description);

                await _solver.Solve(coords);
                var coordMathes = _coordsRegex.Matches(coords.Code);

                var coordinates = new List<Coord>();
                foreach (Match match in coordMathes)
                {
                    MatchCollection coordMatch = _numberRegex.Matches(match.Value);
                    if (coordMatch.Count == 2)
                    {
                        var coord = new Coord();
                        coord.X = Int32.Parse(coordMatch[0].Value);
                        coord.Y = Int32.Parse(coordMatch[1].Value);
                        coordinates.Add(coord);
                    }
                }
                var action = new Actions(driver);
                
                try
                {
                    driver.SwitchTo().Frame(driver.FindElement(By.CssSelector("iframe-selector")));
                }
                catch (Exception ex)
                {
                }

                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.AdvancedCaptcha-View")));

                using (var img = Image.FromFile(file.FullName))
                {
                    xDelimiter = (decimal)(element.Size.Width) / (decimal)img.Width;
                    yDelimiter = (decimal)(element.Size.Height) / (decimal)img.Height;
                }

                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.AdvancedCaptcha_silhouette")));
                int steps = 15;

                var point = element.Location;
                var body = driver.FindElement(By.TagName("body"));

                foreach (var coord in coordinates)
                {
                    try
                    {
                        var js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript(@"
  const el = document.querySelector('.AdvancedCaptcha-ImageWrapper');
  console.log(el);
  const rect = el.getBoundingClientRect();
  console.log(rect);
  const x = rect.left + " + @$"{coord.X},
  const y = rect.top + " + $"{coord.Y}"+ @"

  ['mouseover', 'mousedown', 'mouseup', 'click'].forEach(type => {
        const ev = new MouseEvent(type, {
            bubbles: true,
            cancelable: true,
            view: window,
            clientX: x,
            clientY: y
        });
        el.dispatchEvent(ev);
    });

  console.log(x);
  console.log(y);
  el.dispatchEvent(ev);
");
                    
                    }
                    catch (Exception ex)
                    {

                    }
                    await Task.Delay(1000);
                }

                element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.ClassName("CaptchaButton-SubmitContent")));
                element.Click();
                driver.SwitchTo().DefaultContent();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        static async Task<string> DownloadImageAsBase64(string url)
        {
            byte[] imageBytes = await DownloadFromUrl(url);
            return Convert.ToBase64String(imageBytes);
        }

        static async Task<byte[]> DownloadFromUrl(string url)
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetByteArrayAsync(url);
        }

        public async Task SolvePhoneCapcha(WebDriverWait wait, IWebDriver webDriver)
        {
            var element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.captcha__container")));
            var urlMainImage = element.FindElement(By.TagName("img")).GetAttribute("src");

            Normal text = new Normal(urlMainImage);
            text.SetLang("ru");
            await _solver.Solve(text);

            var result = await _solver.GetResult(text.Code);

            var inputElement = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//span.TextInput[.//input]")));
            inputElement.SendKeys(result);

            var nextButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//button[.//span[text()='Продолжить']]")));
            nextButton.Click();
        }
    }

    internal class RequestBody
    {
        public string type { get; set; } = "CoordinatesTask";
        public string body { get; set; }
        public string comment { get; set; } = "Click on correct subsequence";
        public string imgInstructions { get; set; }

    }

    internal class RequestCreateTaskDto
    {
        public string clientKey { get; set; }
        public RequestBody task { get; set; }
    }

    internal class Coord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    internal class ResponseCreateTaskDTO
    {
        public int errorId { get; set; } = 0;
        public int taskId { get; set; } = 0;
    }

    internal class RequestStatusTaskDto
    {
        public string clientKey { get; set; }
        public int taskId { get; set; }
    }

    internal class ResponseStatusTaskDto
    {
    }
}
