using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SarfReport
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = InitConfig();
            var report = GetSurfReport();
            SendSms(config, report);
            
            Console.ReadKey();
        }
        static IConfigurationRoot InitConfig()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        static string GetSurfReport() {
            StringBuilder sb = new StringBuilder();
            DateTime today = DateTime.Now.Date;

            IWebDriver driver = new ChromeDriver(Environment.CurrentDirectory);
            driver.Navigate().GoToUrl("https://www.surfline.com/surf-forecasts/virginia/58581a836630e24c44878fdc");
            driver.Manage().Window.Maximize();

            //start with line break
            sb.Append("\n");

            //use wait to avoid race conditions
            IList<IWebElement> forecasts = new WebDriverWait(driver, TimeSpan.FromSeconds(2)).Until(d => d.FindElements(By.ClassName("quiver-condition-day-summary__condition")));
            for (var i = 0; i < forecasts.Count; i++)
            {
                //group forecast by two, for am and pm
                if (i % 2 == 0)
                {
                    sb.Append($"{today.Date: MM/dd/yyyy} - ");
                }

                var condition = forecasts[i].GetAttribute("class");
                condition = condition.Substring((condition.IndexOf("--") + 2), condition.Length - condition.IndexOf("--") - 2);
                sb.Append($"{condition}");
                if (i % 2 == 0)
                {
                    sb.Append(" (am)   ");
                }
                else
                {
                    sb.Append(" (pm) \n\n");
                    today = today.AddDays(1);
                }
            }
            
            //Close the browser
            driver.Close();

            return sb.ToString();

        }        

        static void SendSms(IConfigurationRoot config, string message) {
            string accountSid = config.GetSection("Twilio").GetValue<string>("TWILIO_ACCOUNT_SID");
            string authToken = config.GetSection("Twilio").GetValue<string>("TWILIO_AUTH_TOKEN");

            TwilioClient.Init(accountSid, authToken);

            var sms = MessageResource.Create(
                   body: message,
                   from: new Twilio.Types.PhoneNumber("+18489995754"),
                   to: new Twilio.Types.PhoneNumber("+17572010051")
               );

            Console.WriteLine(sms.Sid);
        }
    }
}
