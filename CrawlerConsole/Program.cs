
using Denomica.Sitemap.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

var provider = new ServiceCollection()
    .AddDenomicaSitemap()
    .Services
    .BuildServiceProvider();

var crawler = provider.GetRequiredService<SitemapCrawler>();

var url = args[0];
var uri = new Uri(url);
int counter = 0;


var canCrawl = await crawler.CanCrawlAsync(uri);
if(canCrawl)
{
    await foreach (var u in crawler.CrawlAsync(uri))
    {
        counter++;
        Console.Write($"{counter:D4}");
        Console.Write(": ");
        Console.WriteLine(u.Location);
    }
}
