
using Denomica.Sitemap.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

var provider = new ServiceCollection()
    .AddDenomicaSitemap()
    .Services
    .BuildServiceProvider();

var url = args[0];
var crawler = provider.GetRequiredService<SitemapCrawler>();
int counter = 0;
await foreach(var u in crawler.CrawlAsync(new Uri(url)))
{
    counter++;
    Console.Write($"{counter:D4}");
    Console.Write(": ");
    Console.WriteLine(u.Location);
}