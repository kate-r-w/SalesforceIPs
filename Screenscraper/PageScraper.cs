using HtmlAgilityPack;
using Microsoft.Playwright;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Scraper
{
    public class PageScraper
    {
        private const string _salesforceIpUrl = "https://help.salesforce.com/s/articleView?id=000384438&type=1";

        public static async Task<IList<IpResult>> GetIps()
        {
            var ipResults = new List<IpResult>();

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(_salesforceIpUrl);

            // Wait for the final table to be loaded in the DOM
            await page.WaitForSelectorAsync("(//table)[4]");

            // Get the fully rendered HTML
            var content = await page.ContentAsync();

            // Parse the HTML content
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(content);

            var first = GetBlocksFromFirstTable(htmlDoc);
            var second = GetIpsAndBlocksFromSecondTable(htmlDoc);
            var third = GetFromMyTrailheadTable(htmlDoc);
            var fourth = GetFromByoTable(htmlDoc);

            return first.Union(second).Union(third).Union(fourth).ToList();
        }

        private static IList<IpResult> GetFromByoTable(HtmlDocument htmlDoc)
        {
            //todo: add SF region
            return GetFromTable(htmlDoc, IpType.BYOLLM, "(//table)[4]", ".//td[position()>1]", true);
        }

        private static IList<IpResult> GetFromMyTrailheadTable(HtmlDocument htmlDoc)
        {
            return GetFromTable(htmlDoc, IpType.myTrailhead, "(//table)[3]", ".//td", false);
        }

        private static IList<IpResult> GetIpsAndBlocksFromSecondTable(HtmlDocument htmlDoc)
        {
            return GetFromTable(htmlDoc, IpType.NonHyperforce, "(//table)[2]", ".//td", true);
        }

        private static IList<IpResult> GetBlocksFromFirstTable(HtmlDocument htmlDoc)
        {
            return GetFromTable(htmlDoc, IpType.NonHyperforce, "(//table)[1]", ".//td[1]", true);
        }

        private static IList<IpResult> GetFromTable(HtmlDocument htmlDoc, IpType ipType, string tableSelector, string dataSelector, bool regionFrpmNonIpCell, string? fixedRegion = null)
        {
            // Extract IP blocks from the first column of the first table
            var table = htmlDoc.DocumentNode.SelectSingleNode(tableSelector);
            IList<IpResult> ipResults = new List<IpResult>();
            if (table == null) throw new ScraperUnexpectedDataException($"Table not found with selector: {tableSelector}");
            
            var dataRows = table.SelectNodes(".//tr");
            if (dataRows == null) throw new ScraperUnexpectedDataException($"No data rows found in table with selector: {tableSelector}");
            
            string? region = null;
            foreach (var row in dataRows)
            {
                var columns = row.SelectNodes(dataSelector);
                if (columns != null)
                {
                    foreach (var column in columns)
                    {
                        var ipText = column.InnerText.Trim();
                        var ips = GetValidIpsAndBlocks(column.InnerHtml);
                        if (ips.Count == 0 && !string.IsNullOrEmpty(ipText))
                        {
                            if (regionFrpmNonIpCell)
                            {
                                //assume region name
                                region = ipText;
                            }
                        }
                        else
                        {
                            foreach (var ip in ips) {
                                ipResults.Add(new IpResult
                                {
                                    Ip = ip,
                                    Region = region,
                                    IpType = ipType
                                });
                            }
                        }
                    }
                }
            }

            return ipResults;
        }

        private static List<string> GetValidIpsAndBlocks(string text)
        {
            // Regex matches IPv4 addresses and CIDR blocks
            var matches = System.Text.RegularExpressions.Regex.Matches(
                text,
                @"\b(\d{1,3}\.){3}\d{1,3}(?:/\d{1,2})?\b"
            );

            var results = new List<string>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                results.Add(match.Value);
            }
            return results;
        }
    }

    public class IpResult
    {
        public string Ip { get; set; }
        public string? Region { get; set; }
        public IpType IpType { get; set; }
    }

    public enum IpType
    {
        Hyperforce,
        NonHyperforce,
        myTrailhead,
        BYOLLM
    }

    public class ScraperUnexpectedDataException : Exception
    {
        public ScraperUnexpectedDataException(string? message) : base(message)
        {
        }
    }
}
