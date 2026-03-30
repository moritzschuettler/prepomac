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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    public class FilteredFileNameEditor : System.Windows.Forms.Design.FileNameEditor
    {
        public static string Filter = "";


        public FilteredFileNameEditor()
        {
        }


        protected override void InitializeDialog(System.Windows.Forms.OpenFileDialog openFileDialog)
        {
            base.InitializeDialog(openFileDialog);
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = Filter;
        }
    }
}
