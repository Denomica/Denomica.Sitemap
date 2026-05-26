using Denomica.Sitemap.Configuration;
using System;
using System.Net;
using System.Net.Http;

namespace Denomica.Sitemap.Services
{
    public interface IHttpRequestFactory
    {
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
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Connection.Add("keep-alive");

            return request;
        }
    }
}