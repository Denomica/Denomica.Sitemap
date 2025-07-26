
using Denomica.Sitemap.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

var provider = new ServiceCollection()
    .AddDenomicaSitemap()
    .Services
    .BuildServiceProvider();

//var url = "https://sameboat.fi";
//var url = "https://annonsbladet.fi/";
//var url = "https://www.kimitoon.fi";
//var url = "https://www.kemionsaari.fi/";
//var url = "https://mikaberglund.com";
var url = "https://www.kimitoon.fi/wp-sitemap-taxonomies-category-1.xml";
var rootUrl = new Uri(url);
var crawler = provider.GetRequiredService<SitemapCrawler>();
int counter = 0;
await foreach(var u in crawler.CrawlAsync(rootUrl))
{
    counter++;
    Console.Write($"{counter:D4}");
    Console.Write(": ");
    Console.WriteLine(u);
}