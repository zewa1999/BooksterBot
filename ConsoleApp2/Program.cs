using BooksterBot;
using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;

var count = 1;

try
{
    var configuration = GetConfiguration();

    if (!ValidateConfig(configuration))
    {
        throw new ArgumentException();
    }

    var chromeOptions = new ChromeOptions();
    chromeOptions.AddArguments("headless");
    chromeOptions.AddArguments("disable-gpu");
    chromeOptions.AddArguments("window-size=1920,1080");
    chromeOptions.AddArguments("start-maximized");
    using var driver = new ChromeDriver(chromeOptions);
    Console.Clear();

    Login(driver, configuration.BooksterEmail, configuration.BooksterPassword);
    Random rnd = new Random(65789);

    while (true)
    {
        if (GetBook(driver, configuration.BooksterBookLink) == true)
        {
            Console.WriteLine("The book was booked on: " + DateTime.UtcNow);
            driver.Quit();
            await SendBookedMail(configuration);
            break;
        }

        Console.WriteLine($"Book couldn't be borrowed. Retrying counter: {count}");
        count++;
        Thread.Sleep(5000 + rnd.Next() % 8000);
    }
}
catch (Exception ex)
{
    Console.WriteLine("Something went wrong:" + ex.Message);
}

static bool GetBook(IWebDriver driver, string link)
{
    Thread.Sleep(3000);
    var tabs = driver.WindowHandles;
    driver.SwitchTo().Window(tabs[0]);
    driver.Navigate().GoToUrl(link);
    Thread.Sleep(1000);

    string borrowButtonXPath = "/html/body/div[1]/div/div[1]/div/div[3]/div/div[2]/div[1]/button";
    string officeBorrowXPath = "/html/body/div[1]/div/div/div/div[2]/div/div[2]/div[2]/div/div/div/div[1]/div/button";
    string languageButtonXPath = "/html/body/div[1]/div/div/div/div[2]/div/div[2]/div[2]/div[3]/div/button";
    string borrowDescriptionDivXpath = "/html/body/div[1]/div/div/div/div[2]/div/div[2]/div[2]";

    var element = driver.FindElement(By.XPath(borrowButtonXPath));
    element.Click();
    Thread.Sleep(1000);

    element = driver.FindElement(By.XPath(officeBorrowXPath));
    element.Click();
    Thread.Sleep(1000);

    try
    {
        element = driver.FindElement(By.XPath(languageButtonXPath));
        element.Click();
        Thread.Sleep(1000);
    }
    catch { }

    element = driver.FindElement(By.XPath(borrowDescriptionDivXpath));

    if (element.Text.Contains("Felicitari pentru imprumut"))
    {
        return true;
    }

    return false;
}

static void Login(IWebDriver driver, string email, string password)
{
    driver.Navigate().GoToUrl("https://accounts.bookster.ro/login");

    string emailXPath = "/html/body/div/div[2]/div/div/div/div[1]/div/form/div[1]/div/input";
    string passwordXPath = "/html/body/div/div[2]/div/div/div/div[1]/div/form/div[2]/div/input";
    string loginButtonXPath = "/html/body/div/div[2]/div/div/div/div[1]/div/form/div[4]/div/input";

    var element = driver.FindElement(By.XPath(emailXPath));
    element.Click();
    element.SendKeys(email);

    element = driver.FindElement(By.XPath(passwordXPath));
    element.Click();
    element.SendKeys(password);

    element = driver.FindElement(By.XPath(loginButtonXPath));
    element.Click();
}

static bool ValidateConfig(BooksterBotSettings config)
{
    var context = new ValidationContext(config, serviceProvider: null, items: null);
    var results = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(config, context, results, true);

    if (!isValid)
    {
        foreach (var validationResult in results)
        {
            Console.WriteLine(validationResult.ErrorMessage);
        }
    }

    return isValid;
}

static async Task SendBookedMail(BooksterBotSettings config)
{
    SmtpClient smtp = new SmtpClient
    {
        Host = "smtp.gmail.com",
        Port = 587,
        EnableSsl = true,
        UseDefaultCredentials = false,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        Credentials = new NetworkCredential(config.GmailEmail, config.GmailPassword)
    };

    Email.DefaultSender = new SmtpSender(smtp);

    var email = await Email
    .From(config.GmailEmail)
    .To("costachestelianandrei@gmail.com", "Andrei Costache")
    .Subject("Bookster BOT: The book was booked today!")
    .Body("The book  was booked today. <br> Find it in your library: https://library.bookster.ro/profile/reading")
    .SendAsync();

    if (!email.Successful)
    {
        foreach (var item in email.ErrorMessages)
        {
            Console.WriteLine(item);
        }
    }
}

static BooksterBotSettings GetConfiguration()
{
    var booksterBotSettings = new BooksterBotSettings();
    var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false);

    IConfiguration config = builder.Build();
    var configManager = new ConfigurationManager();
    configManager.AddConfiguration(config);
    configManager.Bind(BooksterBotSettings.SectionName, booksterBotSettings);

    return booksterBotSettings;
}