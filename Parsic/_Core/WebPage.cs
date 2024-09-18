using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Playwright;

namespace AutoTestic._Core
{
    public class WebPage : IDisposable
    {
        private readonly IPlaywright _tool;
        public readonly IBrowser _browser;
        public Settings Settings { get; }
        public Dictionary<string, string> Remember = new Dictionary<string, string>();
        public IPage _page;
        private const int _timeoutForWaitElementIsVisibleIs120Seconds = 120;
        private const int _timeoutForWaitElementIsHiddenIs120Seconds = 120;
        private const int _timeoutForOpenUrlIs120Seconds = 120;
        public string BaseUrl { get; set; }

        public WebPage()
        {
            Settings = new Settings();

            _tool = Playwright.CreateAsync().Result;
            var _browser = _tool.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
            }).Result;
            _page = _browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true,
                ViewportSize = new ViewportSize
                {
                    Height = 950,
                    Width = 1600,
                }
            }).Result;
        }

        public async Task ReloadPage()
        {
            await _page.ReloadAsync(new PageReloadOptions
            {
                Timeout = _timeoutForOpenUrlIs120Seconds*1000
            });
        }

        public async Task AddHeader(string key, string value)
        {
            //var token = Auth0Api.GeAuthTokenFor(UserRole.SellerAdmin);
            await _page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                { key, value },
                //{"Authorization", token}
            });
        }

        public string Url
        {
            get
            {
                var url = _page.Url;
                return url;
            }
        }

        public async Task OpenUrl(string url, bool isRelativeUrl = true)
        {
            if (isRelativeUrl)
            {
                if (url.StartsWith('/') == false)
                {
                    url = "/" + url;
                }
                url = BaseUrl.TrimEnd('/') + url;
            }
            
            await _page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = _timeoutForOpenUrlIs120Seconds*1000
            });
        }

        public async Task<bool> WaitUntilElementIsVisible(string cssSelector, int? timeoutInSeconds = null)
        {
            try
            {
                if (timeoutInSeconds.HasValue)
                {
                    var element = SelectVisibleElement(cssSelector, timeoutInSeconds.Value);
                }
                else
                {
                    var element = SelectVisibleElement(cssSelector);
                }
                
                return true;
            }
            catch (Exception e)
            {
                var imagePath = await MakeScreenShot();
                Console.WriteLine($"WaitUntilElementIsVisible: Element with selector '{cssSelector}' was not visible within the specified timeout.{Environment.NewLine} " +
                                           $"Screenshot: {imagePath}{Environment.NewLine}" +
                                           $"Exception: {e.ToFormattedExceptionDescription()}");
                return false;
            }
        }

        public async Task WaitUntilElementIsNotVisible(string cssSelector)
        {
            try
            {
                var hiddenElement = await _page.WaitForSelectorAsync(cssSelector.NormalizeCssSelector(), new PageWaitForSelectorOptions
                    {
                        Timeout = _timeoutForWaitElementIsHiddenIs120Seconds*1000,
                        State = WaitForSelectorState.Hidden
                    });
                return;
            }
            catch
            {
                // ignore
            }

            var element = await _page.QuerySelectorAsync(cssSelector.NormalizeCssSelector());
            if (element == null)
            {
                return;
            }

            var positionOfElement = await element.BoundingBoxAsync();
            if (positionOfElement == null || _page.ViewportSize == null)
            {
                return;
            }

            // hack: for side bar on the right side, like shopping cart.
            if (positionOfElement.X < _page.ViewportSize.Width)
            {
                throw new Exception($"Element {cssSelector.NormalizeCssSelector()} is visible, but it must be hidden.");
            }
        }

        public async Task RunScript(string script)
        {
            await _page.EvaluateAsync(script);
        }

        public async Task<T> RunScriptAndGetValue<T>(string script)
        {
            return await _page.EvaluateAsync<T>(script);
        }

        public async Task<string> MakeScreenShot()
        {
            var fileName = $"autotestic-{DateTime.UtcNow.ToString("dd-MM-yyyy_HH-mm-ss")}__{Guid.NewGuid().ToString().Substring(0, 8)}.png";
            await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = fileName,
                FullPage = false,
                Timeout = 20000,
            });

            var imagePath = await SaveFileToBlobStorage(fileName);
            Console.WriteLine($"Screenshot: {imagePath}");
            return imagePath;
        }

        private async Task<string> SaveFileToBlobStorage(string fileName)
        {
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=navi2016;AccountKey=0pWpkSj+Q6U3o37qYeWqn12r6STYo839Fe5Pf1MYdbTZoYI7DccJ60ZAOa0pqGeiKxs1fFMclZfUObrKa88YcQ==;EndpointSuffix=core.windows.net";
            var containerName = "autotests";
            var fileUrlPrefix = $"https://navi2016.blob.core.windows.net/{containerName}/";


            if (!File.Exists(fileName))
            {
                Console.WriteLine("File with screen shot not exists");
                return string.Empty;
            }

            var container = new BlobContainerClient(connectionString, containerName);


            var blob = container.GetBlobClient(fileName);
            using var openFileStream = File.OpenRead(fileName);
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await blob.UploadAsync(openFileStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/png"
                }
            }, cancellationToken.Token);
            openFileStream.Close();

            return fileUrlPrefix + fileName;
        }

        public Task WaitFor(int seconds)
        {
            return Task.Delay(seconds * 1000);
        }

        public async Task WaitForGrid()
        {
            await WaitUntilElementIsNotVisible(".dx-loadindicator-icon");
        }

        public async Task WaitForOneTimeLoader()
        {
            await WaitUntilElementIsNotVisible("@dotLoadingAnimation-block");
        }

        public async Task WaitForLoader()
        {
            await WaitUntilElementIsNotVisible("app-ring-loader-only");
        }

        public async Task WaitForUrlContains(string subUrl)
        {
            bool isUrlContainsSubUrl;
            var index = 0;
            do
            {
                await WaitFor(1);
                var currentUrl = this.Url.ToLower();
                isUrlContainsSubUrl = currentUrl.Contains(subUrl.ToLower());

                if (index++ > _timeoutForOpenUrlIs120Seconds)
                {
                    throw new TimeoutException($"Timeout for {nameof(WaitForUrlContains)} with sub url '{subUrl}' was reached.");
                }
            } 
            while (!isUrlContainsSubUrl);
        }

        public async Task WaitForUrlNotContains(string subUrl)
        {
            bool isUrlContainsSubUrl;
            var index = 0;
            do
            {
                await WaitFor(1);
                var currentUrl = this.Url.ToLower();
                isUrlContainsSubUrl = currentUrl.Contains(subUrl.ToLower());

                if (index++ > _timeoutForOpenUrlIs120Seconds)
                {
                    throw new TimeoutException($"Timeout for {nameof(WaitForUrlNotContains)} with sub url '{subUrl}' was reached.");
                }
            } 
            while (isUrlContainsSubUrl);
        }

        public WebElement SelectVisibleElement(string cssSelector, int timeoutInSeconds = _timeoutForWaitElementIsVisibleIs120Seconds)
        {
            try
            {
                cssSelector = cssSelector.NormalizeCssSelector();
                var options = new PageWaitForSelectorOptions();
                options.Timeout = timeoutInSeconds*1000;
                options.State = WaitForSelectorState.Visible;
                var element = _page.WaitForSelectorAsync(cssSelector, options).Result;
                if (element != null)
                {
                    return new WebElement(element, cssSelector, this);
                }

                throw new Exception($"Element {cssSelector} not found.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Element {cssSelector} not found.");
                Console.WriteLine(e.ToFormattedExceptionDescription());
                throw;
            }
        }

        public WebElement SelectVisibleAndHiddenElement(string cssSelector)
        {
            try
            {
                cssSelector = cssSelector.NormalizeCssSelector();
                var options = new PageWaitForSelectorOptions();
                options.Timeout = _timeoutForWaitElementIsVisibleIs120Seconds*1000;
                options.State = WaitForSelectorState.Attached;
                var element = _page.WaitForSelectorAsync(cssSelector, options).Result;
                if (element != null)
                {
                    return new WebElement(element, cssSelector, this);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Element {cssSelector} not found.");
                Console.WriteLine(e.ToFormattedExceptionDescription());
                throw;
            }

            throw new Exception($"Element {cssSelector} not found.");
        }

        public async Task<IReadOnlyList<IElementHandle>> SelectAll(string cssSelector)
        {
            cssSelector = cssSelector.NormalizeCssSelector();
            var firstElement = SelectVisibleElement(cssSelector);
            return await _page.QuerySelectorAllAsync(cssSelector);
        }

        public async Task<bool> IsSelectedElementExists(string cssSelector)
        {
            try
            {
                var element = SelectVisibleElement(cssSelector);
                return true;
            }
            catch
            {
                // ignore
                return false;
            }
        }


        #region Disposing
        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        ~WebPage()
        {
            ReleaseResources();
        }

        private bool _resourcesWasRelease = false;
        private void ReleaseResources()
        {
            if (_resourcesWasRelease)
                return;

            if (_browser != null)
            {
                _browser.CloseAsync().GetAwaiter().GetResult();
                _browser.DisposeAsync().GetAwaiter().GetResult();
            }

            if (_tool != null)
            {
                _tool.Dispose();
            }

            _resourcesWasRelease = true;
        }
        #endregion Disposing

        public async Task ClickAsync(string body)
        {
            await _page.ClickAsync(body);
        }
    }
}
