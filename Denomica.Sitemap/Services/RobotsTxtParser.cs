using Denomica.Sitemap.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Sitemap.Services
{
    /// <summary>
    /// Provides functionality to parse and retrieve information from robots.txt files.
    /// </summary>
    /// <remarks>The <see cref="RobotsTxtParser"/> class offers methods to extract specific lines for user
    /// agents, retrieve sitemap URLs, and fetch the content of robots.txt files from websites. It utilizes an <see
    /// cref="IHttpClientFactory"/> to create HTTP clients for network operations.</remarks>
    public class RobotsTxtParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RobotsTxtParser"/> class with a specified HTTP client factory.
        /// </summary>
        /// <param name="clientFactory">The factory used to create an <see cref="HttpClient"/> instance. Must not be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="clientFactory"/> is <see langword="null"/>.</exception>
        public RobotsTxtParser(IHttpClientFactory clientFactory)
        {
            this.Client = clientFactory?.CreateClient(Constants.HttpClientName)
                ?? throw new ArgumentNullException(nameof(clientFactory), "HttpClientFactory must provide a valid HttpClient.");
        }

        private readonly HttpClient Client;

        /// <summary>
        /// Asynchronously reads and returns non-empty, non-comment lines from the provided robots.txt content.
        /// </summary>
        /// <remarks>Lines that are empty or start with a '#' character are considered comments and are
        /// excluded from the results.</remarks>
        /// <param name="robotsTxtContent">The content of a robots.txt file as a string. Can be null or empty.</param>
        /// <returns>An asynchronous stream of strings, each representing a line from the robots.txt content that is not empty or
        /// a comment. If <paramref name="robotsTxtContent"/> is null or empty, the stream will be empty.</returns>
        public async IAsyncEnumerable<string> GetLinesAsync(string? robotsTxtContent)
        {
            if (string.IsNullOrWhiteSpace(robotsTxtContent))
            {
                yield break;
            }
            using var reader = new System.IO.StringReader(robotsTxtContent);
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                {
                    yield return line.Trim();
                }
            }
        }

        /// <summary>
        /// Asynchronously retrieves lines from a robots.txt content that are specific to a given user agent.
        /// </summary>
        /// <remarks>This method processes the robots.txt content line by line, identifying sections that
        /// apply to the specified user agent. It yields lines that belong to the user agent's section, excluding the
        /// "User-agent" lines themselves.</remarks>
        /// <param name="robotsTxtContent">The content of the robots.txt file. Can be <see langword="null"/> if no content is available.</param>
        /// <param name="userAgent">The user agent string to filter the lines for. This string is compared case-insensitively.</param>
        /// <returns>An asynchronous stream of lines that are applicable to the specified user agent. The stream will be empty if
        /// no matching lines are found.</returns>
        public async IAsyncEnumerable<string> GetUserAgentLinesAsync(string? robotsTxtContent, string userAgent)
        {
            bool isUserAgentSection = false;
            await foreach (var line in GetLinesAsync(robotsTxtContent))
            {
                if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                {
                    isUserAgentSection = line.Trim().EndsWith(userAgent, StringComparison.OrdinalIgnoreCase);
                }
                else if (isUserAgentSection && !line.StartsWith("User-agent", StringComparison.OrdinalIgnoreCase))
                {
                    yield return line;
                }
                else if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                {
                    isUserAgentSection = false;
                }
            }
        }

        /// <summary>
        /// Asynchronously retrieves sitemap URLs from the provided robots.txt content.
        /// </summary>
        /// <remarks>This method parses each line of the provided robots.txt content and yields any URLs
        /// that are prefixed with "Sitemap:". Only valid absolute URIs are returned. The method is case-insensitive
        /// with respect to the "Sitemap:" prefix.</remarks>
        /// <param name="robotsTxtContent">The content of a robots.txt file as a string. Can be <see langword="null"/> or empty, in which case no
        /// sitemaps will be returned.</param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the absolute URLs of the sitemaps found in
        /// the robots.txt content.</returns>
        public async IAsyncEnumerable<Uri> GetSitemapsAsync(string? robotsTxtContent)
        {
            await foreach (var line in GetLinesAsync(robotsTxtContent))
            {
                if (line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                {
                    var url = line.Substring("Sitemap:".Length).Trim();
                    if (Uri.TryCreate(url, UriKind.Absolute, out var sitemapUri))
                    {
                        yield return sitemapUri;
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously retrieves the sitemap URLs from the robots.txt file of the specified website.
        /// </summary>
        /// <param name="site">The base URI of the website from which to retrieve sitemap URLs. Must not be null.</param>
        /// <returns>An asynchronous stream of <see cref="Uri"/> objects representing the sitemap URLs found in the robots.txt
        /// file.</returns>
        public async IAsyncEnumerable<Uri> GetSitemapsAsync(Uri site)
        {
            var content = await GetRobotsTxtContentAsync(site);
            await foreach (var sitemap in GetSitemapsAsync(content))
            {
                yield return sitemap;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the content of the robots.txt file from the specified URL.
        /// </summary>
        /// <remarks>This method sends an HTTP GET request to the "/robots.txt" path of the specified URL.
        /// If the request is successful, the content of the robots.txt file is returned. If the request fails or the
        /// response is not successful, <see langword="null"/> is returned.</remarks>
        /// <param name="url">The base URL from which to retrieve the robots.txt file. Must be a valid URI.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content of the robots.txt
        /// file as a string, or <see langword="null"/> if the file could not be retrieved or the request was
        /// unsuccessful.</returns>
        public async Task<string?> GetRobotsTxtContentAsync(Uri url)
        {
            var robotsTxtUrl = new Uri(url, "/robots.txt");

            var request = new HttpRequestMessage(HttpMethod.Get, robotsTxtUrl);
            try
            {
                var response = await this.Client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var contents = await response.Content.ReadAsStringAsync();
                    return contents;
                }
            }
            catch (HttpRequestException)
            {
            }

            return null;
        }

    }
}
