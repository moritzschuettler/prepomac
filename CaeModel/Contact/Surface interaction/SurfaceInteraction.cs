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
using System.ComponentModel;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class SurfaceInteraction : NamedClass
    {
        // Variables                                                                                                                
        private List<SurfaceInteractionProperty> _properties;


        // Properties                                                                                                               
        public List<SurfaceInteractionProperty> Properties { get { return _properties; } }


        // Constructors                                                                                                             
        public SurfaceInteraction(string name)
            : base(name)
        {
            _properties = new List<SurfaceInteractionProperty>();
        }


        // Methods                                                                                                                  
        public void AddProperty(SurfaceInteractionProperty property)
        {
            _properties.Add(property);
        }
    }
}
