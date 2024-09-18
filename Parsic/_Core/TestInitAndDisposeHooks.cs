using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BoDi;

namespace AutoTestic._Core
{
    [Binding]
    public class TestInitAndDisposeHooks
    {
        [BeforeScenario]
        public void Setup(ScenarioContext container)
        {
            if (!container.ScenarioContainer.IsRegistered<WebPage>())
            {
                var page = new WebPage();
                container.ScenarioContainer.RegisterInstanceAs(page);

                // For working TLS/SSL correctly
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
        }

        [AfterScenario]
        public void TearDown(ScenarioContext container)
        {
            var page = container.ScenarioContainer.Resolve<WebPage>();
            page.Dispose();
        }
    }
}
