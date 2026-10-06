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
        private readonly IntListAPI _ids;
        private readonly StringListAPI _partNames;
        private readonly double[] _boundingBox;
        private readonly double[] _centroid;


        // Properties
        [PythonAPI("The resolved item type of the selection.", Returns = "str")]
        public string target { get { return _target; } }
        [PythonAPI("The number of resolved items. Zero means the selection finds nothing in the current view.",
                   Returns = "int")]
        public int count { get { return _count; } }
        [PythonAPI("The resolved IDs: node IDs, element IDs, part IDs or geometry IDs, following the target.",
                   Returns = "IntList")]
        public IntListAPI ids { get { return _ids; } }
        [PythonAPI("The names of the parts the resolved items belong to.", Returns = "StringList")]
        public StringListAPI part_names { get { return _partNames; } }
        [PythonAPI("The bounding box of the resolved items as [xMin, xMax, yMin, yMax, zMin, zMax], or an " +
                   "empty list when nothing resolved.", Returns = "list[float]")]
        public object bounding_box { get { return PythonListConverter.ToPythonList(_boundingBox); } }
        [PythonAPI("The mean of the node coordinates of the resolved items as [x, y, z], or an empty list when " +
                   "nothing resolved.", Returns = "list[float]")]
        public object centroid { get { return PythonListConverter.ToPythonList(_centroid); } }


        // Constructors
        internal SelectionPreviewAPI(string target, int[] ids, string[] partNames, double[] boundingBox,
                                     double[] centroid)
        {
            _target = target;
            _count = ids == null ? 0 : ids.Length;
            _ids = new IntListAPI(ids);
            _partNames = new StringListAPI(partNames);
            _boundingBox = boundingBox == null ? new double[0] : (double[])boundingBox.Clone();
            _centroid = centroid == null ? new double[0] : (double[])centroid.Clone();
        }


        // Methods
        public override string ToString()
        {
            return "SelectionPreview(target=" + _target + ", count=" + _count + ", parts=" +
                   _partNames.Count + ")";
        }
    }
}
