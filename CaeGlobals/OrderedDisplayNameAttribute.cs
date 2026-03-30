// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.ComponentModel;
using System.Text;

namespace CaeGlobals
{
    public class OrderedDisplayNameAttribute : DisplayNameAttribute
    {
        public OrderedDisplayNameAttribute(int position, int total, string displayName)
        {
            StringBuilder sb = new StringBuilder(displayName);

            for (int index = position; index < total; index++)
            {
                sb.Insert(0, '\u200B');
            }

            base.DisplayNameValue = sb.ToString();
        }
    }
}