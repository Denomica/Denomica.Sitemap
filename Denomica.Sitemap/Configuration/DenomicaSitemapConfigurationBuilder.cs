using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Sitemap.Configuration
{
    /// <summary>
    /// Provides a fluent configuration surface for Denomica sitemap service registration.
    /// </summary>
    public class DenomicaSitemapConfigurationBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DenomicaSitemapConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection used to register sitemap services.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        public DenomicaSitemapConfigurationBuilder(IServiceCollection services)
        {
            this.Services = services ?? throw new ArgumentNullException(nameof(services), "Service collection cannot be null.");
        }

        /// <summary>
        /// Gets the underlying service collection being configured.
        /// </summary>
        public IServiceCollection Services { get; private set; }
    }
}
