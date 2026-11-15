// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(10497)]
    public class LimitPartDataPoint
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _partName;
        [PmxKey(1)] private double _limit;


        // Properties                                                                                                               
        [DisplayName("Part")]
        public string PartName { get { return _partName; } set { _partName = value; } }
        //
        [DisplayName("Limit")]
        [TypeConverter(typeof(StringDoubleConverter))]
        public double Limit { get { return _limit; } set { _limit = value; } }


        // Constructors                                                                                                             
        public LimitPartDataPoint()
            : this(null, 0)
        {
        }
        public LimitPartDataPoint(string partName, double limit)
        {
            _partName = partName;
            _limit = limit;
        }
    }
}
