using Denomica.Sitemap.Configuration;
using System;
using System.Net;
using System.Net.Http;

namespace Denomica.Sitemap.Services
{
    /// <summary>
    /// Creates configured HTTP requests for sitemap and robots.txt retrieval operations.
    /// </summary>
    public interface IHttpRequestFactory
    {
        /// <summary>
        /// Creates a configured <see cref="HttpRequestMessage"/> for the specified method and URI.
        /// </summary>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="requestUri">The target URI for the request.</param>
        /// <returns>A configured request message.</returns>
        HttpRequestMessage Create(HttpMethod method, Uri requestUri);
    }

    internal sealed class HttpRequestFactory : IHttpRequestFactory
    {
        public HttpRequestMessage Create(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri)
            {
                Version = HttpVersion.Version11
            };

            request.Headers.UserAgent.ParseAdd(Constants.DefaultUserAgent);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7");
            request.Headers.Connection.Add("keep-alive");

            return request;
        }
    }
}