using System;
using System.Threading;
using System.Windows.Forms;
using Plex.Infrastructure.Configuration;

namespace Plex.Entry
{
    public partial class Form1 : Form
    {
        private readonly Infrastructure.Application _ap;

        private readonly Thread _thread;

        public Form1(string cfgFileName = null)
        {
            if(cfgFileName == null)
                Thread.Sleep(3000);
            InitializeComponent();
            _ap = new Infrastructure.Application(cfgFileName);
            
            _thread = new Thread(Start);
            _thread.Start();
        }

        public void Start()
        {
            Thread.Sleep(2000);
            Invoke(new Action(() => { Text = Settings.Get().Services.Name; }));
            _ap.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _ap.Stop();
        }
    }
}
