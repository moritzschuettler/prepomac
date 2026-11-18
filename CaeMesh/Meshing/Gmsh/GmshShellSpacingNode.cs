// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh
{
    /// <summary>
    /// A boundary node with an affine coordinate along one direction of a structured face's ideal outline.
    /// </summary>
    internal struct GmshShellSpacingNode
    {
        // Variables                                                                                                    
        private readonly GmshShellSpacingEdge _edge;
        private readonly int _index;
        private readonly double _offset;
        private readonly double _scale;


        // Properties                                                                                                   
        public double Coordinate { get { return _offset + _scale * _edge.GetFraction(_index); } }
        public double[] Point { get { return _edge.GetPoint(_index); } }


        // Constructor                                                                                                  
        public GmshShellSpacingNode(GmshShellSpacingEdge edge, int index, double offset, double scale)
        {
            _edge = edge;
            _index = index;
            _offset = offset;
            _scale = scale;
        }


        // Methods                                                                                                      
        public void AddError(double error)
        {
            _edge.AddError(_index, _scale, error);
        }
    }
}
