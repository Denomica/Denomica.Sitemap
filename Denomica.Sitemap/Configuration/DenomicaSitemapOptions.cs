using System;
using System.Threading.Tasks;

namespace Denomica.Sitemap.Configuration
{
    public class DenomicaSitemapOptions
    {
        public Func<Uri, Task<string?>>? ForbiddenContentFallbackAsync { get; set; }
    }
}
