// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace PrePoMax.Forms
{
    public class FrmAboutComposites : Form
    {
        private const string _version = "v0.3.0";
        private const string _url = "https://gitlab.com/yvanblanchard/pre-po-max-composites";

        public FrmAboutComposites()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "About Composites Modeler";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(420, 160);
            //
            Label lblTitle = new Label
            {
                Text = "Composites Modeler " + _version,
                Font = new Font(this.Font.FontFamily, 13f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            //
            string buildDate = GetBuildDate();
            Label lblBuild = new Label
            {
                Text = "Build date: " + buildDate,
                AutoSize = true,
                Location = new Point(20, 60)
            };
            //
            LinkLabel llUrl = new LinkLabel
            {
                Text = _url,
                AutoSize = true,
                Location = new Point(20, 85)
            };
            llUrl.LinkClicked += (s, e) => Process.Start(_url);
            //
            Button btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 28),
                Location = new Point(ClientSize.Width - 100, ClientSize.Height - 44)
            };
            this.AcceptButton = btnClose;
            //
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblBuild);
            this.Controls.Add(llUrl);
            this.Controls.Add(btnClose);
        }

        private static string GetBuildDate()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            DateTime dt = System.IO.File.GetLastWriteTime(path);
            return dt.ToString("yyyy-MM-dd");
        }
    }
}
