using Denomica.Sitemap.Configuration;
using Denomica.Sitemap.Model;
using System;
using System.Collections.Generic;
using System.Net;
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
        private const int MaxRedirectCount = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="SitemapCrawler"/> class with the specified robots.txt parser
        /// and HTTP client factory.
        /// </summary>
        /// <param name="parser">The <see cref="RobotsTxtParser"/> used to parse robots.txt files. Cannot be <see langword="null"/>.</param>
        /// <param name="factory">The <see cref="IHttpClientFactory"/> used to create HTTP clients. Cannot be <see langword="null"/>.</param>
        /// <param name="requestFactory">The request factory used to create configured <see cref="HttpRequestMessage"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required dependency is <see langword="null"/>.</exception>
        public SitemapCrawler(
            RobotsTxtParser parser,
            IHttpClientFactory factory,
            IHttpRequestFactory requestFactory)
        {
            this.RobotsParser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.Client = factory?.CreateClient(Constants.HttpClientName) ?? throw new ArgumentNullException(nameof(factory));
            this.RequestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SitemapCrawler"/> class.
        /// </summary>
        /// <param name="parser">The <see cref="RobotsTxtParser"/> used to parse robots.txt files. Cannot be <see langword="null"/>.</param>
        /// <param name="factory">The <see cref="IHttpClientFactory"/> used to create HTTP clients. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
        public SitemapCrawler(RobotsTxtParser parser, IHttpClientFactory factory)
            : this(parser, factory, new HttpRequestFactory())
        {
        }

        private readonly RobotsTxtParser RobotsParser;
        private readonly HttpClient Client;
        private readonly IHttpRequestFactory RequestFactory;

        /// <summary>
        /// Asynchronously determines whether the specified URL can be crawled for sitemap data.
        /// </summary>
        /// <remarks>This method uses the same sitemap discovery pipeline as <see cref="CrawlAsync(Uri)"/>. It returns
        /// <see langword="true"/> when the supplied URL resolves to at least one valid sitemap document, either
        /// directly, through a sitemap location declared in robots.txt, or through a default sitemap location. Valid
        /// sitemap documents may be empty. If the supplied URL resolves to XML that is not a sitemap document, that
        /// result is treated as a miss and discovery continues through the remaining mechanisms. A return value of
        /// <see langword="false"/> indicates that the shared sitemap discovery used by <see cref="CrawlAsync(Uri)"/>
        /// did not resolve a valid sitemap document for the same input, and <see cref="CrawlAsync(Uri)"/> may still
        /// throw before or during enumeration.</remarks>
        /// <param name="url">The URL to evaluate. This can be either a HTTP(S) URI or a URI pointing to a local file
        /// containing sitemap XML.</param>
        /// <returns><see langword="true"/> if the shared sitemap discovery used by <see cref="CrawlAsync(Uri)"/>
        /// resolves at least one valid sitemap document for the supplied URL; otherwise, <see
        /// langword="false"/>.</returns>
        public async Task<bool> CanCrawlAsync(Uri url)
        {
            try
            {
                await foreach (var _ in this.DiscoverSitemapDocumentsAsync(url))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Asynchronously determines whether the specified URL can be crawled for sitemap data.
        /// </summary>
        /// <remarks>This overload parses the supplied URL string into a <see cref="Uri"/> and delegates to <see
        /// cref="CanCrawlAsync(Uri)"/>.</remarks>
        /// <param name="url">The absolute URL to evaluate.</param>
        /// <returns><see langword="true"/> if the supplied URL can be used with <see cref="CrawlAsync(string)"/> to
        /// resolve a valid sitemap document; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> CanCrawlAsync(string url)
        {
            var uri = new Uri(url);
            return await this.CanCrawlAsync(uri);
        }

        /// <summary>
        /// Asynchronously crawls the specified URL to discover and yield URLs specified in a sitemap.
        /// </summary>
        /// <remarks>This method uses the same sitemap discovery pipeline as <see cref="CanCrawlAsync(Uri)"/>. It first
        /// probes the supplied URL directly, then checks sitemap locations declared in robots.txt, and finally checks
        /// standard default sitemap locations when the earlier discovery steps do not resolve to a valid sitemap.
        /// If the supplied URL resolves to XML that is not a sitemap document, that result is treated as a miss and
        /// the method continues to the remaining discovery mechanisms for that input.</remarks>
        /// <param name="url">
        /// The starting <see cref="Uri"/> to begin crawling from. This URL can either point directly to a sitemap XML. If
        /// the URL does not point to a valid XML document, the method will attempt to find sitemaps from the site.
        /// </param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the URLs found in the sitemaps.</returns>
        public async IAsyncEnumerable<UrlsetUrl> CrawlAsync(Uri url)
        {
            await foreach (var (_, xmlDoc) in this.DiscoverSitemapDocumentsAsync(url))
            {
                await foreach (var pageUrl in this.EnumerateSitemapUrlsAsync(xmlDoc))
                {
                    yield return pageUrl;
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
        /// <param name="url">The URL from which to download the XML document. Must be a valid URI. This can be either a HTTP(s) URI or a URI pointing to a local file. Local files must also be valid Sitemap XML files.</param>
        /// <returns>An <see cref="XmlDocument"/> containing the downloaded XML data if the request is successful; otherwise,
        /// <see langword="null"/>.</returns>
        private async Task<XmlDocument?> DownloadXmlDocumentAsync(Uri url)
        {
            var (_, document) = await this.DownloadXmlDocumentWithResolvedUriAsync(url);
            return document;
        }

        private async Task<(Uri ResolvedUri, XmlDocument? Document)> DownloadXmlDocumentWithResolvedUriAsync(Uri url)
        {
            string? xml = null;
            var resolvedUri = url;
            if(url.Scheme == "file")
            {
                xml = await System.IO.File.ReadAllTextAsync(url.LocalPath);
            }
            else
            {
                using var response = await this.SendRequestAsync(url, HttpMethod.Get);
                resolvedUri = response.RequestMessage?.RequestUri ?? url;
                if (response.IsSuccessStatusCode)
                {
                    xml = await response.Content.ReadAsStringAsync();
                }
            }

            if (xml?.Length > 0)
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(xml);
                    return (resolvedUri, doc);
                }
                catch { }
            }

            return (resolvedUri, null);
        }

        private async IAsyncEnumerable<(Uri SitemapUri, XmlDocument Document)> DiscoverSitemapDocumentsAsync(Uri url)
        {
            var visitedInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var discoveredSitemaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var sitemap in this.DiscoverSitemapDocumentsAsync(url, visitedInputs, discoveredSitemaps))
            {
                yield return sitemap;
            }
        }

        private async IAsyncEnumerable<(Uri SitemapUri, XmlDocument Document)> DiscoverSitemapDocumentsAsync(
            Uri url,
            HashSet<string> visitedInputs,
            HashSet<string> discoveredSitemaps)
        {
            if (!visitedInputs.Add(GetUriKey(url)))
            {
                yield break;
            }

            var (resolvedUri, xmlDocument) = await this.DownloadXmlDocumentWithResolvedUriAsync(url);
            if (null != xmlDocument)
            {
                if (IsSitemapDocument(xmlDocument))
                {
                    if (discoveredSitemaps.Add(GetUriKey(resolvedUri)))
                    {
                        yield return (resolvedUri, xmlDocument);
                    }

                    yield break;
                }
            }

            bool foundRobotsSitemap = false;
            await foreach (var sitemap in this.RobotsParser.GetSitemapsAsync(url))
            {
                foreach (var discovered in await this.DiscoverSitemapDocumentsSafeAsync(sitemap, visitedInputs, discoveredSitemaps))
                {
                    foundRobotsSitemap = true;
                    yield return discovered;
                }
            }

            if (foundRobotsSitemap)
            {
                yield break;
            }

            foreach (var sitemap in this.EnumerateDefaultSitemaps(url))
            {
                foreach (var discovered in await this.DiscoverSitemapDocumentsSafeAsync(sitemap, visitedInputs, discoveredSitemaps))
                {
                    yield return discovered;
                }
            }
        }

        private async Task<List<(Uri SitemapUri, XmlDocument Document)>> DiscoverSitemapDocumentsSafeAsync(
            Uri url,
            HashSet<string> visitedInputs,
            HashSet<string> discoveredSitemaps)
        {
            var results = new List<(Uri SitemapUri, XmlDocument Document)>();

            try
            {
                await foreach (var discovered in this.DiscoverSitemapDocumentsAsync(url, visitedInputs, discoveredSitemaps))
                {
                    results.Add(discovered);
                }
            }
            catch
            {
            }

            return results;
        }

        private static bool IsSitemapDocument(XmlDocument document)
        {
            var root = document.DocumentElement;
            if (null == root)
            {
                return false;
            }

            if (root.LocalName != "urlset" && root.LocalName != "sitemapindex")
            {
                return false;
            }

            return root.NamespaceURI == Constants.SchemaNamespace1
                || root.NamespaceURI == Constants.SchemaNamespace2;
        }

        private static string GetUriKey(Uri url)
        {
            return url.AbsoluteUri;
        }

        /// <summary>
        /// Enumerates the default sitemap URLs for a given base URL.
        /// </summary>
        /// <remarks>This method returns common sitemap locations (e.g., "/sitemap.xml" and
        /// "/sitemap_index.xml") so they can be probed with the same GET-based sitemap discovery logic used by the
        /// crawler.</remarks>
        /// <param name="url">The base URL from which to derive the default sitemap URLs.</param>
        /// <returns>A sequence of <see cref="Uri"/> objects representing the default sitemap URLs to probe.</returns>
        private IEnumerable<Uri> EnumerateDefaultSitemaps(Uri url)
        {
            return new List<Uri>
            {
                new Uri(url, "/sitemap.xml"),
                new Uri(url, "/sitemap_index.xml")
            };
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
            await foreach(var url in this.EnumerateSitemapUrlsAsync(doc, Constants.SchemaNamespace1))
            {
                yield return url;
            }

            await foreach (var url in this.EnumerateSitemapUrlsAsync(doc, Constants.SchemaNamespace2))
            {
                yield return url;
            }
        }

        /// <summary>
        /// Asynchronously enumerates all URLs found in a sitemap XML document using the specified XML namespace.
        /// </summary>
        /// <param name="doc">
        /// The XML document containing the sitemap data. This document should conform to the sitemap protocol
        /// </param>
        /// <param name="xmlNamespace">
        /// The XML namespace to use for selecting nodes in the document. This is typically "http://www.sitemaps.org/schemas/sitemap/0.9"
        /// </param>
        private async IAsyncEnumerable<UrlsetUrl> EnumerateSitemapUrlsAsync(XmlDocument doc, string xmlNamespace)
        {
            var nsMan = new XmlNamespaceManager(doc.NameTable);
            nsMan.AddNamespace("sitemap", xmlNamespace);
            nsMan.AddNamespace("image", Constants.ImageSchemaNamespace);

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
                    if (DateTimeOffset.TryParse(modNode?.InnerText, out DateTimeOffset dto))
                    {
                        dt = dto;
                    }
                    Image? img = null;
                    var imageNode = node.SelectSingleNode("image:image", nsMan);
                    if(null != imageNode)
                    {
                        img = new Image();
                        var imageLocNode = imageNode.SelectSingleNode("image:loc", nsMan);
                        if(Uri.TryCreate(imageLocNode.InnerText, UriKind.Absolute, out var imageLoc))
                        {
                            img.Location = imageLoc;
                        }

                        img.Title = imageNode?.SelectSingleNode("image:title", nsMan)?.InnerText;
                        img.Caption = imageNode?.SelectSingleNode("image:caption", nsMan)?.InnerText;
                    }

                    yield return new UrlsetUrl { Location = pageUrl, LastModified = dt, Image = img };
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
            return await this.SendRequestAsync(url, method, 0);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(Uri url, HttpMethod method, int redirectCount)
        {
            if (redirectCount >= MaxRedirectCount)
            {
                throw new HttpRequestException($"Too many redirects while requesting '{url}'.");
            }

            using var request = this.RequestFactory.Create(method, url);
            var response = await this.Client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.MovedPermanently
                && response.StatusCode != HttpStatusCode.Found)
            {
                return response;
            }

            var redirectLocation = response.Headers.Location;
            if (null == redirectLocation)
            {
                return response;
            }

            var requestUri = response.RequestMessage?.RequestUri ?? url;
            var redirectUri = redirectLocation.IsAbsoluteUri
                ? redirectLocation
                : new Uri(requestUri, redirectLocation);

            response.Dispose();
            return await this.SendRequestAsync(redirectUri, method, redirectCount + 1);
        }

    }
}
