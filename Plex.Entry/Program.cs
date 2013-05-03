using System;
using System.Linq;
using System.Windows.Forms;

namespace Plex.Entry
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var cfgLine = args.FirstOrDefault(p => p.StartsWith("cfg="));
            if (cfgLine != null)
                cfgLine = cfgLine.Substring(4);
            Application.Run(new Form1(cfgLine));
        }
    }
}
