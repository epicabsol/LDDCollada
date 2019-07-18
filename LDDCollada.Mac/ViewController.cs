using System;

using AppKit;
using Foundation;

namespace LDDCollada.Mac
{
    public partial class ViewController : NSViewController
    {
        private const string LDD_DB_DEFAULT_PATH = "/Applications/LEGO Digital Designer.app/Contents/Resources/Assets/db";

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Do any additional setup after loading the view.
            if (System.IO.Directory.Exists(LDD_DB_DEFAULT_PATH))
                DBPathTextField.StringValue = LDD_DB_DEFAULT_PATH;
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }

        partial void BrowseDB(NSObject sender)
        {
            NSOpenPanel panel = NSOpenPanel.OpenPanel;
            panel.CanChooseFiles = false;
            panel.CanChooseDirectories = true;
            panel.TreatsFilePackagesAsDirectories = true;

            if (panel.RunModal() == 1)
            {
                DBPathTextField.StringValue = panel.Urls[0].Path;
            }
        }

        partial void BrowseInput(NSObject sender)
        {
            NSOpenPanel panel = NSOpenPanel.OpenPanel;
            panel.CanChooseFiles = true;
            panel.CanChooseDirectories = true;
            panel.AllowedFileTypes = new string[] { "lxf", "lxfml" };

            if (panel.RunModal() == 1)
            {
                InputPathTextField.StringValue = panel.Urls[0].Path;
            }
        }

        partial void Convert(NSObject sender)
        {
            LDDDB.Load(DBPathTextField.StringValue,
                CopyTexturesCheckButton.State == NSCellStateValue.On,
                FillTexturesCheckButton.State == NSCellStateValue.On,
                GenerateBlanksCheckButton.State == NSCellStateValue.On,
                FlipTextureCoordinatesCheckButton.State == NSCellStateValue.On);

            string input = InputPathTextField.StringValue;
            if (input.EndsWith(".lxf", StringComparison.InvariantCultureIgnoreCase))
            {
                DAEConversion.ConvertLXF(input);
            }
            else if (input.EndsWith(".lxfml", StringComparison.InvariantCultureIgnoreCase))
            {
                DAEConversion.ConvertLXFML(input);
            }
            else
            {
                NSAlert alert = NSAlert.WithMessage("Not an LXF or LXFML file!", "", "", "", "");
            }
        }

        partial void CopyTexturesToggled(NSObject sender)
        {
            FillTexturesCheckButton.Enabled = CopyTexturesCheckButton.State == NSCellStateValue.On;
            if (CopyTexturesCheckButton.State != NSCellStateValue.On)
                FillTexturesCheckButton.State = NSCellStateValue.Off;
        }
    }
}
