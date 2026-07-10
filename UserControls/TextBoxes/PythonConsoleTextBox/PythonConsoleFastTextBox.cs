// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using FastColoredTextBoxNS;
using System.Windows.Forms;

namespace UserControls
{
    internal class PythonConsoleFastTextBox : FastColoredTextBox
    {
        // Variables                                                                                                                
        private readonly PythonConsoleTextBox _owner;


        // Constructors                                                                                                             
        public PythonConsoleFastTextBox(PythonConsoleTextBox owner)
        {
            _owner = owner;
        }


        // Methods                                                                                                                  
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_owner.ProcessKeyDown(e)) return;

            base.OnKeyDown(e);
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            _owner.ProcessKeyPress(e);

            base.OnKeyPress(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            _owner.ProcessMouseDown(e);

            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _owner.ProcessMouseUp(e);
        }
        public override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            _owner.ProcessSelectionChanged();
        }
    }
}