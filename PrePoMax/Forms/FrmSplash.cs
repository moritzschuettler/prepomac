using PrePoMax.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrePoMax.Forms
{
    public partial class FrmSplash : Form
    {
        private Size _collapsedSize = new Size(600, 280);
        private Size _expandedSize = new Size(600, 380);
        public FrmSplash()
        {
            InitializeComponent();
            //
            ShowHelp = false;
            labProgramName.Text = Globals.ProgramName;
            // Select the image
            Random rand = new Random(DateTime.Now.Millisecond);
            int splashId = rand.Next(2) + 1;
            //
            if (splashId == 1) BackgroundImage = Resources.Splash_01;
            else if (splashId == 2) BackgroundImage = Resources.Splash_02;
        }
        public bool ShowHelp
        {
            set
            {
                llHomePage.Visible = value;
                llClose.Visible = value;
                //progressBar.Visible = !value;
                if (value) progressBar.Style = ProgressBarStyle.Continuous;
                else progressBar.Style = ProgressBarStyle.Marquee;
                //
                Size size;
                if (value) size = _expandedSize;
                else size = _collapsedSize;
                //
                this.MinimumSize = size;
                this.MaximumSize = size;
            }
        }
        public void Close(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
            this.Close();
        }
        private void llClose_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
        }
        private void llHomePage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(Globals.HomePage);
        }

        private void llKimm_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.kimm.re.kr/eng");
        }

        private void llSmartDo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.smartdo.co/");
        }
    }
}

