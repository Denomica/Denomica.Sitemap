using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Sitemap.Configuration
{
    public class DenomicaSitemapConfigurationBuilder
    {
        public DenomicaSitemapConfigurationBuilder(IServiceCollection services)
        {
            this.Services = services ?? throw new ArgumentNullException(nameof(services), "Service collection cannot be null.");
        }

        public IServiceCollection Services { get; private set; }
    }
}
