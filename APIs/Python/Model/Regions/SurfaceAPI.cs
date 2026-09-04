// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class SurfaceAPI : BaseAPI
    {
        // Variables
        private readonly string _surfaceName;
        private readonly SurfaceService _service;


        // Properties
        [PythonAPI("The surface name.", Returns = "str")]
        public string name { get { return _service.ResolveSurface(_surfaceName).Name; } }
        [PythonAPI("How the surface was created: Selection, NodeSet or Faces.", Returns = "str")]
        public string created_from { get { return _service.ResolveSurface(_surfaceName).CreatedFrom.ToString(); } }
        [PythonAPI("The surface type: Element or Node.", Returns = "str")]
        public string surface_type { get { return _service.ResolveSurface(_surfaceName).Type.ToString(); } }
        [PythonAPI("The surface area.", Returns = "float")]
        public double area { get { return _service.ResolveSurface(_surfaceName).Area; } }
        [PythonAPI("The name of the node set the surface was created from, if any.", Returns = "str")]
        public string node_set_name { get { return _service.ResolveSurface(_surfaceName).CreatedFromNodeSetName; } }
        [PythonAPI("True when the surface resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return _service.ResolveSurface(_surfaceName).Valid; } }
        [PythonAPI("True when the surface was created internally for another model item.", Returns = "bool")]
        public bool is_internal { get { return _service.ResolveSurface(_surfaceName).Internal; } }


        // Constructors
        internal SurfaceAPI(string surfaceName, SurfaceService service)
        {
            _surfaceName = surfaceName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the geometry face IDs of the surface.", Returns = "IntList")]
        public IntListAPI get_face_ids()
        {
            int[] faceIds = _service.ResolveSurface(_surfaceName).FaceIds;
            return new IntListAPI(faceIds == null ? new int[0] : faceIds);
        }
        public override string ToString()
        {
            FeSurface surface = _service.ResolveSurface(_surfaceName);
            return "Surface(name=" + StringListAPI.ToDisplayString(surface.Name) +
                   ", created_from=" + surface.CreatedFrom + ", area=" + surface.Area + ")";
        }
    }
}
