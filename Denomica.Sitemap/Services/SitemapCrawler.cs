using Denomica.Sitemap.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Denomica.Sitemap.Services
{
    /// <summary>
    /// Provides functionality to crawl and extract URLs from sitemaps and robots.txt files.
    /// </summary>
    /// <remarks>The <see cref="SitemapCrawler"/> class is designed to discover and enumerate URLs from a
    /// given website's sitemap. It attempts to download and parse XML sitemap files, and if unavailable, it will look
    /// for sitemaps specified in the robots.txt file or default sitemap locations. This class uses asynchronous
    /// operations to efficiently handle network requests and XML parsing.</remarks>
    public class SitemapCrawler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SitemapCrawler"/> class with the specified robots.txt parser
        /// and HTTP client factory.
        /// </summary>
        /// <param name="parser">The <see cref="RobotsTxtParser"/> used to parse robots.txt files. Cannot be <see langword="null"/>.</param>
        /// <param name="factory">The <see cref="IHttpClientFactory"/> used to create HTTP clients. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
        public SitemapCrawler(RobotsTxtParser parser, IHttpClientFactory factory)
        {
            this.RobotsParser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.Client = factory?.CreateClient(Constants.HttpClientName) ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly RobotsTxtParser RobotsParser;
        private readonly HttpClient Client;

        /// <summary>
        /// Asynchronously crawls the specified URL to discover and yield URLs specified in a sitemap.
        /// </summary>
        /// <remarks>The method first attempts to download and parse an XML document from the specified
        /// URL. If successful, it enumerates and yields URLs from the sitemap. If the URL does not correspond to an XML
        /// document, the method attempts to discover sitemaps by checking the site's robots.txt file and default
        /// sitemap locations.</remarks>
        /// <param name="url">
        /// The starting <see cref="Uri"/> to begin crawling from. This URL can either point directly to a sitemap XML. If
        /// the URL does not point to a valid XML document, the method will attempt to find sitemaps from the site.
        /// </param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the URLs found in the sitemaps.</returns>
        public async IAsyncEnumerable<UrlsetUrl> CrawlAsync(Uri url)
        {
            var xmlDoc = await this.DownloadXmlDocumentAsync(url);
            if (null != xmlDoc)
            {
                await foreach (var pageUrl in this.EnumerateSitemapUrlsAsync(xmlDoc))
                {
                    yield return pageUrl;
                }
            }
            else
            {
                // If the given url did not correspond to an XML document, then we need to
                // try and discover sitemaps from the site specified by url.
                // First we try to find any sitemaps specified in robots.txt.
                int sitemapCount = 0;
                var sitemaps = this.RobotsParser.GetSitemapsAsync(url);
                await foreach (var sitemap in sitemaps)
                {
                    sitemapCount++;
                    await foreach (var u in this.CrawlAsync(sitemap))
                    {
                        yield return u;
                    }
                }

                if (sitemapCount == 0)
                {
                    // If no sitemaps were found in robots.txt, we try to find default sitemap locations.
                    await foreach (var sitemap in this.EnumerateDefaultSitemapsAsync(url))
                    {
                        xmlDoc = await this.DownloadXmlDocumentAsync(sitemap);
                        if (null != xmlDoc)
                        {
                            await foreach(var pageUrl in  this.EnumerateSitemapUrlsAsync(xmlDoc))
                            {
                                yield return pageUrl;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously crawls the specified URL and retrieves a sequence of <see cref="UrlsetUrl"/> objects.
        /// </summary>
        /// <param name="url">The URL to crawl. Must be a valid, absolute URL.</param>
        /// <returns>An asynchronous stream of <see cref="UrlsetUrl"/> objects representing the crawled data.</returns>
        public async IAsyncEnumerable<UrlsetUrl> CrawlAsync(string url)
        {
            var uri = new Uri(url);
            await foreach(var set in  this.CrawlAsync(uri))
            {
                yield return set;
            }
        }



        /// <summary>
        /// Asynchronously downloads an XML document from the specified URL.
        /// </summary>
        /// <remarks>This method sends an HTTP GET request to the specified URL and attempts to parse the
        /// response content as an XML document. If the request fails or the response cannot be parsed as XML, the
        /// method returns <see langword="null"/>.</remarks>
        /// <param name="url">The URL from which to download the XML document. Must be a valid URI.</param>
        /// <returns>An <see cref="XmlDocument"/> containing the downloaded XML data if the request is successful; otherwise,
        /// <see langword="null"/>.</returns>
        private async Task<XmlDocument?> DownloadXmlDocumentAsync(Uri url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            try
            {
                var response = await this.Client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    return xmlDoc;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Asynchronously enumerates the default sitemap URLs for a given base URL.
        /// </summary>
        /// <remarks>This method checks common sitemap locations (e.g., "/sitemap.xml" and
        /// "/sitemap_index.xml") and yields only those that are accessible.</remarks>
        /// <param name="url">The base URL from which to derive the default sitemap URLs.</param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the default sitemap URLs that are
        /// accessible.</returns>
        private async IAsyncEnumerable<Uri> EnumerateDefaultSitemapsAsync(Uri url)
        {
            var urls = new List<Uri>
            {
                new Uri(url, "/sitemap.xml"),
                new Uri(url, "/sitemap_index.xml")
            };

            var actualUrls = new List<Uri>();
            foreach (var sitemapUrl in urls)
            {
                // Enumerate all default sitemap locations, and ensure that we do not return duplicates.
                var sitemapResponse = await this.SendRequestAsync(sitemapUrl, HttpMethod.Head);
                if(sitemapResponse.IsSuccessStatusCode && !actualUrls.Contains(sitemapResponse.RequestMessage.RequestUri))
                {
                    actualUrls.Add(sitemapResponse.RequestMessage.RequestUri);
                    yield return sitemapUrl;
                }
            }
        }

        /// <summary>
        /// Asynchronously enumerates all URLs found in a sitemap XML document.
        /// </summary>
        /// <remarks>This method processes both sitemap index files and regular sitemap files. It
        /// recursively follows sitemap index entries to enumerate all contained URLs. The method assumes the XML
        /// document adheres to the sitemap protocol as defined by sitemaps.org.</remarks>
        /// <param name="doc">The XML document containing the sitemap data. Must not be <see langword="null"/>.</param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the URLs found in the sitemap.</returns>
        private async IAsyncEnumerable<UrlsetUrl> EnumerateSitemapUrlsAsync(XmlDocument doc)
        {
            var nsMan = new XmlNamespaceManager(doc.NameTable);
            nsMan.AddNamespace("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (XmlNode node in doc.SelectNodes("sitemap:sitemapindex/sitemap:sitemap/sitemap:loc", nsMan))
            {
                if (Uri.TryCreate(node.InnerText, UriKind.Absolute, out var sitemapUrl))
                {
                    await foreach (var pageUrl in this.EnumerateSitemapUrlsAsync(sitemapUrl))
                    {
                        yield return pageUrl;
                    }
                }
            }
            
            foreach (XmlNode node in doc.SelectNodes("sitemap:urlset/sitemap:url", nsMan))
            {
                var locNode = node.SelectSingleNode("sitemap:loc", nsMan);
                var modNode = node.SelectSingleNode("sitemap:lastmod", nsMan);
                if (Uri.TryCreate(locNode.InnerText, UriKind.Absolute, out var pageUrl))
                {
                    DateTimeOffset? dt = null;
                    if(DateTimeOffset.TryParse(modNode?.InnerText, out DateTimeOffset dto))
                    {
                        dt = dto;
                    }
                    yield return new UrlsetUrl { Location = pageUrl, LastModified = dt };
                }
            }
        }

        /// <summary>
        /// Asynchronously enumerates all URLs found in a sitemap.
        /// </summary>
        /// <remarks>This method downloads the XML document from the specified sitemap URL and enumerates
        /// each URL contained within it.</remarks>
        /// <param name="url">The URI of the sitemap to be processed.</param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the URLs found in the sitemap.</returns>
        private async IAsyncEnumerable<UrlsetUrl> EnumerateSitemapUrlsAsync(Uri url)
        {
            var doc = await this.DownloadXmlDocumentAsync(url);
            if (null != doc)
            {
                await foreach (var pageUrl in this.EnumerateSitemapUrlsAsync(doc))
                {
                    yield return pageUrl;
                }
            }
        }

        /// <summary>
        /// Sends an HTTP request asynchronously to the specified URL using the given HTTP method.
        /// </summary>
        /// <param name="url">The URI to which the request is sent. Cannot be null.</param>
        /// <param name="method">The HTTP method to use for the request, such as GET or POST. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message
        /// received from the server.</returns>
        private async Task<HttpResponseMessage> SendRequestAsync(Uri url, HttpMethod method)
        {
            var request = new HttpRequestMessage(method, url);
            var response = await this.Client.SendAsync(request);
            return response;
        }

    }
}
