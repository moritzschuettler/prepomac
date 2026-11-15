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
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeMesh
{
    [PmxType(2089)]
    public class GeometrySelection : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int[] _geometryIds;
        [PmxKey(1)] private Selection _creationData;
        

        // Properties                                                                                                               
        public int[] GeometryIds { get { return _geometryIds; } set { _geometryIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }


        // Constructors                                                                                                             
        public GeometrySelection(string name)
            :base(name)
        {
            Clear();
        }
        public GeometrySelection(string name, int[] geometryIds, Selection creationDataClone)
           : this(name)
        {
            _geometryIds = geometryIds;
            _creationData = creationDataClone;
        }
        public GeometrySelection(GeometrySelection geometrySelection)
           : this(geometrySelection.Name)
        {
            _geometryIds = geometrySelection.GeometryIds?.ToArray();
            _creationData = geometrySelection.CreationData?.DeepCopy(true, false);
            _creationData = geometrySelection.CreationData?.DeepCopy(true, false);
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _geometryIds = null;
            _creationData = null;
        }
    }
}
