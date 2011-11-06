using System;
using DZone;
using NUnit.Framework;
using HtmlAgilityPack;

namespace DZoneTests
{
	[TestFixture]
	public class DZoneProxyTestFixture
	{
		[Test]
		public void ShouldFetchRawContent()
		{
			var fetcher = new DZoneProxy.PageFetcher();
			var html = fetcher.FetchDocument(1);
			
			Assert.IsNotNull(html, "HTML is null");
			Assert.IsNotEmpty(html, "HTML is empty");
		}
		
		[Test]
		public void ShouldExtractLinks()
		{
			var html = "<html><body><div class='linkblock frontpage '><div class='details'><h3><a href='www.link1.com'></a>L1</h3><p class='description'>LinkOne</p></div></div><div class='linkblock frontpage '><div class='details'><h3><a href='www.link2.com'></a>L2</h3><p class='description'>LinkTwo</p></div></div></body></html>";
			
			var extractor = new DZoneProxy.PageExtractor();
			var links = extractor.ExtractLinks(html);
			
			Assert.AreEqual(2, links.Count, "Link's count should be equal to 2");
			Assert.AreEqual("L1", links[0].Title);
			Assert.AreEqual("LinkOne", links[0].Desc);
			Assert.AreEqual("www.link1.com", links[0].Href);
			Assert.AreEqual("L2", links[1].Title);
			Assert.AreEqual("LinkTwo", links[1].Desc);
			Assert.AreEqual("www.link2.com", links[1].Href);
		}
	}
}

