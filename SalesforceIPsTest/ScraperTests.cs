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

        [Fact]
        public async void AllNonHyperforceHaveRegion()
        {
            var something = await PageScraper.GetIps();
            foreach (var item in something.Where(x => x.IpType == IpType.NonHyperforce))
            {
                Assert.NotNull(item.Region);
            }
        }
    }
}