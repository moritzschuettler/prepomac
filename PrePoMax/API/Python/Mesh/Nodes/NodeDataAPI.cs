// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;

namespace PrePoMax.API.Python
{
    public class NodeDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly object _ids;
        private readonly object _coordinates;


        // Properties
        [PythonAPI("Node IDs.", Returns = "numpy.ndarray or IntList")]
        public object ids { get { return _ids; } }
        [PythonAPI("Undeformed node coordinates with shape (node_count, 3).", Returns = "numpy.ndarray or NumericArray" )]
        public object coordinates { get { return _coordinates; } }
        [PythonAPI("The number of nodes.", Returns = "int")]
        public int count { get { return _count; } }


        // Constructors
        internal NodeDataAPI(int[] ids, double[,] coordinates)
        {
            _count = ids != null ? ids.Length : 0;
            _ids = NumPyConverter.ToPythonArray(ids);
            _coordinates = NumPyConverter.ToPythonArray(coordinates);
        }


        // Methods
        public override string ToString()
        {
            return "NodeData(count=" + _count + ")";
        }
    }
}
