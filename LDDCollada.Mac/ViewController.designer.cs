// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LDDCollada.Mac
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSButton ConvertButton { get; set; }

		[Outlet]
		AppKit.NSButton CopyTexturesCheckButton { get; set; }

		[Outlet]
		AppKit.NSTextField DBPathTextField { get; set; }

		[Outlet]
		AppKit.NSButton FillTexturesCheckButton { get; set; }

		[Outlet]
		AppKit.NSButton FlipTextureCoordinatesCheckButton { get; set; }

		[Outlet]
		AppKit.NSButton GenerateBlanksCheckButton { get; set; }

		[Outlet]
		AppKit.NSTextField InputPathTextField { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator ProgressIndicator { get; set; }

		[Action ("BrowseDB:")]
		partial void BrowseDB (Foundation.NSObject sender);

		[Action ("BrowseInput:")]
		partial void BrowseInput (Foundation.NSObject sender);

		[Action ("Convert:")]
		partial void Convert (Foundation.NSObject sender);

		[Action ("CopyTexturesToggled:")]
		partial void CopyTexturesToggled (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (ProgressIndicator != null) {
				ProgressIndicator.Dispose ();
				ProgressIndicator = null;
			}

			if (ConvertButton != null) {
				ConvertButton.Dispose ();
				ConvertButton = null;
			}

			if (CopyTexturesCheckButton != null) {
				CopyTexturesCheckButton.Dispose ();
				CopyTexturesCheckButton = null;
			}

			if (DBPathTextField != null) {
				DBPathTextField.Dispose ();
				DBPathTextField = null;
			}

			if (FillTexturesCheckButton != null) {
				FillTexturesCheckButton.Dispose ();
				FillTexturesCheckButton = null;
			}

			if (FlipTextureCoordinatesCheckButton != null) {
				FlipTextureCoordinatesCheckButton.Dispose ();
				FlipTextureCoordinatesCheckButton = null;
			}

			if (GenerateBlanksCheckButton != null) {
				GenerateBlanksCheckButton.Dispose ();
				GenerateBlanksCheckButton = null;
			}

			if (InputPathTextField != null) {
				InputPathTextField.Dispose ();
				InputPathTextField = null;
			}
		}
	}
}
