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
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public class LimitElementSetDataPoint
    {
        // Variables                                                                                                                
        private string _elementSetName;
        private double _limit;


        // Properties                                                                                                               
        [DisplayName("Element Set")]
        public string ElementSetName { get { return _elementSetName; } set { _elementSetName = value; } }
        //
        [DisplayName("Limit")]
        [TypeConverter(typeof(StringDoubleConverter))]
        public double Limit { get { return _limit; } set { _limit = value; } }


        // Constructors                                                                                                             
        public LimitElementSetDataPoint()
            : this(null, 0)
        {
        }
        public LimitElementSetDataPoint(string elementSetName, double limit)
        {
            _elementSetName = elementSetName;
            _limit = limit;
        }
    }
}
