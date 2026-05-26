using Denomica.Sitemap.Configuration;
using Denomica.Sitemap.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for adding sitemap-related services to an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        public static DenomicaSitemapConfigurationBuilder AddDenomicaSitemap(this IServiceCollection services)
        {
            return new DenomicaSitemapConfigurationBuilder(
                services
                    .AddSingleton<IHttpRequestFactory, HttpRequestFactory>()
                    .AddSingleton<RobotsTxtParser>()
                    .AddScoped<SitemapCrawler>()
                    .AddHttpClient(Constants.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHttpMessageHandler)
                    .Services
            );
        }

        private static HttpMessageHandler CreatePrimaryHttpMessageHandler()
        {
            return new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                UseDefaultCredentials = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        }
    }
}
