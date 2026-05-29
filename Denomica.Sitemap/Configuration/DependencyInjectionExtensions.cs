using Denomica.Sitemap.Configuration;
using Denomica.Sitemap.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for adding sitemap-related services to an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Registers the core Denomica sitemap services and default named <see cref="HttpClient"/> in the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to register sitemap services into.</param>
        /// <returns>A configuration builder for additional Denomica sitemap customization.</returns>
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

        /// <summary>
        /// Configures the sitemap crawler to use a custom <see cref="HttpClient"/> implementation.
        /// </summary>
        /// <typeparam name="TClient">The concrete <see cref="HttpClient"/> type to register.</typeparam>
        /// <param name="builder">The sitemap configuration builder.</param>
        /// <returns>The same configuration builder instance for chaining.</returns>
        public static DenomicaSitemapConfigurationBuilder WithHttpClient<TClient>(this DenomicaSitemapConfigurationBuilder builder) where TClient : HttpClient
        {
            builder.Services.AddHttpClient<HttpClient, TClient>(Constants.HttpClientName);
            return builder;
        }

        /// <summary>
        /// Configures the sitemap crawler to use a provided <see cref="HttpClient"/> instance.
        /// </summary>
        /// <param name="builder">The sitemap configuration builder.</param>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance to use.</param>
        /// <returns>The same configuration builder instance for chaining.</returns>
        public static DenomicaSitemapConfigurationBuilder WithHttpClient(this DenomicaSitemapConfigurationBuilder builder, HttpClient httpClient)
        {
            builder.Services.AddHttpClient<HttpClient, HttpClient>(Constants.HttpClientName, c => httpClient);
            return builder;
        }

        /// <summary>
        /// Configures the sitemap crawler to resolve its <see cref="HttpClient"/> by using the specified factory.
        /// </summary>
        /// <param name="builder">The sitemap configuration builder.</param>
        /// <param name="clientFactory">A factory that creates the <see cref="HttpClient"/> from the current service provider.</param>
        /// <returns>The same configuration builder instance for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided factory returns <see langword="null"/>.</exception>
        public static DenomicaSitemapConfigurationBuilder WithHttpClient(this DenomicaSitemapConfigurationBuilder builder, Func<IServiceProvider, HttpClient> clientFactory)
        {
            builder.Services.AddHttpClient<HttpClient, HttpClient>(Constants.HttpClientName, (c, sp) =>
            {
                var httpClient = clientFactory(sp);
                if (httpClient == null)
                {
                    throw new InvalidOperationException("The provided HttpClient factory returned null.");
                }
                return httpClient;
            });
            return builder;
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
