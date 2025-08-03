using Denomica.Sitemap;
using Denomica.Sitemap.Configuration;
using Denomica.Sitemap.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using System;
using System.Runtime.CompilerServices;

var url = args[0];
var cookieConsentSelector = args[1];
var scrollToSelector = args[2];
var contentSelector = args[3];

var provider = new ServiceCollection()
    .AddDenomicaSitemap()
    .Services
    .BuildServiceProvider();

var pw = await Playwright.CreateAsync();
var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    SlowMo = 1000 //Add some delay in order not to overload the server and potentially get blocked. Can vary greately from server to server.
});
var page = await browser.NewPageAsync(new BrowserNewPageOptions { });
if (cookieConsentSelector?.Length > 0)
{
    await page.GotoAsync(url);
    await page.ClickAsync(cookieConsentSelector);
}

var crawler = provider.GetRequiredService<SitemapCrawler>();
await foreach (var set in crawler.CrawlAsync(url))
{
    var result = await page.GotoAsync(set.Location.ToString(), new PageGotoOptions { Referer = url });
    if (result?.Status == 200)
    {
        WriteConsole(set.Location, ConsoleColor.Green);
        if (scrollToSelector?.Length > 0)
        {
            try
            {
                var scrollToElement = await page.QuerySelectorAsync(scrollToSelector);
                if (null != scrollToElement)
                {
                    await scrollToElement.ScrollIntoViewIfNeededAsync();
                }
            }
            catch { }
        }

        if (contentSelector?.Length > 0)
        {
            var contentElement = await page.QuerySelectorAsync(contentSelector);
            var plainText = await GetTextContentAsync(contentElement);
            WriteConsole(plainText, ConsoleColor.Gray);
        }
    }
    else
    {
        WriteConsole(set.Location, ConsoleColor.Red);
    }
}


void WriteConsole(object? output, ConsoleColor color = ConsoleColor.White)
{
    if (null != output)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(output);
        Console.ForegroundColor = oldColor;
    }
}

async Task<string?> GetTextContentAsync(IElementHandle? element)
{
    if(null != element)
    {
        var removables = new List<string>
        {
            "\u00AD",
            "\t\t",
            "\n\n",
            "\n\t",
            "\t\n"
        };

        var text = await element.TextContentAsync();
        if (null != text)
        {
            while(removables.Any(r => text.Contains(r)))
            {
                foreach (var removable in removables)
                {
                    text = text.Replace(removable, string.Empty);
                }
            }

            return text;
        }
    }

    return null;
}