// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    // What a selection resolves to right now, without creating anything. It exists so that a script
    // can check a pick before it builds a model item out of it: an empty count and a centroid in the
    // wrong place both say that the point or the ID list is not the entity that was meant.
    public class SelectionPreviewAPI : BaseAPI
    {
        // Variables
        private readonly string _target;
        private readonly int _count;
        private readonly object _ids;
        private readonly StringListAPI _partNames;
        private readonly NumericArrayAPI _boundingBox;
        private readonly NumericArrayAPI _centroid;


        // Properties
        [PythonAPI("The resolved item type of the selection.", Returns = "str")]
        public string target { get { return _target; } }
        [PythonAPI("The number of resolved items. Zero means the selection finds nothing in the current view.",
                   Returns = "int")]
        public int count { get { return _count; } }
        [PythonAPI("The resolved IDs: node IDs, element IDs, part IDs or geometry IDs, following the target.",
                   Returns = "numpy.ndarray or IntList")]
        public object ids { get { return _ids; } }
        [PythonAPI("The names of the parts the resolved items belong to.", Returns = "StringList")]
        public StringListAPI part_names { get { return _partNames; } }
        [PythonAPI("The bounding box of the resolved items as [xMin, xMax, yMin, yMax, zMin, zMax], or an " +
                   "empty array when nothing resolved.", Returns = "NumericArray")]
        public NumericArrayAPI bounding_box { get { return _boundingBox; } }
        [PythonAPI("The mean of the node coordinates of the resolved items, or an empty array when nothing " +
                   "resolved.", Returns = "NumericArray")]
        public NumericArrayAPI centroid { get { return _centroid; } }


        // Constructors
        internal SelectionPreviewAPI(string target, int[] ids, string[] partNames, double[] boundingBox,
                                     double[] centroid)
        {
            _target = target;
            _count = ids == null ? 0 : ids.Length;
            _ids = NumPyConverter.ToPythonArray(ids);
            _partNames = new StringListAPI(partNames);
            _boundingBox = new NumericArrayAPI(boundingBox);
            _centroid = new NumericArrayAPI(centroid);
        }


        // Methods
        public override string ToString()
        {
            return "SelectionPreview(target=" + _target + ", count=" + _count + ", parts=" +
                   _partNames.Count + ")";
        }
    }
}
