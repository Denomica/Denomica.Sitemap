# Denomica.Sitemap

A library that facilitates discovering and enumerating XML sitemaps on a site.

## Main Features

The main features that this library provides are described in the following sections.

### Discover URLs in Sitemaps

The SitemapCrawler services attempts to discover all sitemaps defined on a site, and then enumerate all URLs defined in those sitemaps. It supports both standard sitemap files and sitemap index files.

If the URL provided is a sitemap XML file, only that sitemap file is processed. If that sitemap XML file contains references to other sitemap files, those are also processed recursively.

## Getting Started

In addition to the `Denomica.Sitemap` package, you will need to install the [`Microsoft.Extensions.DependencyInjection`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) Nuget package.

The following code sample demonstrates how to use the `SitemapCrawler` service to crawl a sitemap and retrieve all URLs defined in it.

``` C#
using Denomica.Sitemap.Services;
using Microsoft.Extensions.DependencyInjection;

var provider = new ServiceCollection()
	.AddDenomicaSitemap()
	.Services
	.BuildServiceProvider();

var crawler = provider.GetRequiredService<SitemapCrawler>();
await foreach(var pageUrl in crawler.CrawlAsync(new Uri("https://yoursite.com")))
{
	Console.WriteLine(pageUrl.Location);
}
```

## Version Hightlights

### v1.0.0-beta.2

- Added support for crawling sitemaps that use an XML namespace other than the default `http://www.sitemaps.org/schemas/sitemap/0.9`. Many sites seem to incorrectly use a namespace with `https` prefix.

### v1.0.0-beta.1

- Added overloaded `CrawlAsync` method that accepts a `string` parameter to specify the base URL of the site to crawl.

### v1.0.0-alpha.2

- Modified the return type of the `CrawlAsync` method to return an async enumerable of `UrlsetUrl` objects.

### v1.0.0-alpha.1

- Initial release of the Denomica.Sitemap package.
- Provides a `SitemapCrawler` service that crawls [sitemap XML files](https://en.wikipedia.org/wiki/Sitemaps) and returns all URLs defined in them.
