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

        [Fact]
        public async void Has137Ips()
        {
            var something = await PageScraper.GetIps();
            Assert.Equal(137, something.Count);
        }
    }
}