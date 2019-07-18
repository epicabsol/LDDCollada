using System;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using SharpDX;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LDDCollada
{
    public class Program
    {
        private const string INI_FILENAME = "LDDCollada.ini";
        public static INIFile Config;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("LDDCollada by benji for kaltathenoblemind\n");
                Console.WriteLine("When calling LDDCollada, pass in filenames of .lxfml files to convert.\n");
                Console.WriteLine("If the directory that contains the extracted contents of db.lif is not stored in the LDDCollada.ini file, you will be prompted to browse to it.");
                return;
            }

            Config = new INIFile(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), INI_FILENAME));

            // Make sure we have the folder to the part DB
            string partDBPath = Config.GetValueOrDefault("LDDCollada", "PartDBPath", null);
            if (partDBPath == null)
            {
                FolderBrowserDialog browser = new FolderBrowserDialog();
                if (browser.ShowDialog() == DialogResult.OK)
                {
                    partDBPath = browser.SelectedPath;
                    Config.SetValue("LDDCollada", "PartDBPath", partDBPath);
                }
                else
                {
                    return;
                }
            }

            LDDDB.Load(partDBPath, true, true, true, true);
             
            foreach (string filename in args)
            {
                if (filename.EndsWith(".lxfml", StringComparison.InvariantCultureIgnoreCase))
                {
                    DAEConversion.ConvertLXFML(filename);
                }
                else if (filename.EndsWith(".lxf", StringComparison.InvariantCultureIgnoreCase))
                {
                    DAEConversion.ConvertLXF(filename);
                }
                else
                {
                    Console.WriteLine("[WARNING]: Filename does not end in .lxf or .lxfml: '" + filename + "'!");
                }
            }

            Config.Write(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), INI_FILENAME));

#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}
