using System;
using System.Threading.Tasks;

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

        private async Task DoConversion(string input)
        {
            ConvertButton.Enabled = false;
            ProgressIndicator.Hidden = false;
            ProgressIndicator.StartAnimation(this);

            string dbPath = DBPathTextField.StringValue;
            bool copyTextures = CopyTexturesCheckButton.State == NSCellStateValue.On;
            bool fillTextures = FillTexturesCheckButton.State == NSCellStateValue.On;
            bool generateBlanks = GenerateBlanksCheckButton.State == NSCellStateValue.On;
            bool flipUV = FlipTextureCoordinatesCheckButton.State == NSCellStateValue.On;

            await Task.Run(() =>
            {
                try
                {
                    LDDDB.Load(dbPath,
                    copyTextures,
                    fillTextures,
                    generateBlanks,
                    flipUV);

                    if (input.EndsWith(".lxf", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DAEConversion.ConvertLXF(input);
                    }
                    else if (input.EndsWith(".lxfml", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DAEConversion.ConvertLXFML(input);
                    }
                }
                catch (Exception ex)
                {

                }
            });
            ConvertButton.Enabled = true;
            ProgressIndicator.Hidden = true;
        }

        partial void Convert(NSObject sender)
        {
            string input = InputPathTextField.StringValue;
            DoConversion(input);
        }

        partial void CopyTexturesToggled(NSObject sender)
        {
            FillTexturesCheckButton.Enabled = CopyTexturesCheckButton.State == NSCellStateValue.On;
            if (CopyTexturesCheckButton.State != NSCellStateValue.On)
                FillTexturesCheckButton.State = NSCellStateValue.Off;
        }
    }
}
