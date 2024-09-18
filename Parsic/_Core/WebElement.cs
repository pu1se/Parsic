using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTestic._Core
{
    public class WebElement
    {
        private readonly IElementHandle _element;
        private readonly WebPage _page;
        private readonly string _cssSelector;

        public WebElement(IElementHandle element, string cssSelector, WebPage page)
        {
            _element = element;
            _page = page;
            _cssSelector = cssSelector;
        }

        public async Task<string> Value()
        {
            if (_page.Url.ToLower().Contains("login"))
            {
                return string.Empty;
            }
            
            var text = await _page.RunScriptAndGetValue<string>($"$(\"{_cssSelector}\").val()");
            if (!text.IsNullOrEmpty())
            {
                return text.Trim();
            }

            text = await _element.GetAttributeAsync("value");
            if (!text.IsNullOrEmpty())
            {
                return text!.Trim();
            }

            text = await _element.InnerTextAsync();
            if (!text.IsNullOrEmpty())
            {
                return text.Trim();
            }

            text = await _element.GetAttributeAsync("ng-reflect-model");
            if (!text.IsNullOrEmpty())
            {
                return text!.Trim();
            }

            return string.Empty;
        }

        public async Task Value(string text)
        {
            var index = 0;
            string currentText;
            do
            {
                await SetValue(text);
                
                var typeAttribute = await _element.GetAttributeAsync("type");
                if (typeAttribute == "password")
                {
                    return;
                }

                await Task.Delay(400);
                currentText = await Value();

                if (index++ > 3 || _page.Url.ToLower().Contains("login"))
                {
                    return;
                }

                if (text.Equals(currentText) == false)
                {
                    Console.WriteLine($"Current text: {currentText}, but expected text is: {text}");
                    await _page.MakeScreenShot();
                }
                
            } while (text.Equals(currentText) == false);
        }

        private async Task SetValue(string text)
        {
            var currentText = await this.Value();
            if (text.Equals(currentText))
            {
                return;
            }

            if (currentText.IsNullOrEmpty())
            {
                await _element.FillAsync(text);
                return;
            }
            
            await _element.FillAsync("");
            await _element.FillAsync(text);
            // await _page.RunScript("$('body').click()");
            await _page.ClickAsync("body");
        }

        /*public async Task<string> Value(string text)
        {
            var currentText = await this.Value();
            if (!currentText.IsNullOrEmpty())
            {
                await _element.FillAsync(text);
                Thread.Sleep(300);
                await _page.ClickAsync("body");
                return text;
            }

            await _element.FillAsync(text);
            return text;
        }*/

        public Task PressKeyEnter()
        {
            return _element.PressAsync("Enter");
        }

        public async Task Click()
        {
            await _element.ClickAsync();
        }

        public async Task<string> Html()
        {
            var html = await _element.InnerHTMLAsync();
            return html;
        }
    }
}
