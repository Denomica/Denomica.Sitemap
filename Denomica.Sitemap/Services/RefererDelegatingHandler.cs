using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Denomica.Sitemap.Services
{
    public class RefererDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Host = $"{request.RequestUri.Host}:{request.RequestUri.Port}";
            request.Headers.Referrer = new Uri(request.RequestUri, "/");
            request.Headers.Add("Origin", request.Headers.Referrer.ToString());
            return base.SendAsync(request, cancellationToken);
        }
    }
}
