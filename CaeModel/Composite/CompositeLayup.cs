// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public enum CompositeElementType
    {
        ConventionalShell,
        ContinuumShell,
        Solid
    }

    [Serializable]
    public enum OrientationDefinitionType
    {
        PartGlobal,
        CoordinateSystem,
        DiscreteOrientations,
        Rosette                 // must stay last: the value is serialized as an integer
    }

    [Serializable]
    public enum NormalDirectionType
    {
        Axis1,
        Axis2,
        Axis3
    }

    [Serializable]
    public enum AdditionalRotationType
    {
        None,
        Angle
    }

    [Serializable]
    public enum ProcessType
    {
        HandLayup,
        TapePlacement,
        Winding,
        Braiding,
        // Appended last on purpose: the enum is serialized as an int, so inserting None first would
        // turn every hand layup of an existing model into a None. The dialog lists it first.
        None
    }

    [Serializable]
    public enum ShellOffsetType
    {
        MiddleSurface,
        TopSurface,
        BottomSurface,
        SpecifyRatio
    }

    [Serializable]
    public class CompositeLayup : NamedClass
    {
        // Variables
        private int _initialPlyCount;
        private CompositeElementType _elementType;
        private ProcessType _processType;
        private OrientationDefinitionType _orientationDefinition;
        private NormalDirectionType _normalDirection;
        private AdditionalRotationType _additionalRotationType;
        private double _additionalRotationAngle;
        private List<Ply> _plies;
        private bool _symmetric;
        private ShellOffsetType _shellOffset;
        private double _offsetRatio;
        private DiscreteOrientationsData _discreteOrientationsData;
        private bool _useDrapingForFiberDirection;
        private double _drapingCellSize;
        private int _drapingPlyIndex;
        private string _solidPartName;
        private string _sourceShellPartName;
        private string _orientationRosette;


        // Properties
        public int InitialPlyCount
        {
            get { return _initialPlyCount; }
            set
            {
                if (value < 1) throw new CaeException("Initial ply count must be at least 1.");
                _initialPlyCount = value;
            }
        }

        public CompositeElementType ElementType
        {
            get { return _elementType; }
            set { _elementType = value; }
        }

        public ProcessType ProcessType
        {
            get { return _processType; }
            set { _processType = value; }
        }

        public OrientationDefinitionType OrientationDefinition
        {
            get { return _orientationDefinition; }
            set { _orientationDefinition = value; }
        }

        public NormalDirectionType NormalDirection
        {
            get { return _normalDirection; }
            set { _normalDirection = value; }
        }

        public AdditionalRotationType AdditionalRotationType
        {
            get { return _additionalRotationType; }
            set { _additionalRotationType = value; }
        }

        public double AdditionalRotationAngle
        {
            get { return _additionalRotationAngle; }
            set { _additionalRotationAngle = value; }
        }

        public List<Ply> Plies
        {
            get { return _plies; }
            set { _plies = value; }
        }

        public bool Symmetric
        {
            get { return _symmetric; }
            set { _symmetric = value; }
        }

        public ShellOffsetType ShellOffset
        {
            get { return _shellOffset; }
            set { _shellOffset = value; }
        }

        public double OffsetRatio
        {
            get { return _offsetRatio; }
            set { _offsetRatio = value; }
        }

        public bool DiscreteOrientationsEnabled
        {
            get { return _orientationDefinition == OrientationDefinitionType.DiscreteOrientations; }
            set
            {
                if (value) _orientationDefinition = OrientationDefinitionType.DiscreteOrientations;
                else if (_orientationDefinition == OrientationDefinitionType.DiscreteOrientations)
                    _orientationDefinition = OrientationDefinitionType.PartGlobal;
            }
        }

        public DiscreteOrientationsData DiscreteOrientationsData
        {
            get { return _discreteOrientationsData; }
            set { _discreteOrientationsData = value; }
        }

        public bool UseDrapingForFiberDirection
        {
            get { return _useDrapingForFiberDirection; }
            set { _useDrapingForFiberDirection = value; }
        }

        public double DrapingCellSize
        {
            get { return _drapingCellSize; }
            set { _drapingCellSize = value; }
        }

        public int DrapingPlyIndex
        {
            get { return _drapingPlyIndex; }
            set { _drapingPlyIndex = value; }
        }

        public string SolidPartName
        {
            get { return _solidPartName; }
            set { _solidPartName = value; }
        }

        public string SourceShellPartName
        {
            get { return _sourceShellPartName; }
            set { _sourceShellPartName = value; }
        }

        // Rosette applied to every ply that does not define its own one
        public string OrientationRosette
        {
            get { return _orientationRosette; }
            set { _orientationRosette = value; }
        }

        public bool RosetteEnabled
        {
            get { return _orientationDefinition == OrientationDefinitionType.Rosette; }
        }


        // Constructors
        public CompositeLayup(string name, int initialPlyCount, CompositeElementType elementType)
            : base(name)
        {
            _initialPlyCount = initialPlyCount;
            _elementType = elementType;
            _processType = CaeModel.ProcessType.None;
            _orientationDefinition = OrientationDefinitionType.PartGlobal;
            _normalDirection = NormalDirectionType.Axis3;
            _additionalRotationType = AdditionalRotationType.None;
            _additionalRotationAngle = 0.0;
            _plies = new List<Ply>();
            _symmetric = false;
            _shellOffset = ShellOffsetType.MiddleSurface;
            _offsetRatio = 0.0;

            // Initialize plies based on initial ply count
            for (int i = 0; i < initialPlyCount; i++)
            {
                _plies.Add(new Ply("Ply-" + (i + 1)));
            }
        }


        // Methods
        // A ply rosette wins over the layup rosette; the layup rosette only applies when the layup
        // orientation definition is set to Rosette. Resolving on demand means that a ply added after
        // the rosette was selected inherits it without any additional bookkeeping.
        public string GetEffectivePlyRosette(int plyIndex)
        {
            if (_plies == null || plyIndex < 0 || plyIndex >= _plies.Count) return null;
            string plyRosette = _plies[plyIndex].OrientationRosette;
            if (!string.IsNullOrEmpty(plyRosette)) return plyRosette;
            return RosetteEnabled ? _orientationRosette : null;
        }

        // Same inheritance rule for the manufacturing process: an explicit ply process type wins,
        // otherwise the ply follows the layup
        public ProcessType GetEffectivePlyProcessType(int plyIndex)
        {
            if (_plies == null || plyIndex < 0 || plyIndex >= _plies.Count) return _processType;
            return _plies[plyIndex].ProcessType ?? _processType;
        }

        public bool IsPlyProcessTypeInherited(int plyIndex)
        {
            if (_plies == null || plyIndex < 0 || plyIndex >= _plies.Count) return true;
            return _plies[plyIndex].ProcessType == null;
        }

        public bool IsPlyRosetteInherited(int plyIndex)
        {
            if (_plies == null || plyIndex < 0 || plyIndex >= _plies.Count) return false;
            return string.IsNullOrEmpty(_plies[plyIndex].OrientationRosette) &&
                   RosetteEnabled && !string.IsNullOrEmpty(_orientationRosette);
        }

        public bool UsesRosette(string rosetteName)
        {
            if (string.IsNullOrEmpty(rosetteName)) return false;
            if (RosetteEnabled && _orientationRosette == rosetteName) return true;
            if (_plies != null)
                foreach (Ply ply in _plies) if (ply.OrientationRosette == rosetteName) return true;
            return false;
        }

        public bool UsesAnyRosette()
        {
            if (RosetteEnabled && !string.IsNullOrEmpty(_orientationRosette)) return true;
            if (_plies != null)
                foreach (Ply ply in _plies) if (!string.IsNullOrEmpty(ply.OrientationRosette)) return true;
            return false;
        }

        public Material GetPlyMaterial(int plyIndex, CaeGlobals.OrderedDictionary<string, Material> materials)
        {
            if (materials == null || plyIndex < 0 || plyIndex >= _plies.Count) return null;
            string materialName = _plies[plyIndex].Material;
            if (string.IsNullOrEmpty(materialName)) return null;
            Material material;
            return materials.TryGetValue(materialName, out material) ? material : null;
        }
        // The width of a woven or non-crimp fabric is its weave pitch and therefore the draping grid
        // cell size; the width of a unidirectional tape only sets the spacing of the fiber curves,
        // so the draping grid keeps its own finer resolution
        public double GetPlyDrapingCellSize(int plyIndex, CaeGlobals.OrderedDictionary<string, Material> materials,
                                            double defaultCellSize)
        {
            Material material = GetPlyMaterial(plyIndex, materials);
            if (material == null || material.CompositeMaterialType == CompositeMaterialType.Unidirectional) return defaultCellSize;
            if (material.Width.HasValue && material.Width.Value > 0) return material.Width.Value;
            return defaultCellSize;
        }
        // Number of draping grid columns between two displayed fiber curves
        public int GetPlyFiberCurveStride(int plyIndex, CaeGlobals.OrderedDictionary<string, Material> materials, double cellSize)
        {
            Material material = GetPlyMaterial(plyIndex, materials);
            if (material == null || !material.Width.HasValue || material.Width.Value <= 0 || cellSize <= 0) return 1;
            return Math.Max(1, (int)Math.Round(material.Width.Value / cellSize));
        }

        public void AddPly(Ply ply)
        {
            _plies.Add(ply);
        }

        public void RemovePly(int index)
        {
            if (index >= 0 && index < _plies.Count)
            {
                _plies.RemoveAt(index);
            }
        }

        public void InsertPly(int index, Ply ply)
        {
            if (index >= 0 && index <= _plies.Count)
            {
                _plies.Insert(index, ply);
            }
        }
    }
}
