// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace UserControls
{
    [Serializable]
    [PmxType(6201)]
    public class AdvisorPanelState
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _pageId;
        [PmxKey(1)] private int _verticalScrollPosition;


        // Properties                                                                                                               
        public string PageId { get { return _pageId; } set { _pageId = value; } }
        public int VerticalScrollPosition
        {
            get { return _verticalScrollPosition; }
            set { _verticalScrollPosition = value < 0 ? 0 : value; }
        }


        // Constructor                                                                                                              
        public AdvisorPanelState()
        {
            _pageId = null;
            _verticalScrollPosition = 0;
        }
    }
}
