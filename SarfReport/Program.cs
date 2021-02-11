using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;
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

            //Console.WriteLine(report);

            //Console.ReadKey();
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

        static string GetSurfReport()
        {
            StringBuilder sb = new StringBuilder();
            DateTime today = DateTime.Now.Date;

            IWebDriver driver = new ChromeDriver(Environment.CurrentDirectory);
            driver.Navigate().GoToUrl("https://www.surfline.com/surf-forecasts/virginia/58581a836630e24c44878fdc");
            driver.Manage().Window.Maximize();

            sb.Append($"\n\n");

            IList<IWebElement> dayForecasts = new WebDriverWait(driver, TimeSpan.FromSeconds(2)).Until(e => e.FindElements(By.ClassName("quiver-forecast-graphs__day-summary")));
            foreach (IWebElement forecast in dayForecasts)
            {
                var conditions = forecast.FindElements(By.ClassName("quiver-condition-day-summary__condition__rating"));
                var surfHeight = forecast.FindElements(By.ClassName("quiver-surf-height"));

                for (int i = 0; i < conditions.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        sb.Append($"{today.Date: MM/dd/yyyy}\n");
                    }
                    else
                    {
                        today = today.AddDays(1);
                    }


                    string rating = $"{GetRating(conditions[i].Text) * 10}%";
                    string height = surfHeight[i].Text;

                    sb.Append($"{rating} : {height} \n");

                    if (i % 2 != 0) {
                        sb.Append("\n");
                    }

                }
            }

            return sb.ToString();

        }

        static void SendSms(IConfigurationRoot config, string message)
        {
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

        static int GetRating(string condition) {
            int result = 0;
            switch (condition.ToLower())
            {
                case "flat":
                    result = 1;
                    break;
                case "very poor":
                    result = 2;
                    break;
                case "poor":
                    result = 3;
                    break;
                case "poor to fair":
                    result = 4;
                    break;
                case "fair":
                    result = 5;
                    break;
                case "fair to good":
                    result = 6;
                    break;
                case "good":
                    result = 7;
                    break;
                case "very good":
                    result = 8;
                    break;
                case "good to epic":
                    result = 9;
                    break;
                case "epic":
                    result = 10;
                    break;

                default:
                    return 0;
            }
            return result;
        }
    }
}
