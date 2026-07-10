// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public partial class PythonPanel : SidePanel
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        public override bool Focused => pythonConsole.Focused;


        // Events                                                                                                                   
        public event Action<object, EventArgs> CloseButtonPressedEvent;
        public event EventHandler<PythonConsoleCommandEventArgs> ExecuteCommand;
        public event EventHandler<PythonConsoleAutocompleteEventArgs> AutocompleteRequested;


        // Constructor                                                                                                              
        public PythonPanel()
        {
            DoubleBuffered = true;
            //
            InitializeComponent();
            //
            pythonConsole.ExecuteCommand += PythonConsole_ExecuteCommand;
            pythonConsole.AutocompleteRequested += PythonConsole_AutocompleteRequested;
            //
            pythonConsole.InitializeConsole();
        }


        // Event handlers                                                                                                           
       
        private void PythonPanel_Resize(object sender, EventArgs e)
        {
        }
        //
        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = global::UserControls.Properties.Resources.Close_White;
            }
            catch
            { }
        }
        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = global::UserControls.Properties.Resources.Close;
            }
            catch
            { }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                CloseButtonPressedEvent?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        //
        private void PythonConsole_AutocompleteRequested(object sender, PythonConsoleAutocompleteEventArgs e)
        {
            AutocompleteRequested(sender, e);
        }
        private void PythonConsole_ExecuteCommand(object sender, PythonConsoleCommandEventArgs e)
        {
            ExecuteCommand(sender, e);
        }


        // Methods                                                                                                                  

    }
}
