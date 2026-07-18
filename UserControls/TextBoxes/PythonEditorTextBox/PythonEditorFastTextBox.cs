// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using FastColoredTextBoxNS;
using System.Windows.Forms;

namespace UserControls
{
    internal class PythonEditorFastTextBox : FastColoredTextBox
    {
        // Variables                                                                                                                
        private readonly PythonEditorTextBox _owner;


        // Constructors                                                                                                             
        public PythonEditorFastTextBox(PythonEditorTextBox owner)
        {
            _owner = owner;
        }


        // Methods                                                                                                                  
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_owner.ProcessKeyDown(e)) return;

            base.OnKeyDown(e);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_owner.ProcessCommandKey(keyData)) return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            _owner.ProcessKeyPress(e);

            base.OnKeyPress(e);
        }
        public override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            _owner.ProcessSelectionChanged();
        }
    }
}
