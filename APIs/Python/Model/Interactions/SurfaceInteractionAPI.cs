// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    public class SurfaceInteractionAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> PropertyTypes = new Dictionary<Type, string>
        {
            { typeof(Friction), "friction" },
            { typeof(GapConductance), "gap_conductance" },
            { typeof(SurfaceBehavior), "surface_behavior" }
        };
        private readonly string _surfaceInteractionName;
        private readonly SurfaceInteractionService _service;


        // Properties
        [PythonAPI("The surface interaction name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The number of surface interaction properties.", Returns = "int")]
        public int property_count { get { return Resolve().Properties.Count; } }


        // Constructors
        internal SurfaceInteractionAPI(string surfaceInteractionName, SurfaceInteractionService service)
        {
            _surfaceInteractionName = surfaceInteractionName;
            _service = service;
        }


        // Static methods
        private static string GetPropertyType(SurfaceInteractionProperty property)
        {
            string propertyType;
            if (PropertyTypes.TryGetValue(property.GetType(), out propertyType)) return propertyType;
            //
            throw new PythonAPIException("The surface interaction property " + property.GetType().Name +
                                         " is not made by any pmx.create method.");
        }


        // Methods
        [PythonAPI("Returns the types of the surface interaction properties, named after the pmx.create method " +
                   "that makes each, for example friction.", Returns = "StringList")]
        public StringListAPI get_property_types()
        {
            List<SurfaceInteractionProperty> properties = Resolve().Properties;
            return new StringListAPI(properties.Select(property => GetPropertyType(property)).ToArray());
        }
        public override string ToString()
        {
            SurfaceInteraction surfaceInteraction = Resolve();
            return "SurfaceInteraction(name=" + StringListAPI.ToDisplayString(surfaceInteraction.Name) +
                   ", properties=" + surfaceInteraction.Properties.Count + ")";
        }
        //
        private SurfaceInteraction Resolve()
        {
            return _service.ResolveSurfaceInteraction(_surfaceInteractionName);
        }
    }
}
