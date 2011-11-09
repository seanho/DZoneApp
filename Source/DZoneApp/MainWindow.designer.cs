// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace DZoneApp
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		MonoMac.WebKit.WebView webView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSCollectionView collectionView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator pageIndicator { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator linkIndicator { get; set; }

		[Outlet]
		MonoMac.AppKit.NSToolbarItem refreshBarItem { get; set; }

		[Outlet]
		MonoMac.AppKit.NSToolbarItem openInBrowserBarItem { get; set; }

		[Action ("onRefresh:")]
		partial void onRefresh (MonoMac.Foundation.NSObject sender);

		[Action ("onOpenInBrowser:")]
		partial void onOpenInBrowser (MonoMac.Foundation.NSObject sender);
	}

	[Register ("MainWindow")]
	partial class MainWindow
	{
	}
}
