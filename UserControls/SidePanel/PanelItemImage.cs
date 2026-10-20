// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard

using System;
using System.Drawing;

namespace UserControls
{
    // An image of a panel page, shown in the width of the panel and never enlarged
    [Serializable]
    public class PanelItemImage : PanelItem
    {
        // Variables
        private Image _image;
        private int _maxWidth;


        // Properties
        public Image Image { get { return _image; } set { _image = value; } }
        public int MaxWidth
        {
            get { return _maxWidth; }
            set
            {
                _maxWidth = value;
                if (_maxWidth < 0) _maxWidth = 0;
            }
        }


        // Constructors
        public PanelItemImage()
        {
            _image = null;
            _maxWidth = 0;
        }
        public PanelItemImage(Image image)
        {
            _image = image;
            _maxWidth = 0;
        }
    }
}
