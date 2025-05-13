using Scraper;

namespace SalesforceIPsTest
{
    public class ScraperTests
    {
        [Fact]
        public async void GetsNonHyperForceBlocksFromPage()
        {
            var something = await PageScraper.GetIps();
            Assert.NotNull(something);
        }
    }
}