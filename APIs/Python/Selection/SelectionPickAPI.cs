// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace APIs.Python
{
    // One pick of a selection, which is what a mouse click or a rubber band records: either a point in
    // space, or the planes of the volume the rubber band swept. It is kept as the description rather
    // than as the IDs it resolves to, so that a mesh change re-resolves it the way the application does.
    internal class SelectionPickAPI
    {
        // Variables
        private readonly double[] _point;
        private readonly double[] _direction;
        private readonly double[][] _planeParameters;
        private readonly vtkSelectBy _selectBy;
        private readonly double _angle;
        private readonly int[] _partIds;
        private readonly bool _completelyInside;
        private readonly double _precision;
        private readonly double[][] _partOffsets;


        // Properties
        public double[] Point { get { return _point; } }
        public double[] Direction { get { return _direction; } }
        public double[][] PlaneParameters { get { return _planeParameters; } }
        public vtkSelectBy SelectBy { get { return _selectBy; } }
        public double Angle { get { return _angle; } }
        public int[] PartIds { get { return _partIds; } }
        public bool CompletelyInside { get { return _completelyInside; } }
        public bool IsArea { get { return _point == null; } }


        // Constructors
        public SelectionPickAPI(double[] point, double[] direction, double[][] planeParameters,
                                vtkSelectBy selectBy, double angle, int[] partIds, bool completelyInside,
                                double precision = -1, double[][] partOffsets = null)
        {
            _point = point;
            _direction = direction;
            _planeParameters = planeParameters;
            _selectBy = selectBy;
            _angle = angle;
            _partIds = partIds;
            _completelyInside = completelyInside;
            _precision = precision;
            _partOffsets = partOffsets;
        }


        // Methods
        // The application searches only the parts the pick names, so a pick without them finds nothing;
        // the whole mesh is the meaningful default for "whatever lies under this point"
        public SelectionNodeMouse CreateNode(vtkSelectOperation selectOperation, int[] allPartIds)
        {
            int[] partIds = _partIds != null ? _partIds : allPartIds;
            SelectionNodeMouse node = new SelectionNodeMouse(_point, _direction, _planeParameters, _completelyInside,
                                                              selectOperation, partIds, _partOffsets, _selectBy, _angle);
            node.Precision = _precision;
            node.PartIdsGiven = _partIds != null;
            return node.DeepClone();
        }
        public override string ToString()
        {
            if (IsArea)
                return "Pick(planes=" + _planeParameters.Length + ", select_by=" + _selectBy + ")";
            //
            return "Pick(point=[" + string.Join(", ", _point) + "], select_by=" + _selectBy + ")";
        }
    }
}
