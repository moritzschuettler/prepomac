// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace vtkControl
{
    // The anchor of an annotation that follows an animation: the points of the cell it lies in, their weights and its
    // offset from the point they interpolate. The points are the surface points of the actor, or the nodes of its
    // volume grid read at the frame shown
    class vtkMaxAnimationArrowAnchor
    {
        // Variables
        private vtkMaxActor _actor;
        private int[] _pointIds;
        private int[] _nodeIds;
        private double[] _weights;
        private double[] _offset;


        // Constructors
        public vtkMaxAnimationArrowAnchor(vtkMaxActor actor, int[] pointIds, int[] nodeIds, double[] weights,
                                          double[] offset)
        {
            _actor = actor;
            _pointIds = pointIds;
            _nodeIds = nodeIds;
            _weights = weights;
            _offset = offset;
        }


        // Methods
        public double[] GetAnchorPoint()
        {
            double[] anchorPoint = new double[] { _offset[0], _offset[1], _offset[2] };
            NodesExchangeData nodes = null;
            if (_nodeIds != null)
            {
                // An anchor inside a part only needs its own points at the displayed frame
                nodes = _actor.GetAnimationNodes(GetFrameNumber(), _nodeIds);
                if (nodes == null) return anchorPoint;
            }
            for (int i = 0; i < _pointIds.Length; i++)
            {
                double[] point;
                if (nodes != null)
                    point = new double[] { nodes.FlatCoor[3 * i], nodes.FlatCoor[3 * i + 1], nodes.FlatCoor[3 * i + 2] };
                else point = _actor.GetPoint(_pointIds[i]);
                if (point == null) continue;
                anchorPoint[0] += _weights[i] * point[0];
                anchorPoint[1] += _weights[i] * point[1];
                anchorPoint[2] += _weights[i] * point[2];
            }
            return anchorPoint;
        }
        private int GetFrameNumber()
        {
            return _actor.AnimationFrameNumber;
        }
    }
}
