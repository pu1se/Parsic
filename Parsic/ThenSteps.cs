using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoTestic
{
    [Binding]
    public class ThenSteps : BaseSteps
    {
        public ThenSteps(WebPage page) : base(page)
        {
        }
        
        [Then(@"check belavia")]
        public async Task ThenTestExample()
        {
            try
            {
                await Page.OpenUrl("https://belavia.by/novosti/", false);
                var headers = await Page.SelectAll(".news-list li");
                foreach (var header in headers.Take(2))
                {
                    var dateElement = header.QuerySelectorAsync(".dt").Result?.InnerTextAsync().Result;
                    var dayAsString = dateElement?.Split(' ')[0];
                    int.TryParse(dayAsString, out var dayAsInt);
                    var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, dayAsInt);

                    if (DateTime.UtcNow.AddDays(-3) > date || DateTime.UtcNow < date)
                    {
                        continue;
                    }

                    var text = header.QuerySelectorAsync("a").Result?.InnerTextAsync().Result;
                    text = text!.ToLower().Trim();
                    if (text.Contains("дари") 
                        || 
                        text.Contains("скидк") 
                        || 
                        text.Contains("акци") 
                        || 
                        text.Contains("промо") 
                        || 
                        text.Contains("распрод"))
                    {
                        text += Environment.NewLine + " Ссылка: https://belavia.by/novosti/";
                        await SendEmail.ToSubscribedPeople("Notify about air plain discount", text);
                    }
                }

                await SendEmail.ToMyself("GitHub works", "hello");
            }
            catch (Exception exception)
            {
                await SendEmail.ToMyself("Error in Parser", exception.ToFormattedExceptionDescription());
            }
        }
    }
}
