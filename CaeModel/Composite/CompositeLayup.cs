// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
// Source code: https://gitlab.com/MatejB/PrePoMax
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard (Composites Fiber Modeler)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CaeGlobals;
using CaeMesh;
using DynamicTypeDescriptor;

namespace CaeModel
{
    [Serializable]
    public enum CompositeLayupOrientationEnum
    {
        // The reference axis of a coordinate system projected onto the shell and turned by the rotation angle, the
        // way a material orientation defines it. The global coordinate system and the x axis are the default.
        [StandardValue("CoordinateSystem", DisplayName = "Coordinate system")]
        CoordinateSystem,
        // Fiber directions of stacking points read from an XML file; each element takes the nearest point
        [StandardValue("DiscreteOrientations", DisplayName = "Discrete orientations")]
        DiscreteOrientations,
        // Fiber directions from a kinematic draping simulation of one ply
        [StandardValue("Draping", DisplayName = "Draping simulation")]
        Draping
    }

    [Serializable]
    public enum CompositeLayupOffsetEnum
    {
        [StandardValue("MiddleSurface", DisplayName = "Middle surface")]
        MiddleSurface,
        // The nodes lie on the top surface, so the layup lies below them (Abaqus SPOS, offset 0.5)
        [StandardValue("TopSurface", DisplayName = "Top surface")]
        TopSurface,
        // The nodes lie on the bottom surface, so the layup lies above them (Abaqus SNEG, offset -0.5)
        [StandardValue("BottomSurface", DisplayName = "Bottom surface")]
        BottomSurface,
        [StandardValue("Ratio", DisplayName = "Ratio of the thickness")]
        Ratio
    }

    // A composite layup of the Composites Fiber Modeler. Every ply covers a region of its own, so elements covered by
    // the same plies form a zone that is solved as a composite shell section with one layer per covering ply.
    [Serializable]
    public class CompositeLayup : NamedClass, IContainsEquations, ISerializable
    {
        // Variables
        public const int DefaultSymbolDensity = 50;
        //
        private List<Ply> _plies;                                       //ISerializable
        private bool _symmetric;                                        //ISerializable
        private CompositeLayupOrientationEnum _orientationType;         //ISerializable
        private string _coordinateSystemName;                           //ISerializable
        private MaterialOrientationAxisEnum _referenceAxis;             //ISerializable
        private EquationContainer _rotationAngle;                       //ISerializable
        private CompositeLayupOffsetEnum _offsetType;                   //ISerializable
        private EquationContainer _offsetRatio;                         //ISerializable
        private DiscreteOrientationsData _discreteOrientationsData;     //ISerializable
        private int _drapingPlyIndex;                                   //ISerializable
        private EquationContainer _drapingCellSize;                     //ISerializable
        private double _drapingLimitShearDeg;                           //ISerializable
        private int _symbolDensity;                                     //ISerializable


        // Properties
        public List<Ply> Plies { get { return _plies; } set { _plies = value ?? new List<Ply>(); } }
        public bool Symmetric { get { return _symmetric; } set { _symmetric = value; } }
        public CompositeLayupOrientationEnum OrientationType { get { return _orientationType; } set { _orientationType = value; } }
        public string CoordinateSystemName
        {
            get
            {
                if (_coordinateSystemName == null) return CoordinateSystem.DefaultCoordinateSystemName;
                else return _coordinateSystemName;
            }
            set
            {
                _coordinateSystemName = value;
                if (_coordinateSystemName == CoordinateSystem.DefaultCoordinateSystemName) _coordinateSystemName = null;
            }
        }
        public MaterialOrientationAxisEnum ReferenceAxis { get { return _referenceAxis; } set { _referenceAxis = value; } }
        public EquationContainer RotationAngle { get { return _rotationAngle; } set { SetRotationAngle(value); } }
        public CompositeLayupOffsetEnum OffsetType { get { return _offsetType; } set { _offsetType = value; } }
        public EquationContainer OffsetRatio { get { return _offsetRatio; } set { SetOffsetRatio(value); } }
        public DiscreteOrientationsData DiscreteOrientationsData
        {
            get { return _discreteOrientationsData; }
            set { _discreteOrientationsData = value; }
        }
        public int DrapingPlyIndex { get { return _drapingPlyIndex; } set { _drapingPlyIndex = Math.Max(0, value); } }
        public EquationContainer DrapingCellSize { get { return _drapingCellSize; } set { SetDrapingCellSize(value); } }
        // Cells sheared more than this angle are marked in the draping preview
        public double DrapingLimitShearDeg { get { return _drapingLimitShearDeg; } set { _drapingLimitShearDeg = value; } }
        // The percentage of the full density of the fiber direction symbols
        public int SymbolDensity
        {
            get { return _symbolDensity; }
            set
            {
                _symbolDensity = value;
                if (_symbolDensity < 0) _symbolDensity = 0;
                else if (_symbolDensity > 100) _symbolDensity = 100;
            }
        }


        // Constructors
        public CompositeLayup(string name)
            : base(name)
        {
            _plies = new List<Ply>();
            _symmetric = false;
            _orientationType = CompositeLayupOrientationEnum.CoordinateSystem;
            _coordinateSystemName = null;
            _referenceAxis = MaterialOrientationAxisEnum.X;
            RotationAngle = new EquationContainer(typeof(StringAngleDegConverter), 0);
            _offsetType = CompositeLayupOffsetEnum.MiddleSurface;
            OffsetRatio = new EquationContainer(typeof(StringDoubleConverter), 0);
            _discreteOrientationsData = null;
            _drapingPlyIndex = 0;
            DrapingCellSize = new EquationContainer(typeof(StringLengthConverter), 5);
            _drapingLimitShearDeg = 45;
            _symbolDensity = DefaultSymbolDensity;
        }
        public CompositeLayup(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _symbolDensity = DefaultSymbolDensity;
            _drapingLimitShearDeg = 45;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_plies":
                        _plies = (List<Ply>)entry.Value; break;
                    case "_symmetric":
                        _symmetric = (bool)entry.Value; break;
                    case "_orientationType":
                        _orientationType = (CompositeLayupOrientationEnum)entry.Value; break;
                    case "_coordinateSystemName":
                        _coordinateSystemName = (string)entry.Value; break;
                    case "_referenceAxis":
                        _referenceAxis = (MaterialOrientationAxisEnum)entry.Value; break;
                    case "_rotationAngle":
                        SetRotationAngle((EquationContainer)entry.Value, false); break;
                    case "_offsetType":
                        _offsetType = (CompositeLayupOffsetEnum)entry.Value; break;
                    case "_offsetRatio":
                        SetOffsetRatio((EquationContainer)entry.Value, false); break;
                    case "_discreteOrientationsData":
                        _discreteOrientationsData = (DiscreteOrientationsData)entry.Value; break;
                    case "_drapingPlyIndex":
                        _drapingPlyIndex = (int)entry.Value; break;
                    case "_drapingCellSize":
                        SetDrapingCellSize((EquationContainer)entry.Value, false); break;
                    case "_drapingLimitShearDeg":
                        _drapingLimitShearDeg = (double)entry.Value; break;
                    case "_symbolDensity":
                        _symbolDensity = (int)entry.Value; break;
                    default:
                        break;
                }
            }
            if (_plies == null) _plies = new List<Ply>();
        }


        // Methods
        private void SetRotationAngle(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _rotationAngle, value, CheckFinite, checkEquation);
        }
        private void SetOffsetRatio(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _offsetRatio, value, CheckFinite, checkEquation);
        }
        private void SetDrapingCellSize(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _drapingCellSize, value, CheckPositive, checkEquation);
        }
        private double CheckFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new CaeException("The value must be finite.");
            else return value;
        }
        private double CheckPositive(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                throw new CaeException("The value must be larger than 0.");
            else return value;
        }
        // The offset of *SHELL SECTION in units of the shell thickness
        public double GetOffset()
        {
            switch (_offsetType)
            {
                case CompositeLayupOffsetEnum.TopSurface: return 0.5;
                case CompositeLayupOffsetEnum.BottomSurface: return -0.5;
                case CompositeLayupOffsetEnum.Ratio: return _offsetRatio.Value;
                default: return 0;
            }
        }
        // The plies the solver gets, from ply 1 up; a symmetric layup is completed by its mirror image
        public List<Ply> GetSolverPlies()
        {
            List<Ply> plies = new List<Ply>(_plies);
            if (_symmetric)
            {
                for (int i = _plies.Count - 1; i >= 0; i--) plies.Add(_plies[i]);
            }
            return plies;
        }
        public double GetTotalThickness()
        {
            double thickness = 0;
            foreach (Ply ply in GetSolverPlies()) thickness += ply.Thickness.Value;
            return thickness;
        }
        public HashSet<string> GetMaterialNames()
        {
            HashSet<string> materialNames = new HashSet<string>();
            foreach (Ply ply in _plies) materialNames.Add(ply.MaterialName);
            return materialNames;
        }
        public HashSet<string> GetRegionNames()
        {
            HashSet<string> regionNames = new HashSet<string>();
            foreach (Ply ply in _plies) if (!string.IsNullOrEmpty(ply.RegionName)) regionNames.Add(ply.RegionName);
            return regionNames;
        }
        // Element ids of a ply region: an element set or a part
        public static int[] GetRegionElementIds(FeMesh mesh, string regionName)
        {
            if (string.IsNullOrEmpty(regionName) || mesh == null) return null;
            //
            FeElementSet elementSet;
            BasePart part;
            if (mesh.ElementSets.TryGetValue(regionName, out elementSet))
            {
                if (!elementSet.CreatedFromParts) return elementSet.Labels;
                //
                HashSet<int> elementIds = new HashSet<int>();
                foreach (var partName in mesh.GetPartNamesFromPartIds(elementSet.Labels))
                    elementIds.UnionWith(mesh.Parts[partName].Labels);
                return elementIds.ToArray();
            }
            else if (mesh.Parts.TryGetValue(regionName, out part)) return part.Labels;
            return null;
        }
        public HashSet<int> GetElementIds(FeMesh mesh)
        {
            HashSet<int> elementIds = new HashSet<int>();
            int[] ids;
            foreach (Ply ply in _plies)
            {
                ids = GetRegionElementIds(mesh, ply.RegionName);
                if (ids != null) elementIds.UnionWith(ids);
            }
            return elementIds;
        }
        // IContainsEquations
        public void CheckEquations()
        {
            if (_plies.Count == 0) throw new CaeException("A composite layup must contain at least one ply.");
            foreach (Ply ply in _plies) ply.CheckEquations();
            _rotationAngle.CheckEquation();
            _offsetRatio.CheckEquation();
            _drapingCellSize.CheckEquation();
        }
        public bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception) { return false; }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_plies", _plies, typeof(List<Ply>));
            info.AddValue("_symmetric", _symmetric, typeof(bool));
            info.AddValue("_orientationType", _orientationType, typeof(CompositeLayupOrientationEnum));
            info.AddValue("_coordinateSystemName", _coordinateSystemName, typeof(string));
            info.AddValue("_referenceAxis", _referenceAxis, typeof(MaterialOrientationAxisEnum));
            info.AddValue("_rotationAngle", _rotationAngle, typeof(EquationContainer));
            info.AddValue("_offsetType", _offsetType, typeof(CompositeLayupOffsetEnum));
            info.AddValue("_offsetRatio", _offsetRatio, typeof(EquationContainer));
            info.AddValue("_discreteOrientationsData", _discreteOrientationsData, typeof(DiscreteOrientationsData));
            info.AddValue("_drapingPlyIndex", _drapingPlyIndex, typeof(int));
            info.AddValue("_drapingCellSize", _drapingCellSize, typeof(EquationContainer));
            info.AddValue("_drapingLimitShearDeg", _drapingLimitShearDeg, typeof(double));
            info.AddValue("_symbolDensity", _symbolDensity, typeof(int));
        }
    }
}
