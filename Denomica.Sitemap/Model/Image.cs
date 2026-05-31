using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Sitemap.Model
{
    /// <summary>
    /// Represents image metadata declared for a sitemap URL.
    /// </summary>
    public class Image
    {
        /// <summary>
        /// Gets or sets the absolute URL of the image.
        /// </summary>
        public Uri Location { get; set; }

        /// <summary>
        /// Gets or sets the image title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the image caption.
        /// </summary>
        public string? Caption { get; set; }

    }
}
