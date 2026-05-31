using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Sitemap.Configuration
{
    internal static class Constants
    {

        /// <summary>
        /// The name of the HttpClient registration used by sitemap services.
        /// </summary>
        public const string HttpClientName = "SitemapCrawlerHttpClient";

        /// <summary>
        /// The default browser-like User-Agent header used for sitemap requests.
        /// </summary>
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0";

        /// <summary>
        /// The standard HTTP sitemap XML namespace.
        /// </summary>
        public const string SchemaNamespace1 = "http://www.sitemaps.org/schemas/sitemap/0.9";

        /// <summary>
        /// The HTTPS sitemap XML namespace used by some sitemap documents.
        /// </summary>
        public const string SchemaNamespace2 = "https://www.sitemaps.org/schemas/sitemap/0.9";

        /// <summary>
        /// The image extension XML namespace for sitemap image metadata.
        /// </summary>
        public const string ImageSchemaNamespace = "http://www.google.com/schemas/sitemap-image/1.1";
    }
}
