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
                    .AddSingleton<RobotsTxtParser>()
                    .AddScoped<SitemapCrawler>()
                    .AddHttpClient(Constants.HttpClientName, client =>
                    {
                        client.DefaultRequestHeaders.Add("Accept", "*/*");
                        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                        client.DefaultRequestHeaders.Add("User-Agent", Constants.DefaultUserAgent);
                        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    })
                    .AddHttpMessageHandler(() =>
                    {
                        return new RefererDelegatingHandler();
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        AllowAutoRedirect = true,
                        UseCookies = true,
                        CookieContainer = new CookieContainer(),
                        UseDefaultCredentials = false,
                        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                    }).Services
            );
        }
    }
}
