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

namespace CaeMesh
{
    /// <summary>
    /// The maps of a part's visualization that the sweep detection asks for again on every source it tries.
    /// They depend on the part alone, so one instance of them serves every trial of one detection. They are
    /// read and never written, and one instance belongs to one detection, so nothing is shared between the
    /// detections of two groups running at the same time.
    /// </summary>
    public class SweepPartMaps
    {
        // Variables
        private Dictionary<int, Dictionary<int, HashSet<int>>> _partIdSurfaceIdSurfaceNeighbourIds;
        private Dictionary<int, Dictionary<int, HashSet<int>>> _partIdVertexIdEdgeIds;
        private Dictionary<int, List<HashSet<int>>> _partIdSmoothSurfaceIdGroups;
        private Dictionary<int, bool> _partIdIsTransfiniteBlock;
        private Dictionary<int, Dictionary<int, GeomFaceData>> _partIdSurfaceIdFaceData;
        private Dictionary<int, Dictionary<int[], double[]>> _partIdSurfaceIdPairAngles;
        private Dictionary<int, Dictionary<string, SweepBlockValidation>> _partIdSourceValidations;
        private Dictionary<int, Dictionary<string, HashSet<int>>> _partIdSourceExtensions;


        // Constructors
        public SweepPartMaps()
        {
            _partIdSurfaceIdSurfaceNeighbourIds = new Dictionary<int, Dictionary<int, HashSet<int>>>();
            _partIdVertexIdEdgeIds = new Dictionary<int, Dictionary<int, HashSet<int>>>();
            _partIdSmoothSurfaceIdGroups = new Dictionary<int, List<HashSet<int>>>();
            _partIdIsTransfiniteBlock = new Dictionary<int, bool>();
            _partIdSurfaceIdFaceData = new Dictionary<int, Dictionary<int, GeomFaceData>>();
            _partIdSurfaceIdPairAngles = new Dictionary<int, Dictionary<int[], double[]>>();
            _partIdSourceValidations = new Dictionary<int, Dictionary<string, SweepBlockValidation>>();
            _partIdSourceExtensions = new Dictionary<int, Dictionary<string, HashSet<int>>>();
        }


        // Methods
        public Dictionary<int, HashSet<int>> GetSurfaceIdSurfaceNeighbourIds(BasePart part)
        {
            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds;
            if (_partIdSurfaceIdSurfaceNeighbourIds.TryGetValue(part.PartId, out surfaceIdSurfaceNeighbourIds))
                return surfaceIdSurfaceNeighbourIds;
            //
            surfaceIdSurfaceNeighbourIds = part.Visualization.GetSurfaceIdSurfaceNeighbourIds();
            _partIdSurfaceIdSurfaceNeighbourIds.Add(part.PartId, surfaceIdSurfaceNeighbourIds);
            //
            return surfaceIdSurfaceNeighbourIds;
        }
        public Dictionary<int, HashSet<int>> GetVertexIdEdgeIds(BasePart part)
        {
            Dictionary<int, HashSet<int>> vertexIdEdgeIds;
            if (_partIdVertexIdEdgeIds.TryGetValue(part.PartId, out vertexIdEdgeIds)) return vertexIdEdgeIds;
            //
            vertexIdEdgeIds = part.Visualization.GetVertexIdEdgeIds();
            _partIdVertexIdEdgeIds.Add(part.PartId, vertexIdEdgeIds);
            //
            return vertexIdEdgeIds;
        }
        /// <summary>
        /// Whether the part is a block the transfinite mesher accepts, asked of the caller once per part.
        /// </summary>
        public bool IsTransfiniteBlock(BasePart part, Func<BasePart, bool> isTransfiniteBlock)
        {
            bool isBlock;
            if (_partIdIsTransfiniteBlock.TryGetValue(part.PartId, out isBlock)) return isBlock;
            //
            isBlock = isTransfiniteBlock(part);
            _partIdIsTransfiniteBlock.Add(part.PartId, isBlock);
            //
            return isBlock;
        }
        public List<HashSet<int>> GetSmoothSurfaceIdGroups(FeMesh geometry, BasePart part, double angleDeg)
        {
            List<HashSet<int>> smoothSurfaceIdGroups;
            if (_partIdSmoothSurfaceIdGroups.TryGetValue(part.PartId, out smoothSurfaceIdGroups))
                return smoothSurfaceIdGroups;
            //
            smoothSurfaceIdGroups = geometry.GetSmoothSurfaceIdGroups(part, angleDeg);
            _partIdSmoothSurfaceIdGroups.Add(part.PartId, smoothSurfaceIdGroups);
            //
            return smoothSurfaceIdGroups;
        }
        /// <summary>
        /// The recognized face data of the part's surfaces, filled by the caller as it asks for a surface and
        /// kept for every later trial.
        /// </summary>
        public Dictionary<int, GeomFaceData> GetSurfaceIdFaceData(BasePart part)
        {
            Dictionary<int, GeomFaceData> surfaceIdFaceData;
            if (_partIdSurfaceIdFaceData.TryGetValue(part.PartId, out surfaceIdFaceData)) return surfaceIdFaceData;
            //
            surfaceIdFaceData = new Dictionary<int, GeomFaceData>();
            _partIdSurfaceIdFaceData.Add(part.PartId, surfaceIdFaceData);
            //
            return surfaceIdFaceData;
        }
        public Dictionary<int[], double[]> GetSurfaceIdPairAngles(FeMesh geometry, BasePart part)
        {
            Dictionary<int[], double[]> surfaceIdPairAngles;
            if (_partIdSurfaceIdPairAngles.TryGetValue(part.PartId, out surfaceIdPairAngles)) return surfaceIdPairAngles;
            //
            surfaceIdPairAngles = geometry.GetSurfaceIdPairAngles(part);
            _partIdSurfaceIdPairAngles.Add(part.PartId, surfaceIdPairAngles);
            //
            return surfaceIdPairAngles;
        }
        /// <summary>
        /// The validation of a block of the part swept from the given source, made once and read again by every
        /// trial that reaches the same source. Whether the surfaces the parts share were known to the validation
        /// is part of the key, since a validation made without them reads a target differently.
        /// </summary>
        public SweepBlockValidation GetSweepBlockValidation(BasePart part, HashSet<int> sourceSurfaceIds,
                                                            bool sharedSurfacesKnown, Func<SweepBlockValidation> validate)
        {
            Dictionary<string, SweepBlockValidation> validations;
            if (!_partIdSourceValidations.TryGetValue(part.PartId, out validations))
            {
                validations = new Dictionary<string, SweepBlockValidation>();
                _partIdSourceValidations.Add(part.PartId, validations);
            }
            string key = (sharedSurfacesKnown ? "shared:" : "alone:") + string.Join(",", sourceSurfaceIds.OrderBy(id => id));
            SweepBlockValidation validation;
            if (validations.TryGetValue(key, out validation)) return validation;
            //
            validation = validate();
            validations.Add(key, validation);
            //
            return validation;
        }
        /// <summary>
        /// The source surfaces a source of the part grows to before its block is validated - over the surfaces in
        /// its plane, and for a source the sweep derived over the surfaces leaning on it - grown once per source
        /// and read again by every trial that reaches the same source. The set is kept, so a caller only reads it.
        /// </summary>
        public HashSet<int> GetExtendedSourceSurfaceIds(BasePart part, HashSet<int> sourceSurfaceIds, bool derived,
                                                        Func<HashSet<int>> extend)
        {
            Dictionary<string, HashSet<int>> extensions;
            if (!_partIdSourceExtensions.TryGetValue(part.PartId, out extensions))
            {
                extensions = new Dictionary<string, HashSet<int>>();
                _partIdSourceExtensions.Add(part.PartId, extensions);
            }
            string key = (derived ? "derived:" : "selected:") + string.Join(",", sourceSurfaceIds.OrderBy(id => id));
            HashSet<int> extendedSourceSurfaceIds;
            if (extensions.TryGetValue(key, out extendedSourceSurfaceIds)) return extendedSourceSurfaceIds;
            //
            extendedSourceSurfaceIds = extend();
            extensions.Add(key, extendedSourceSurfaceIds);
            //
            return extendedSourceSurfaceIds;
        }
    }
}
