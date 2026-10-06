// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    public class SurfaceInteractionAPI : BaseAPI
    {
        // Variables
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


        // Methods
        [PythonAPI("Returns the types of the surface interaction properties, named after the pmx.create method " +
                   "that makes each, for example friction.", Returns = "StringList")]
        public StringListAPI get_property_types()
        {
            List<SurfaceInteractionProperty> properties = Resolve().Properties;
            return new StringListAPI(properties.Select(property => SurfaceInteractionPropertyAPI.GetPropertyType(property))
                                               .ToArray());
        }
        [PythonAPI("Returns detached copies of the surface interaction properties. Their values are read with " +
                   "get_values, and the list can be passed straight back to surface_interactions.replace.",
                   Returns = "list[SurfaceInteractionProperty]",
                   Example = "pmx.model.surface_interactions.replace('SI-1', " +
                             "properties=pmx.model.surface_interactions['SI-1'].get_properties())")]
        public SurfaceInteractionPropertyAPI[] get_properties()
        {
            SurfaceInteraction surfaceInteraction = Resolve().DeepClone();
            return surfaceInteraction.Properties.Select(property => new SurfaceInteractionPropertyAPI(property))
                                                .ToArray();
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
