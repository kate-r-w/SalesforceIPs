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

            // Wait for the first table to be loaded in the DOM
            await page.WaitForSelectorAsync("table");

            // Get the fully rendered HTML
            var content = await page.ContentAsync();

            // Parse the HTML content
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(content);

            return GetBlocksFromFirstTable(htmlDoc);
        }

        private static IList<IpResult> GetIpsAndBlocksFromSecondTable(HtmlDocument htmlDoc)
        {
            var tableSelector = "//table[2]";
            throw new NotImplementedException();
        }

        private static IList<IpResult> GetBlocksFromFirstTable(HtmlDocument htmlDoc)
        {
            return GetFromTable(htmlDoc, IpType.NonHyperforce, "//table[1]", true);
        }

        private static IList<IpResult> GetFromTable(HtmlDocument htmlDoc, IpType ipType, string tableSelector, bool regionFromHeader, string? fixedRegion = null)
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
                var firstColumn = row.SelectSingleNode(".//td[1]");
                if (firstColumn != null)
                {
                    var ipText = firstColumn.InnerText.Trim();
                    if (!IsValidIpBlock(ipText))
                    {
                        if (regionFromHeader)
                        {
                            //assume region name
                            region = ipText;
                        }
                    }
                    else
                    {
                        ipResults.Add(new IpResult
                        {
                            Ip = ipText,
                            Region = region,
                            IpType = ipType
                        });
                    }
                }
            }

            return ipResults;
        }

        private static bool IsValidIpBlock(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^(\d{1,3}\.){3}\d{1,3}\/\d{1,2}$");
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
