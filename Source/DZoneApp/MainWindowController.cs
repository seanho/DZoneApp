using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DZone;
using MonoMac;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace DZoneApp
{
	public partial class MainWindowController : NSWindowController
	{
		private List<DLinkModel> models = new List<DLinkModel>();
		private int lastPage = 0;
		private bool loadingLinks = false;
		
		#region Constructors
		
		// Called when created from unmanaged code
		public MainWindowController(IntPtr handle) : base (handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController(NSCoder coder) : base (coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowController() : base ("MainWindow")
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
			Window.Title = "DZone Reader";
		}
		
		#endregion
		
		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			
			NSNotificationCenter.DefaultCenter.AddObserver((NSString)"DLinkViewItemSelected", DLinkViewItemSelected);
			
			webView.FinishedLoad += (sender, e) => 
			{
				pageIndicator.StopAnimation(this);
				pageIndicator.Hidden = true;
			};
			//LoadUrl(NSUrl.FromString("http://www.google.com"));
			
			var collectionViewItem = new DLinkViewItem();
			collectionView.ItemPrototype = collectionViewItem;
			
			var prototypeView = new DLinkView(new RectangleF(10, 10, 350, 80));
			prototypeView.TitleLabel.Bind("value", collectionViewItem, "representedObject.title", null);
			prototypeView.DescLabel.Bind("value", collectionViewItem, "representedObject.desc", null);
			collectionViewItem.View = prototypeView;
			
			var contentView = collectionView.EnclosingScrollView.ContentView;
			contentView.PostsBoundsChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.NSViewBoundsDidChangeNotification, ContentBoundsDidChange, contentView);
			
			// Path.Combine (NSBundle.MainBundle.ResourceUrl.Path, "Images", "back.png")
			refreshBarItem.Image.Template = true;
			openInBrowserBarItem.Image.Template = true;
			copyUrlBarItem.Image.Template = true;
			backBarItem.Image.Template = true;
			forwardBarItem.Image.Template = true;
			
			LoadLinks();
		}
		
		public new MainWindow Window
		{
			get { return (MainWindow)base.Window; }
		}
		
		partial void onRefresh(NSObject sender)
		{
			if (!loadingLinks)
			{
				lastPage = 0;
				models.Clear();
				
				LoadLinks();
			}
		}
		
		partial void onOpenInBrowser(NSObject sender)
		{
			if (!string.IsNullOrEmpty(webView.MainFrameUrl))
			{
				NSWorkspace.SharedWorkspace.OpenUrl(NSUrl.FromString(webView.MainFrameUrl));
			}
		}
		
		partial void onCopyUrl(NSObject sender)
		{
			if (!string.IsNullOrEmpty(webView.MainFrameUrl))
			{
				NSPasteboard.GeneralPasteboard.DeclareTypes(new string[] { NSPasteboard.NSStringType }, null);
				NSPasteboard.GeneralPasteboard.SetStringForType(webView.MainFrameUrl, NSPasteboard.NSStringType);
			}
		}
		
		partial void onBack(NSObject sender)
		{
			if (webView.CanGoBack())
			{
				webView.GoBack();
			}
		}
		
		partial void onForward(NSObject sender)
		{
			if (webView.CanGoForward())
			{
				webView.GoForward();
			}
		}
		
		private void LoadUrl(NSUrl url)
		{
			pageIndicator.Hidden = false;
			pageIndicator.StartAnimation(this);
			webView.MainFrame.LoadRequest(NSUrlRequest.FromUrl(url));
		}
		
		private void LoadLinks()
		{
			if (loadingLinks) return;
			loadingLinks = true;
			
			linkIndicator.Hidden = false;
			linkIndicator.StartAnimation(this);
			
			ThreadPool.QueueUserWorkItem(obj =>
			{
				var links = new DZone.DZoneProxy().GetLinks(lastPage + 1);
				Console.WriteLine("Number of links: {0}", links.Count);
				
				links.ForEach(l => models.Add(new DLinkModel(l)));
				
				BeginInvokeOnMainThread(delegate
				{
					collectionView.Content = models.ToArray();
					lastPage++;
					loadingLinks = false;
					linkIndicator.Hidden = true;
					linkIndicator.StopAnimation(this);
				});
			});
		}
			
		private void ContentBoundsDidChange(NSNotification notification)
		{
			var contentView = (NSClipView)notification.Object;
			
			var offsetY = contentView.DocumentVisibleRect().Location.Y;
			var visibleHeight = contentView.Frame.Height;
			var contentHeight = collectionView.Frame.Height;
			
			if (offsetY == contentHeight - visibleHeight)
			{
				LoadLinks();
			}
		}
			
		private void DLinkViewItemSelected(NSNotification notification)
		{
			var model = (DLinkModel)notification.UserInfo.ObjectForKey((NSString)"Model");
			
			if (model != null)
			{
				Console.WriteLine("Selected: {0} - {1}", model.Title, model.Href);
				
				LoadUrl(NSUrl.FromString((string)model.Href));
			}
		}
	}
	
	#region DLinkModel
	
	public class DLinkModel : NSObject
	{
		[Export("title")]
		public NSString Title { get; set; }
		
		[Export("desc")]
		public NSString Desc { get; set; }
		
		[Export("href")]
		public NSString Href { get; set; }
		
		public DLinkModel()
		{
		}
		
		public DLinkModel(DZoneLink link)
		{
			Title = (NSString)link.Title;
			Desc = (NSString)link.Desc;
			Href = (NSString)link.Href;
		}
	}
	
	#endregion
	
	#region DLinkViewItem
	
	public class DLinkViewItem : NSCollectionViewItem
	{
		private DLinkModel model;
		
		public override NSObject RepresentedObject
		{
			get { return model; }
			set
			{
				if (value == null) return;
				
				base.RepresentedObject = value;
				model = (DLinkModel)value;
			}
		}
		
		public override bool Selected
		{
			get { return base.Selected; }
			set
			{
				base.Selected = value;
				
				((DLinkView)View).Selected = value;
				
				if (model != null && value)
				{
					var userInfo = NSDictionary.FromObjectsAndKeys(
						new object[] { model },
						new object[] { (NSString)"Model" });
					NSNotificationCenter.DefaultCenter.PostNotificationName("DLinkViewItemSelected", this, userInfo);
				}
			}
		}
		
		public DLinkViewItem()
		{
		}
		
		public DLinkViewItem(IntPtr handle) : base(handle)
		{
		}
	}
	
	#endregion
	
	#region DLinkView
	
	public class DLinkView : NSView
	{
		public NSTextField TitleLabel { get; private set; }
		public NSTextField DescLabel { get; private set; }
		
		private bool selected;
		public bool Selected
		{
			get { return selected; }
			set
			{
				selected = value;
				NeedsDisplay = true;
			}
		}
		
		[DllImport (Constants.AppKitLibrary, EntryPoint="NSRectFill")]
		public extern static void RectFill(RectangleF rect);
		
		public DLinkView()
		{
			Initialize();
		}
		
		public DLinkView(RectangleF frame) : base(frame)
		{
			Initialize();
		}
		
		public DLinkView(IntPtr handle) : base(handle)
		{
			Initialize();
		}
		
		private void Initialize()
		{
			TitleLabel = new NSTextField(RectangleF.Empty);
			TitleLabel.Frame = new RectangleF(10, 52, 350 - 20, 20);
			TitleLabel.AutoresizingMask = NSViewResizingMask.WidthSizable | 
				NSViewResizingMask.MinXMargin |
				NSViewResizingMask.MinYMargin |
				NSViewResizingMask.MaxXMargin;
			TitleLabel.Font = NSFont.SystemFontOfSize(13);
			TitleLabel.TextColor = NSColor.FromDeviceRgba(0.16f, 0.52f, 0.91f, 1.0f);
			TitleLabel.DrawsBackground = false;
			TitleLabel.Bordered = false;
			TitleLabel.Editable = false;
			TitleLabel.Selectable = false;
			AddSubview(TitleLabel);
			
			DescLabel = new NSTextField(RectangleF.Empty);
			DescLabel.Frame = new RectangleF(10, 12, 350 - 20, 40);
			DescLabel.AutoresizingMask = NSViewResizingMask.WidthSizable | 
				NSViewResizingMask.MinXMargin |
				NSViewResizingMask.MinYMargin |
				NSViewResizingMask.MaxXMargin;
			DescLabel.Font = NSFont.SystemFontOfSize(11);
			DescLabel.TextColor = NSColor.DisabledControlText;
			DescLabel.DrawsBackground = false;
			DescLabel.Bordered = false;
			DescLabel.Editable = false;
			DescLabel.Selectable = false;
			AddSubview(DescLabel);
		}
		
		public override void DrawRect(RectangleF dirtyRect)
		{
			if (selected)
			{
				NSColor.FromDeviceRgba(0.78f, 0.90f, 1.0f, 1.0f).Set();
				RectFill(dirtyRect);
			}
			base.DrawRect(dirtyRect);
		}
	}
	
	#endregion
}

