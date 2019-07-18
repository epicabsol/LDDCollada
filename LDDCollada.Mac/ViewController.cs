using System;
using System.Threading.Tasks;

using AppKit;
using Foundation;

namespace LDDCollada.Mac
{
    public partial class ViewController : NSViewController
    {
        private const string LDD_DB_DEFAULT_PATH = "/Applications/LEGO Digital Designer.app/Contents/Resources/Assets/db";

        private static string DefaultDBPath
        {
            get
            {
                string result = NSUserDefaults.StandardUserDefaults.StringForKey(nameof(DefaultDBPath));
                if (result == null)
                {
                    if (System.IO.Directory.Exists(LDD_DB_DEFAULT_PATH))
                        result = LDD_DB_DEFAULT_PATH;
                    else
                        result = "";
                }
                return result;
            }
            set
            {
                NSUserDefaults.StandardUserDefaults.SetString(value, nameof(DefaultDBPath));
            }
        }

        private static bool DefaultCopyTextures
        {
            get
            {
                return NSUserDefaults.StandardUserDefaults.BoolForKey(nameof(DefaultCopyTextures));
            }
            set
            {
                NSUserDefaults.StandardUserDefaults.SetBool(value, nameof(DefaultCopyTextures));
            }
        }

        private static bool DefaultFillTextures
        {
            get
            {
                return NSUserDefaults.StandardUserDefaults.BoolForKey(nameof(DefaultFillTextures));
            }
            set
            {
                NSUserDefaults.StandardUserDefaults.SetBool(value, nameof(DefaultFillTextures));
            }
        }

        private static bool DefaultGenerateBlanks
        {
            get
            {
                return NSUserDefaults.StandardUserDefaults.BoolForKey(nameof(DefaultGenerateBlanks));
            }
            set
            {
                NSUserDefaults.StandardUserDefaults.SetBool(value, nameof(DefaultGenerateBlanks));
            }
        }

        private static bool DefaultFlipUV
        {
            get
            {
                return NSUserDefaults.StandardUserDefaults.BoolForKey(nameof(DefaultFlipUV));
            }
            set
            {
                NSUserDefaults.StandardUserDefaults.SetBool(value, nameof(DefaultFlipUV));
            }
        }

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Do any additional setup after loading the view.
            DBPathTextField.StringValue = DefaultDBPath;
            CopyTexturesCheckButton.State = DefaultCopyTextures ? NSCellStateValue.On : NSCellStateValue.Off;
            FillTexturesCheckButton.State = DefaultFillTextures ? NSCellStateValue.On : NSCellStateValue.Off;
            GenerateBlanksCheckButton.State = DefaultGenerateBlanks ? NSCellStateValue.On : NSCellStateValue.Off;
            FlipTextureCoordinatesCheckButton.State = DefaultFlipUV ? NSCellStateValue.On : NSCellStateValue.Off;
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
            bool fillTextures = FillTexturesCheckButton.State == NSCellStateValue.On && FillTexturesCheckButton.Enabled;
            bool generateBlanks = GenerateBlanksCheckButton.State == NSCellStateValue.On;
            bool flipUV = FlipTextureCoordinatesCheckButton.State == NSCellStateValue.On;

            DefaultDBPath = dbPath;
            DefaultCopyTextures = copyTextures;
            DefaultFillTextures = fillTextures;
            DefaultGenerateBlanks = generateBlanks;
            DefaultFlipUV = flipUV;

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
