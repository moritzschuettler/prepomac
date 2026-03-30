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
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using CaeResults;

namespace PrePoMax.Forms
{
    [Serializable]
    public abstract class ViewTransformation
    {
        // Variables                                                                                                                
        protected DynamicCustomTypeDescriptor _dctd = null;
        protected ItemSetData _startPointItemSetData;
        protected ItemSetData _endPointItemSetData;


        // Properties                                                                                                               
        //[Browsable(false)]
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Enter the transformation name.")]
        [Id(1, 1)]
        public virtual string Name { get; set; }


        // Constructors                                                                                                             


        // Methods                                                                                                                  
        [Browsable(false)]
        public abstract Transformation Base { get; }
    }
}
