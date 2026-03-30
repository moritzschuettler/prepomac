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
using System.Windows.Forms;

namespace PrePoMax.Forms
{ 
    class ListViewItemComparer : System.Collections.IComparer
    {
        //  Variables                                                                                                               
        private Dictionary<string, int> _namePosition = new Dictionary<string, int>();        
        private int _col;


        //  Constructors                                                                                                            
        public ListViewItemComparer()
            :this(0, null)
        {
        }
        public ListViewItemComparer(int column, Dictionary<string, int> namePosition)            
        {
            _col = column;
            _namePosition = namePosition;
        }
        public int Compare(object x, object y)
        {
            if (_namePosition == null) return 0;
            //
            int xPos = 0;
            int yPos = 0;
            if (_namePosition.TryGetValue(((ListViewItem)x).Text, out xPos) &&
                _namePosition.TryGetValue(((ListViewItem)y).Text, out yPos))
            {
                xPos = _namePosition[((ListViewItem)x).Text];
                yPos = _namePosition[((ListViewItem)y).Text];
            }
            //
            if (xPos > yPos) return 1;
            else if (xPos < yPos) return -1;
            return 0;
        }
    }
}
