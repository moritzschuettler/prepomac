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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// A group of connected parts that moves as one rigid body in the exploded view, together with the
    /// visualization faces of its parts. Wire parts and parts without the visualization faces contribute
    /// to the bounding box but carry no faces, so they can never form an interface.
    /// </summary>
    public class ExplodedViewCluster
    {
        // Variables                                                                                                                
        private int _id;
        private BasePart[] _parts;
        private string[] _partNames;
        private BoundingBox _boundingBox;
        private BoundingBox _toleranceBoundingBox;
        private ExplodedViewFace[] _faces;


        // Properties                                                                                                               
        public int Id { get { return _id; } }
        public string[] PartNames { get { return _partNames; } }
        public BoundingBox BoundingBox { get { return _boundingBox; } }
        /// <summary>
        /// The bounding box of the cluster, inflated by one half of the contact tolerance
        /// </summary>
        public BoundingBox ToleranceBoundingBox { get { return _toleranceBoundingBox; } }
        public ExplodedViewFace[] Faces { get { return _faces; } }
        public double Volume { get { return _boundingBox.GetVolume(); } }
        public double[] Center { get { return _boundingBox.GetCenter(); } }


        // Constructors                                                                                                             
        public ExplodedViewCluster(FeMesh mesh, List<BasePart> parts, int id, double tolerance)
        {
            _id = id;
            _parts = parts.OrderBy(part => part.Name, StringComparer.Ordinal).ToArray();
            _partNames = _parts.Select(part => part.Name).ToArray();
            //
            _boundingBox = new BoundingBox();
            List<ExplodedViewFace> faces = new List<ExplodedViewFace>();
            //
            foreach (BasePart part in _parts)
            {
                _boundingBox.IncludeBox(part.BoundingBox);
                //
                VisualizationData visualization = part.Visualization;
                if (visualization == null || visualization.CellIdsByFace == null || visualization.Cells == null) continue;
                //
                for (int faceId = 0; faceId < visualization.CellIdsByFace.Length; faceId++)
                {
                    int[] faceCellIds = visualization.CellIdsByFace[faceId];
                    if (faceCellIds == null || faceCellIds.Length == 0) continue;
                    //
                    faces.Add(new ExplodedViewFace(mesh, part, faceId, tolerance));
                }
            }
            //
            _faces = faces.ToArray();
            //
            _toleranceBoundingBox = new BoundingBox(_boundingBox);
            _toleranceBoundingBox.Inflate(0.5 * tolerance);
        }


        // Methods                                                                                                                  
        public string GetName()
        {
            if (_partNames.Length == 1) return _partNames[0];
            else return _partNames[0] + " (+" + (_partNames.Length - 1) + ")";
        }
    }
}
