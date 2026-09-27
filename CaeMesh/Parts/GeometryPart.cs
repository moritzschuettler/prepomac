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
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public class GeometryPart : BasePart
    {
        // Variables                                                                                                                
        private MeshingParameters _meshingParameters;
        private List<Enum> _elementTypeEnums;
        private int[] _freeEdgeCellIds;
        private int[] _errorEdgeCellIds;
        private int[] _freeNodeIds;
        private int[] _errorNodeIds;
        private string _cadFileData;
        //
        // Derived from the geometry and recomputed from it, so they are not saved
        [NonSerialized]
        private MeshTypeEnum _meshType;
        [NonSerialized]
        private SweepDetectionData _sweepDetection;
        [NonSerialized]
        private SweepDetectionData _symmetricSweepDetection;
        [NonSerialized]
        private SweepDetectionData _sweepableCellsSweepDetection;


        // Properties
        public MeshingParameters MeshingParameters
        {
            get { return _meshingParameters; }
            set
            {
                _meshingParameters = value;
                if (_meshingParameters != null && (PartType == PartType.Solid || PartType == PartType.SolidAsShell))
                    _meshingParameters.QuadDominated = false;
            }
        }
        
        public int[] FreeEdgeCellIds { get { return _freeEdgeCellIds; } set { _freeEdgeCellIds = value; } }
        public int[] ErrorEdgeCellIds { get { return _errorEdgeCellIds; } set { _errorEdgeCellIds = value; } }
        public int[] FreeNodeIds { get { return _freeNodeIds; } set { _freeNodeIds = value; } }
        public int[] ErrorNodeIds { get { return _errorNodeIds; } set { _errorNodeIds = value; } }
        public bool HasErrors
        {
            get
            {
                if (_errorEdgeCellIds != null) return true;
                else if (_errorNodeIds != null) return true;
                else return false;
            }
        }
        public bool HasFreeEdges
        {
            get
            {
                if (_freeEdgeCellIds != null) return true;
                else if (_freeNodeIds != null) return true;
                else return false;
            }
        }
        public string CADFileData
        {
            get
            {
                if (_cadFileData != null) return StringCompressor.DecompressString(_cadFileData);
                else return null;
            }
            set
            {
                if (value == null) _cadFileData = null;
                else _cadFileData = StringCompressor.CompressString(value);
            }
        }
        public bool IsCADPart { get { return _cadFileData != null; } }
        /// <summary>
        /// The most structured mesh the part supports, recognized from its geometry.
        /// </summary>
        public MeshTypeEnum MeshType { get { return _meshType; } set { _meshType = value; } }
        /// <summary>
        /// What the automatic sweep detection found for this part: for a single solid part or a cell of a compound,
        /// the part taken alone; for a compound part, all its cells swept together.
        /// </summary>
        public SweepDetectionData SweepDetection { get { return _sweepDetection; } set { _sweepDetection = value; } }
        /// <summary>
        /// What the automatic sweep detection found for the symmetric piece of the compound part this part stands
        /// for - the cells the sweep meshes before the mesh is mirrored onto the others - or null.
        /// </summary>
        public SweepDetectionData SymmetricSweepDetection
        {
            get { return _symmetricSweepDetection; }
            set { _symmetricSweepDetection = value; }
        }
        /// <summary>
        /// What the automatic sweep detection found for the cells of the compound part this part stands for that
        /// can be swept on their own, swept together without the others, which are left to tetrahedra - or null.
        /// </summary>
        public SweepDetectionData SweepableCellsSweepDetection
        {
            get { return _sweepableCellsSweepDetection; }
            set { _sweepableCellsSweepDetection = value; }
        }


        // Constructors
        public GeometryPart(string name, int partId,  int[] nodeLabels, int[] elementLabels, Type[] elementTypes)
            : base(name, partId, nodeLabels, elementLabels, elementTypes)
        {
            _meshingParameters = null;
            _elementTypeEnums = null;
            _freeEdgeCellIds = null;
            _errorEdgeCellIds = null;
            _freeNodeIds = null;
            _errorNodeIds = null;
            _cadFileData = null;
            _meshType = MeshTypeEnum.Unknown;
            _sweepDetection = null;
            _symmetricSweepDetection = null;
            _sweepableCellsSweepDetection = null;
        }
        public GeometryPart(BasePart part)
            : base(part)
        {
            _meshingParameters = null;
            _elementTypeEnums = null;
            _freeEdgeCellIds = null;
            _errorEdgeCellIds = null;
            _freeNodeIds = null;
            _errorNodeIds = null;
            _cadFileData = null;
            _meshType = MeshTypeEnum.Unknown;
            _sweepDetection = null;
            _symmetricSweepDetection = null;
            _sweepableCellsSweepDetection = null;
        }
        public GeometryPart(GeometryPart part)
            : base((BasePart)part)
        {
            _meshingParameters = part._meshingParameters != null ? part._meshingParameters.DeepClone() : null;
            _elementTypeEnums = part._elementTypeEnums != null ? part._elementTypeEnums.ToList() : null;
            _freeEdgeCellIds = part.FreeEdgeCellIds != null ? part.FreeEdgeCellIds.ToArray() : null;
            _errorEdgeCellIds = part.ErrorEdgeCellIds != null ? part.ErrorEdgeCellIds.ToArray() : null;
            _freeNodeIds = part.FreeNodeIds != null ? part.FreeNodeIds.ToArray() : null;
            _errorNodeIds = part.ErrorNodeIds != null ? part.ErrorNodeIds.ToArray() : null;
            _cadFileData = part._cadFileData != null ? part._cadFileData : null;
            _meshType = part._meshType;
            _sweepDetection = part._sweepDetection;
            _symmetricSweepDetection = part._symmetricSweepDetection;
            _sweepableCellsSweepDetection = part._sweepableCellsSweepDetection;
        }


        // Methods                                                                                                                  
        public override BasePart DeepCopy()
        {
            return new GeometryPart(this);
        }
        //
        public void CADFileDataFromFile(string brepFileName)
        {
            if (System.IO.Path.GetExtension(brepFileName) == ".brep") CADFileData = System.IO.File.ReadAllText(brepFileName);
            else throw new NotSupportedException();
        }
        //
        public override PartProperties GetProperties()
        {
            PartProperties properties = base.GetProperties();
            properties.IsCADPart = IsCADPart;
            return properties;
        }
        public override void SetProperties(PartProperties properties)
        {
            base.SetProperties(properties);
        }
        public List<Enum> GetElementTypeEnums()
        {
            return _elementTypeEnums;
        }
        public void AddElementTypeEnums(List<Enum> elementTypeEnums)
        {     
            if (elementTypeEnums != null)
            {
                Dictionary<Type, Enum> oldEnums = new Dictionary<Type, Enum>();
                if (_elementTypeEnums != null)
                {
                    foreach (var oldElementTypeEnum in _elementTypeEnums)
                        oldEnums.Add(oldElementTypeEnum.GetType(), oldElementTypeEnum);
                }
                //
                foreach (var newEnum in elementTypeEnums)
                {
                    if (oldEnums.ContainsKey(newEnum.GetType())) oldEnums[newEnum.GetType()] = newEnum;
                    else oldEnums.Add(newEnum.GetType(), newEnum);
                }
                //
                _elementTypeEnums = oldEnums.Values.ToList();
            }
        }
        public override void RenumberVisualizationElements(Dictionary<int, int> newIds)
        {
            base.RenumberVisualizationElements(newIds);
        }
        public override void RenumberVisualizationNodes(Dictionary<int, int> newIds)
        {
            base.RenumberVisualizationNodes(newIds);
            //
            if (_errorNodeIds != null)
            {
                for (int i = 0; i < _errorNodeIds.Length; i++) _errorNodeIds[i] = newIds[_errorNodeIds[i]];
            }
            if (_freeNodeIds != null)
            {
                for (int i = 0; i < _freeNodeIds.Length; i++) _freeNodeIds[i] = newIds[_freeNodeIds[i]];
            }
        }
    }
}
