using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Sitemap.Services
{
    public class UrlsetUrl
    {
        public Uri Location { get; set; }

        public DateTimeOffset? LastModified { get; set; }
    }
}
