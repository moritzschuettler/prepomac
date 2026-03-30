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
using System.Runtime.Serialization;

namespace CaeGlobals
{
    [Serializable]
    public class SelectionNodeInvert : SelectionNode, ISerializable
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public SelectionNodeInvert()
            : base(vtkSelectOperation.Invert)
        {
        }
        public SelectionNodeInvert(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        // Methods                                                                                                                  

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            //info.AddValue("_pickedPoint", _pickedPoint, typeof(double[]));
        }
    }
}
