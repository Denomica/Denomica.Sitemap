using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Sitemap.Model
{
    /// <summary>
    /// Represents a single URL entry extracted from a sitemap URL set.
    /// </summary>
    public class UrlsetUrl
    {
        /// <summary>
        /// Gets or sets the page URL.
        /// </summary>
        public Uri Location { get; set; }

        /// <summary>
        /// Gets or sets the optional last-modified timestamp for the page URL.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// Gets or sets optional image metadata associated with the page URL.
        /// </summary>
        public Image? Image { get; set; }
    }
}
