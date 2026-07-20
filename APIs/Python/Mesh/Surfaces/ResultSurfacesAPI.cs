// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

namespace APIs.Python
{
    public class ResultSurfacesAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Constructors
        internal ResultSurfacesAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        public ResultSurfaceAPI __getitem__(string surfaceName)
        {
            return this[surfaceName];
        }
        public ResultSurfaceAPI this[string surfaceName]
        {
            get { return _meshService.GetSurface(_resultName, surfaceName); }
        }
        public int __len__()
        {
            return _meshService.GetSurfaceNames(_resultName).Length;
        }
        public bool __contains__(string surfaceName)
        {
            return contains(surfaceName);
        }

        [PythonAPI("Returns user-visible result surface names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_meshService.GetSurfaceNames(_resultName));
        }

        [PythonAPI("Returns the first result surface.", Returns = "Surface")]
        public ResultSurfaceAPI first()
        {
            string[] names = _meshService.GetSurfaceNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The surfaces collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last result surface.", Returns = "Surface")]
        public ResultSurfaceAPI last()
        {
            string[] names = _meshService.GetSurfaceNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The surfaces collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the result contains the specified user-visible surface.",
                   Arguments = "surface_name: str", Returns = "bool")]
        public bool contains(string surfaceName)
        {
            return _meshService.GetSurfaceNames(_resultName).Contains(surfaceName);
        }
        public override string ToString()
        {
            string[] names = _meshService.GetSurfaceNames(_resultName);
            return "Surfaces(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _meshService.GetSurfaceNames(_resultName);
            return "Surfaces(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
