// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    /// <summary>
    /// What a repair would take away from one part: every edge as the points sampled on it and every vertex as its
    /// point. A repair the modelling kernel refuses would leave the part as it was, and takes nothing away.
    /// </summary>
    public class GeometryRepairPreview
    {
        // Variables                                                                                                                
        private bool _refused;
        private double[][][] _edges;
        private double[][] _points;


        // Properties                                                                                                               
        public bool Refused { get { return _refused; } }
        public double[][][] Edges { get { return _edges; } }
        public double[][] Points { get { return _points; } }


        // Constructors                                                                                                             
        public GeometryRepairPreview(double[][][] edges, double[][] points)
        {
            _refused = edges == null || points == null;
            _edges = edges ?? new double[0][][];
            _points = points ?? new double[0][];
        }
    }
}
