using System.Collections.Generic;
using HtmlAgilityPack;
using MonoMac.Foundation;

namespace DZone
{
	public class DZoneProxy
	{
		private PageFetcher fetcher;
		private PageExtractor extractor;
		
		public DZoneProxy()
		{
			fetcher = new PageFetcher();
			extractor = new PageExtractor();
		}
		
		public List<DZoneLink> GetLinks(int page = 0)
		{
			return extractor.ExtractLinks(fetcher.FetchDocument(page));
		}
		
		public class PageFetcher
		{
			public string FetchDocument(int page)
			{
				var p = page > 1 ? "&p=" + page : "";
				var uri = string.Format("http://www.dzone.com/links/index.html?type=html{0}", p);
				
				/* WebClient doesn't work because libc.dylib is not being linked properly by MonoMac
				 * Use Cocoa NSUrlRequest instead as a not-so-pretty-fix
				 * 
				var web = new WebClient();
				web.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 5.1; U; rv:5.0) Gecko/20100101 Firefox/5.0";
				
				return web.DownloadString(uri);
				*/
				
				var pool = new NSAutoreleasePool();
				
				NSError error = null;
				NSUrlResponse response = null;
				var request = NSUrlRequest.FromUrl(NSUrl.FromString(uri));
				var data = NSUrlConnection.SendSynchronousRequest(request, out response, out error);
				var html = (string)NSString.FromData(data, NSStringEncoding.UTF8);
				
				pool.Dispose();
				
				return html;
				
			}
		}
		
		public class PageExtractor
		{
			public List<DZoneLink> ExtractLinks(string html)
			{
				var links = new List<DZoneLink>();
				
				var doc = new HtmlDocument();
				doc.LoadHtml(html);
				
				var details = doc.DocumentNode.SelectNodes("//div[@class='linkblock frontpage ']//div[@class='details']");
				foreach (var detail in details)
				{
					var h3 = detail.SelectSingleNode(".//h3");
					var a = h3.SelectSingleNode(".//a");
					var desc = detail.SelectSingleNode(".//p[@class='description']").FirstChild;
					
					links.Add(new DZoneLink()
					{
						Title = h3.InnerText.Trim(),
						Desc = desc.InnerText.Trim(),
						Href = a.Attributes["href"].Value
					});
				}
				
				return links;
			}
		}
	}
}

