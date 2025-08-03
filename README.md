# Denomica.Sitemap
A library that facilitates discovering and enumerating XML sitemaps on a site.

## Sample Applications

This repo contains the following sample applications, that demonstrate how you can use the `Denomica.Sitemap` library.

### CrawlerConsole

This application is a simple console application that takes one command line argument. This argument is assumed to be the URL of a site. The application then uses `Denomica.Sitemap` to enumerate all URLs defines in sitemaps found on the site.

### ContentExtractor

Another console application that takes the following command-line arguments:

1. **URL**: The URL of the site to crawl.
2. **Cookie Consent Selector**: The selector to an element that is used to consent to cookies on the site. This is useful for sites that require cookie consent before displaying content.
3. **Scroll to Selector**: A CSS selector that defines an element to scroll to before extracting content. This is useful for sites that load content dynamically.
4. **CSS Selector**: A CSS selector that defines the content to extract from each discovered page.