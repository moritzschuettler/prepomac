using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    interface IFormItemSetDataParent
    {
        bool IsSelectionGeometryBased();
        //void GetSelectionProperties(out bool isGometryBased, out bool isIdBased);
    }
}
