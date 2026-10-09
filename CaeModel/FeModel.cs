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
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using Calculix = FileInOut.Output.Calculix;
using System.IO;
using System.Drawing;
using System.Data;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using static System.Collections.Specialized.BitVector32;
using System.Net.NetworkInformation;
using System.Xml.Linq;
using FileInOut.Input;
using CaeResults;
using System.CodeDom;
using System.Collections.Concurrent;

namespace CaeModel
{
    [Serializable]
    public class FeModel : ISerializable
    {
        // Variables                                                                                                                
        private string _name;                                                                   //ISerializable
        private string _hashName;                                                               //ISerializable
        private FeMesh _geometry;                                                               //ISerializable
        private FeMesh _mesh;                                                                   //ISerializable
        protected OrderedDictionary<string, UserViewParameters> _userViews;                     //ISerializable
        private EquationParameterCollection _parameters;                                        //ISerializable
        private OrderedDictionary<string, Material> _materials;                                 //ISerializable
        private OrderedDictionary<string, Section> _sections;                                   //ISerializable
        private OrderedDictionary<string, Constraint> _constraints;                             //ISerializable
        private OrderedDictionary<string, SurfaceInteraction> _surfaceInteractions;             //ISerializable
        private OrderedDictionary<string, ContactPair> _contactPairs;                           //ISerializable
        private OrderedDictionary<string, Distribution> _distributions;                         //ISerializable
        private OrderedDictionary<string, Amplitude> _amplitudes;                               //ISerializable
        private OrderedDictionary<string, InitialCondition> _initialConditions;                 //ISerializable
        private StepCollection _stepCollection;                                                 //ISerializable
        private OrderedDictionary<int[], Calculix.CalculixUserKeyword> _calculixUserKeywords;   //ISerializable
        private List<Calculix.CalculixKeywordOverride> _calculixKeywordOverrides;                //ISerializable
        private ModelProperties _properties;                                                    //ISerializable
        private UnitSystem _unitSystem;                                                         //ISerializable
        [NonSerialized] private Dictionary<int, int> _elementIdSectionId;
        [NonSerialized] private List<Section> _assignedSections;
        [NonSerialized] private int[] _unAssignedElementIds;
        [NonSerialized] private object[] _sectionAssignmentsState;
        [NonSerialized] private string _meshTypesStamp;
        [NonSerialized] private HashSet<int> _heldPartIds;


        // Properties                                                                                                               
        public string Name { get { return _name; } set { _name = value; } }
        public string HashName { get { return _hashName; } }
        public FeMesh Geometry { get { return _geometry; } set { _geometry = value; } }
        public FeMesh Mesh { get { return _mesh; } }
        public OrderedDictionary<string, UserViewParameters> UserViews { get { return _userViews; } }
        public EquationParameterCollection Parameters { get { return _parameters; } }
        public OrderedDictionary<string, Material> Materials { get { return _materials; } }
        public OrderedDictionary<string, Section> Sections { get { return _sections; } }
        public OrderedDictionary<string, Constraint> Constraints { get { return _constraints; } }
        public OrderedDictionary<string, SurfaceInteraction> SurfaceInteractions { get { return _surfaceInteractions; } }
        public OrderedDictionary<string, ContactPair> ContactPairs { get { return _contactPairs; } }
        public OrderedDictionary<string, Distribution> Distributions { get { return _distributions; } }
        public OrderedDictionary<string, Amplitude> Amplitudes { get { return _amplitudes; } }
        public OrderedDictionary<string, InitialCondition> InitialConditions { get { return _initialConditions; } }
        public StepCollection StepCollection { get { return _stepCollection; } }
        public OrderedDictionary<int[], Calculix.CalculixUserKeyword> CalculixUserKeywords 
        { 
            get { return _calculixUserKeywords; } 
            set 
            {
                _calculixUserKeywords = value;
            } 
        }
        public List<Calculix.CalculixKeywordOverride> CalculixKeywordOverrides
        {
            get { return _calculixKeywordOverrides; }
            set
            {
                _calculixKeywordOverrides = value ?? new List<Calculix.CalculixKeywordOverride>();
            }
        }
        public ModelProperties Properties { get { return _properties; } set { _properties = value; } }
        public UnitSystem UnitSystem
        {
            get { return _unitSystem; }
            set { _unitSystem = value; }
        }


        // Constructors                                                                                                             
        public FeModel(string name, UnitSystem unitSystem, bool resetGlobalNCalcParameters,
                       OrderedDictionary<string, EquationParameter> overriddenParameters = null)
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;
            //
            Name = name;
            _hashName = Tools.GetRandomString(8);
            _geometry = new FeMesh(MeshRepresentation.Geometry);
            _mesh = new FeMesh(MeshRepresentation.Mesh);
            _userViews = new OrderedDictionary<string, UserViewParameters>("User Views", sc);
            _parameters = new EquationParameterCollection();
            _materials = new OrderedDictionary<string, Material>("Materials", sc);
            _sections = new OrderedDictionary<string, Section>("Sections", sc);
            _constraints = new OrderedDictionary<string, Constraint>("Constraints", sc);
            _surfaceInteractions = new OrderedDictionary<string, SurfaceInteraction>("Surface Tractions", sc);
            _contactPairs = new OrderedDictionary<string, ContactPair>("Contact Pairs", sc);
            _initialConditions = new OrderedDictionary<string, InitialCondition>("Initial Conditions", sc);
            _distributions = new OrderedDictionary<string, Distribution>("Distributions", sc);
            _amplitudes = new OrderedDictionary<string, Amplitude>("Amplitudes", sc);
            _stepCollection = new StepCollection();
            _calculixKeywordOverrides = new List<Calculix.CalculixKeywordOverride>();
            _properties = new ModelProperties();
            if (unitSystem == null) _unitSystem = new UnitSystem();
            else _unitSystem = unitSystem;
            //
            if (resetGlobalNCalcParameters) UpdateNCalcParameters();
            // Set overridden parameters
            if (overriddenParameters != null)
            {
                foreach (var entry in overriddenParameters) _parameters.AddOverriddenParameter(entry.Key, entry.Value);
            }
        }
        public FeModel(FeModel model)
        {
            FeModel copy = model.DeepCopy();
            _name = copy._name;
            _hashName = copy._hashName;
            _geometry = copy._geometry;
            _mesh = copy._mesh;
            _userViews = copy._userViews;
            _parameters = copy._parameters;
            _materials = copy._materials;
            _sections = copy._sections;
            _constraints = copy._constraints;
            _surfaceInteractions = copy._surfaceInteractions;
            _contactPairs = copy._contactPairs;
            _distributions = copy._distributions;
            _amplitudes = copy._amplitudes;
            _initialConditions = copy._initialConditions;
            _stepCollection = copy._stepCollection;
            _calculixUserKeywords = copy._calculixUserKeywords;
            _calculixKeywordOverrides = copy._calculixKeywordOverrides;
            _properties = copy._properties;
            _unitSystem = copy._unitSystem;
        }
        public FeModel(SerializationInfo info, StreamingContext context)
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;
            // Compatibility for version v.0.6.0
            _surfaceInteractions = new OrderedDictionary<string, SurfaceInteraction>("Surface Tractions", sc);
            _contactPairs = new OrderedDictionary<string, ContactPair>("Contact Pairs", sc);
            // Compatibility for version v.0.7.0
            _unitSystem = new UnitSystem();
            // Compatibility for version v.0.8.0
            _hashName = Tools.GetRandomString(8);
            // Compatibility for version v.1.0.0
            _initialConditions = new OrderedDictionary<string, InitialCondition>("Initial Conditions", sc);
            // Compatibility for version v.2.3.3
            _distributions = new OrderedDictionary<string, Distribution>("Distributions", sc);
            // Compatibility for version v.1.2.1
            _amplitudes = new OrderedDictionary<string, Amplitude>("Amplitudes", sc);
            // Compatibility for version v.1.4.0
            _parameters = new EquationParameterCollection();
            // Compatibility for version v.2.3.9
            _userViews = new OrderedDictionary<string, UserViewParameters>("User Views", sc);
            // Compatibility for models saved before CalculiX keyword overrides were introduced
            _calculixKeywordOverrides = new List<Calculix.CalculixKeywordOverride>();
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "<Name>k__BackingField":               // Compatibility for version v.0.5.1
                    case "_name":
                        _name = (string)entry.Value; break;
                    case "_geometry":
                        _geometry = (FeMesh)entry.Value; break;
                    case "_mesh":
                        _mesh = (FeMesh)entry.Value; break;
                    case "_userViews":
                        _userViews = (OrderedDictionary<string, UserViewParameters>)entry.Value;
                        break;
                    case "_parameters":                         // Compatibility for version v.2.1.11
                        if (entry.Value is OrderedDictionary<string, EquationParameter> dic)
                        {
                            dic.OnDeserialization(null);
                            _parameters = new EquationParameterCollection(dic);
                        }
                        else _parameters = (EquationParameterCollection)entry.Value;
                        break;
                    case "_materials":
                        if (entry.Value is Dictionary<string, Material> md)
                        {
                            // Compatibility for version v.0.5.1
                            md.OnDeserialization(null);
                            _materials = new OrderedDictionary<string, Material>("Materials", md, sc);
                        }
                        else if (entry.Value is OrderedDictionary<string, Material> mod) _materials = mod;
                        else if (entry.Value == null) _materials = null;
                        else throw new NotSupportedException();
                        break;
                    case "_sections":
                        if (entry.Value is Dictionary<string, Section> sd)
                        {
                            // Compatibility for version v.0.5.1
                            sd.OnDeserialization(null);
                            _sections = new OrderedDictionary<string, Section>("Sections", sd, sc);
                        }
                        else if (entry.Value is OrderedDictionary<string, Section> sod) _sections = sod;
                        else if (entry.Value == null) _sections = null;
                        else throw new NotSupportedException();
                        break;
                    case "_constraints":
                        if (entry.Value is Dictionary<string, Constraint> cd)   
                        {
                            // Compatibility for version v.0.5.1
                            cd.OnDeserialization(null);
                            _constraints = new OrderedDictionary<string, Constraint>("Constraints", cd, sc);
                        }
                        else if (entry.Value is OrderedDictionary<string, Constraint> cod) _constraints = cod;
                        else if (entry.Value == null) _constraints = null;
                        else throw new NotSupportedException();
                        break;
                    case "_surfaceInteractions":
                        _surfaceInteractions = (OrderedDictionary<string, SurfaceInteraction>)entry.Value; break;
                    case "_contactPairs":
                        _contactPairs = (OrderedDictionary<string, ContactPair>)entry.Value; break;
                    case "_distributions":
                        _distributions = (OrderedDictionary<string, Distribution>)entry.Value; break;
                    case "_amplitudes":
                        _amplitudes = (OrderedDictionary<string, Amplitude>)entry.Value; break;
                    case "_initialConditions":
                        _initialConditions = (OrderedDictionary<string, InitialCondition>)entry.Value; break;
                    case "_stepCollection":
                        _stepCollection = (StepCollection)entry.Value; break;
                    case "_calculixUserKeywords":
                        if (entry.Value is Dictionary<int[], Calculix.CalculixUserKeyword> cukd)
                        {
                            // Compatibility for version v.0.5.1
                            cukd.OnDeserialization(null);
                            _calculixUserKeywords = new OrderedDictionary<int[], Calculix.CalculixUserKeyword>("Keywords", cukd);
                        }
                        else if (entry.Value is OrderedDictionary<int[], Calculix.CalculixUserKeyword> cukod)
                            _calculixUserKeywords = cukod;
                        else if (entry.Value == null) _calculixUserKeywords = null;
                        else throw new NotSupportedException();
                        break;
                    case "_calculixKeywordOverrides":
                        CalculixKeywordOverrides = (List<Calculix.CalculixKeywordOverride>)entry.Value;
                        break;
                    case "_properties":
                        _properties = (ModelProperties)entry.Value;
                        break;
                    case "_unitSystem":
                        _unitSystem = (UnitSystem)entry.Value; break;
                    case "_hashName":
                        _hashName = (string)entry.Value; break;
                    default:
                        throw new NotSupportedException();
                }
            }
            //
            _unitSystem.SetConverterUnits();
            //
            // Call it to load the dictionary immediately
            //_parameters.OnDeserialization(null);    
            //_geometry.Parts.OnDeserialization(null);
            //_mesh.Parts.OnDeserialization(null);
            //UpdateNCalcParameters();  // is called after the data is read from binary reader
        }


        // Methods                                                                                                                  
        public static void WriteToBinaryWriter(FeModel model, BinaryWriter bw)
        {
            // Write geometry
            if (model == null || model.Geometry == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                FeMesh.WriteToBinaryWriter(model.Geometry, bw);
            }
            // Write mesh
            if (model == null || model.Mesh == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                FeMesh.WriteToBinaryWriter(model.Mesh, bw);
            }
        }
        public static void ReadFromBinaryReader(FeModel model, BinaryReader br, int version)
        {
            // Read geometry
            int geometryExists = br.ReadInt32();
            if (geometryExists == 1)
            {
                FeMesh.ReadFromBinaryReader(model.Geometry, br, version);
            }
            // Read mesh
            int meshExists = br.ReadInt32();
            if (meshExists == 1)
            {
                FeMesh.ReadFromBinaryReader(model.Mesh, br, version);
            }
        }
        // Check                                                                                    
        public string[] CheckValidity(List<Tuple<NamedClass, string>> items)
        {
            // Tuple<NamedClass, string>   ...   Tuple<invalidItem, stepName>
            if (_mesh == null) return new string[0];
            //
            List<string> invalidItems = new List<string>();
            bool valid = false;
            invalidItems.AddRange(_geometry.CheckValidity(items, IsMeshSetupItemProperlyDefined));
            invalidItems.AddRange(_mesh.CheckValidity(items, IsMeshSetupItemProperlyDefined));
            // Materials
            Material material;
            foreach (var entry in _materials)
            {
                material = entry.Value;
                valid = true;
                foreach (var property in material.Properties)
                {
                    // Check equations
                    valid &= property.TryCheckEquations();
                }
                //
                SetItemValidity(null, material, valid, items);
                if (!valid && material.Active) invalidItems.Add("Material: " + material.Name);
            }
            // Sections
            Section section;
            foreach (var entry in _sections)
            {
                section = entry.Value;
                // Region
                valid = IsSectionRegionValid(section);
                //
                if (section is SolidSection || section is ShellSection || section is MembraneSection)
                    valid &= _materials.ContainsValidActiveKey(section.MaterialName);
                else if (section is PointMassSection || section is DistributedMassSection) { }
                else throw new NotSupportedException();
                // Check equations
                valid &= section.TryCheckEquations();
                //
                SetItemValidity(null, section, valid, items);
                if (!valid && section.Active) invalidItems.Add("Section: " + section.Name);
            }
            // Constraints
            Constraint constraint;
            foreach (var entry in _constraints)
            {
                constraint = entry.Value;
                // Region
                valid = IsConstraintRegionValid(constraint);
                // Check equations
                valid &= constraint.TryCheckEquations();
                //
                SetItemValidity(null, constraint, valid, items);
                if (!valid && constraint.Active) invalidItems.Add("Constraint: " + constraint.Name);
            }
            // Contact pairs
            ContactPair contactPair;
            foreach (var entry in _contactPairs)
            {
                contactPair = entry.Value;
                valid = _surfaceInteractions.ContainsValidKey(contactPair.SurfaceInteractionName)
                        && _mesh.Surfaces.ContainsValidKey(contactPair.SlaveRegionName)
                        && _mesh.Surfaces.ContainsValidKey(contactPair.MasterRegionName);
                //
                SetItemValidity(null, contactPair, valid, items);
                if (!valid && contactPair.Active) invalidItems.Add("Contact pair: " + contactPair.Name);
            }
            // Distributions
            Distribution distribution;
            foreach (var entry in _distributions)
            {
                distribution = entry.Value;
                valid = true;
                // Coordinate system
                if (distribution.CoordinateSystemName != CaeMesh.CoordinateSystem.DefaultCoordinateSystemName &&
                    !_mesh.CoordinateSystems.ContainsValidKey(distribution.CoordinateSystemName)) valid = false;
                //
                SetItemValidity(null, distribution, valid, items);
                if (!valid && distribution.Active) invalidItems.Add("Distribution: " + distribution.Name);
            }
            // Initial conditions
            InitialCondition initialCondition;
            foreach (var entry in _initialConditions)
            {
                initialCondition = entry.Value;
                // Region
                valid = IsInitialConditionRegionValid(initialCondition);
                // Distribution
                if (initialCondition is IDistribution icd)
                {
                    if (icd.DistributionName != Distribution.DefaultDistributionName &&
                        !_distributions.ContainsValidKey(icd.DistributionName)) valid = false;
                }
                // Check equations
                valid &= initialCondition.TryCheckEquations();
                //
                SetItemValidity(null, initialCondition, valid, items);
                if (!valid && initialCondition.Active) invalidItems.Add("Initial condition: " + initialCondition.Name);
            }
            // Steps
            HistoryOutput historyOutput;
            BoundaryCondition bc;
            Load load;
            DefinedField definedField;
            //
            foreach (var step in _stepCollection.StepsList)
            {
                // History output
                foreach (var hoEntry in step.HistoryOutputs)
                {
                    historyOutput = hoEntry.Value;
                    // Region
                    valid = IsHistoryOutputRegionValid(historyOutput);
                    //
                    SetItemValidity(step.Name, historyOutput, valid, items);
                    if (!valid && historyOutput.Active) invalidItems.Add("History output: " + step.Name + ", " + historyOutput.Name);
                }
                // Boundary conditions
                foreach (var bcEntry in step.BoundaryConditions)
                {
                    bc = bcEntry.Value;
                    // Region
                    valid = IsBoundaryConditionRegionValid(bc);
                    // Distribution
                    if (bc is IDistribution bcd)
                    {
                        if (bcd.DistributionName != Distribution.DefaultDistributionName &&
                            !_distributions.ContainsValidKey(bcd.DistributionName)) valid = false;
                    }
                    // Amplitude
                    if (bc.AmplitudeName != Amplitude.DefaultAmplitudeName &&
                        !_amplitudes.ContainsValidKey(bc.AmplitudeName)) valid = false;
                    // Coordinate system
                    if (bc.CoordinateSystemName != CaeMesh.CoordinateSystem.DefaultCoordinateSystemName &&
                        !_mesh.CoordinateSystems.ContainsValidKey(bc.CoordinateSystemName)) valid = false;
                    // Check equations
                    valid &= bc.TryCheckEquations();
                    //
                    SetItemValidity(step.Name, bc, valid, items);
                    if (!valid && bc.Active) invalidItems.Add("Boundary condition: " + step.Name + ", " + bc.Name);
                }
                // Loads
                foreach (var loadEntry in step.Loads)
                {
                    load = loadEntry.Value;
                    // Region
                    valid = IsLoadRegionValid(load);
                    // D2 vs axisymmetric
                    if (valid && load is CentrifLoad cf)
                    {
                        if (_properties.ModelSpace == ModelSpaceEnum.PlaneStress ||
                            _properties.ModelSpace == ModelSpaceEnum.PlaneStrain)
                        {
                            if (cf.Axisymmetric == true) valid = false;
                        }
                        else if (_properties.ModelSpace == ModelSpaceEnum.Axisymmetric)
                        {
                            if (cf.Axisymmetric == false) valid = false;
                        }
                    }
                    // Distribution
                    if (load is IDistribution lwd)
                    {
                        if (lwd.DistributionName != Distribution.DefaultDistributionName &&
                            !_distributions.ContainsValidKey(lwd.DistributionName)) valid = false;
                    }
                    // Amplitude
                    if (load.AmplitudeName != Amplitude.DefaultAmplitudeName && 
                        !_amplitudes.ContainsValidKey(load.AmplitudeName)) valid = false;
                    if (load is FilmHeatTransfer fht && fht.CoefficientAmplitudeName != Amplitude.DefaultAmplitudeName &&
                        !_amplitudes.ContainsValidKey(fht.CoefficientAmplitudeName)) valid = false;
                    // Coordinate system
                    if (load.CoordinateSystemName != CoordinateSystem.DefaultCoordinateSystemName &&
                        !_mesh.CoordinateSystems.ContainsValidKey(load.CoordinateSystemName)) valid = false;
                    // Check equations
                    valid &= load.TryCheckEquations();
                    //
                    SetItemValidity(step.Name, load, valid, items);
                    if (!valid && load.Active) invalidItems.Add("Load: " + step.Name + ", " + load.Name);
                }
                // Defined fields
                foreach (var fieldEntry in step.DefinedFields)
                {
                    definedField = fieldEntry.Value;
                    // Region
                    valid = IsDefinedFieldRegionValid(definedField);
                    // Distribution
                    if (definedField is IDistribution dfd)
                    {
                        if (dfd.DistributionName != Distribution.DefaultDistributionName &&
                            !_distributions.ContainsValidKey(dfd.DistributionName)) valid = false;
                    }
                    // Check equations
                    valid &= definedField.TryCheckEquations();
                    //
                    SetItemValidity(step.Name, definedField, valid, items);
                    if (!valid && definedField.Active) invalidItems.Add("Defined field: " + step.Name + ", " + definedField.Name);
                }
            }
            //
            return invalidItems.ToArray();
        }
        private void Check2D(FeMesh mesh)
        {
            // Check for 2D geometry - the same check in ImportGeometry
            if (_properties.ModelSpace.IsTwoD())
            {
                if (!mesh.BoundingBox.Is2D())
                    throw new CaeException("The selected file does not contain 2D geometry in x-y plane.");
                else if (!Check2DSurfaceNormals(mesh))
                {
                    MessageBoxes.ShowWarning("The imported geometry contains faces oriented in the wrong direction (negative Z). " +
                                             "To use them in the analysis, change their orientation.");
                }
            }
        }
        private bool Check2DSurfaceNormals(FeMesh mesh)
        {
            int cellId;
            int[] cell;
            FeNode normal;
            foreach (var entry in mesh.Parts)
            {
                for (int i = 0; i < entry.Value.Visualization.CellIdsByFace.Length; i++)
                {
                    if (entry.Value.Visualization.CellIdsByFace[i].Length > 0)
                    {
                        cellId = entry.Value.Visualization.CellIdsByFace[i][0];
                        cell = entry.Value.Visualization.Cells[cellId];
                        normal = mesh.ComputeNormalFromFaceCellIndices(cell);
                        if (normal.Z < 0) return false;
                    }
                }
            }
            return true;
        }
        public bool IsSectionRegionValid(Section section)
        {
            bool valid;
            //
            if (section is SolidSection ss)
            {
                valid =
                    (ss.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(ss.RegionName))
                     || (ss.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(ss.RegionName));
            }
            else if (section is ShellSection shs)
            {
                valid =
                    (shs.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(shs.RegionName))
                     || (shs.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(shs.RegionName));
            }
            else if (section is MembraneSection ms)
            {
                valid =
                    (ms.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(ms.RegionName))
                     || (ms.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(ms.RegionName));
            }
            else if (section is PointMassSection pms)
            {
                valid =
                    (pms.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(pms.RegionName))
                     || (pms.RegionType == RegionTypeEnum.ReferencePointName &&
                     _mesh.ReferencePoints.ContainsValidKey(pms.RegionName));
            }
            else if (section is DistributedMassSection dms)
            {
                valid = dms.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(dms.RegionName);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsInitialConditionRegionValid(InitialCondition initialCondition)
        {
            bool valid;
            //
            if (initialCondition is InitialTemperature it)
            {
                valid = (it.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(it.RegionName)) ||
                        (it.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(it.RegionName));
            }
            else if (initialCondition is InitialTranslationalVelocity itv)
            {
                valid = (itv.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(itv.RegionName)) ||
                        (itv.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(itv.RegionName)) ||
                        (itv.RegionType == RegionTypeEnum.ReferencePointName
                         && _mesh.ReferencePoints.ContainsValidKey(itv.RegionName));
            }
            else if (initialCondition is InitialAngularVelocity iav)
            {
                valid = (iav.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(iav.RegionName)) ||
                        (iav.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(iav.RegionName)) ||
                        (iav.RegionType == RegionTypeEnum.ReferencePointName
                         && _mesh.ReferencePoints.ContainsValidKey(iav.RegionName));
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsHistoryOutputRegionValid(HistoryOutput historyOutput)
        {
            bool valid;
            //
            if (historyOutput is NodalHistoryOutput nho)
            {
                valid = (nho.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(nho.RegionName))
                        || (nho.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(nho.RegionName))
                        || (nho.RegionType == RegionTypeEnum.ReferencePointName
                        && _mesh.ReferencePoints.ContainsValidKey(nho.RegionName));
            }
            else if (historyOutput is ElementHistoryOutput eho)
            {
                valid = _mesh.ElementSets.ContainsValidKey(eho.RegionName);
            }
            else if (historyOutput is ContactHistoryOutput cho)
            {
                valid = _contactPairs.ContainsValidKey(cho.RegionName);
            }
            else if (historyOutput is SectionHistoryOutput sho)
            {
                valid = _mesh.Surfaces.ContainsValidKey(sho.RegionName);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsConstraintRegionValid(Constraint constraint)
        {
            bool valid;
            //
            if (constraint is PointSpring ps)
            {
                valid = (ps.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(ps.RegionName))
                        || (ps.RegionType == RegionTypeEnum.ReferencePointName
                           && _mesh.ReferencePoints.ContainsValidKey(ps.RegionName));
            }
            else if (constraint is SurfaceSpring ss)
            {
                valid = ss.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(ss.RegionName);
            }
            else if (constraint is CompressionOnly co)
            {
                valid = co.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(co.RegionName);
            }
            else if (constraint is RigidBody rb)
            {
                valid = (rb.ReferencePointName != null && _mesh.ReferencePoints.ContainsValidKey(rb.ReferencePointName))
                        && ((rb.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(rb.RegionName))
                        || (rb.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(rb.RegionName)));
            }
            else if (constraint is Coupling coupling)
            {
                valid = coupling.ReferencePointName != null
                        && _mesh.ReferencePoints.ContainsValidKey(coupling.ReferencePointName)
                        && coupling.RegionType == RegionTypeEnum.SurfaceName
                        && _mesh.Surfaces.ContainsValidKey(coupling.RegionName);
            }
            else if (constraint is CyclicSymmetry cyclicSymmetry)
            {
                FeSurface slaveSurface;
                FeSurface masterSurface;
                bool expansionRegionValid =
                    cyclicSymmetry.NumberOfGraphSectors == 1 ||
                    (cyclicSymmetry.RegionType == RegionTypeEnum.PartName &&
                     _mesh.Parts.ContainsValidKey(cyclicSymmetry.RegionName)) ||
                    (cyclicSymmetry.RegionType == RegionTypeEnum.ElementSetName &&
                     _mesh.ElementSets.ContainsValidKey(cyclicSymmetry.RegionName));
                valid = !_properties.ModelSpace.IsTwoD()
                        && _mesh.Surfaces.TryGetValue(cyclicSymmetry.SlaveRegionName, out slaveSurface)
                        && _mesh.Surfaces.TryGetValue(cyclicSymmetry.MasterRegionName, out masterSurface)
                        && slaveSurface.Valid && masterSurface.Valid
                        && _mesh.NodeSets.ContainsValidKey(slaveSurface.NodeSetName)
                        && _mesh.NodeSets.ContainsValidKey(masterSurface.NodeSetName)
                        && cyclicSymmetry.SlaveRegionName != cyclicSymmetry.MasterRegionName
                        && expansionRegionValid;
            }
            else if (constraint is Tie t)
            {
                valid = _mesh.Surfaces.ContainsValidKey(t.SlaveRegionName) && _mesh.Surfaces.ContainsValidKey(t.MasterRegionName)
                        && (t.SlaveRegionName != t.MasterRegionName);
            }
            else if (constraint is Joint joint)
            {
                valid = IsJointRegionValid(joint) && TryGetJointCenterAndAxis(joint, out _, out _);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsJointRegionValid(Joint joint)
        {
            return !_properties.ModelSpace.IsTwoD()
                   && _mesh.Surfaces.ContainsValidKey(joint.SlaveRegionName)
                   && _mesh.Surfaces.ContainsValidKey(joint.MasterRegionName)
                   && (joint.SlaveRegionName != joint.MasterRegionName);
        }
        public bool TryGetJointCenterAndAxis(Joint joint, out double[] center, out double[] axis)
        {
            center = null;
            axis = null;
            //
            Dictionary<FeFaceName, List<int>> faces = null;
            bool centerFromFaces = joint.CenterDefinition == JointCenterDefinitionEnum.MasterSurface;
            bool axisFromFaces = joint.HasAxis && joint.AxisDefinition == JointDefinitionEnum.MasterSurface;
            if ((centerFromFaces || axisFromFaces) && !TryGetJointMasterElementFaces(joint, out faces)) return false;
            //
            if (centerFromFaces)
            {
                if (!TryGetFacesCenter(faces, out center)) return false;
            }
            else center = joint.GetCenter();
            //
            if (!joint.HasAxis) return true;
            //
            if (axisFromFaces) return TryGetFacesAxisDirection(faces, out axis);
            //
            axis = joint.GetAxisDirection();
            double length = Math.Sqrt(axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2]);
            if (double.IsNaN(length) || double.IsInfinity(length) || length <= 1E-12) return false;
            Vec3D.Normalize(ref axis);
            //
            return true;
        }
        public void GetJointCenterAndAxis(Joint joint, out double[] center, out double[] axis)
        {
            if (TryGetJointCenterAndAxis(joint, out center, out axis)) return;
            //
            if (joint.HasUserDefinedAxis)
                throw new CaeException("The axis direction of the joint '" + joint.Name + "' must be non-zero.");
            else
                throw new CaeException("The centre or the axis direction of the joint '" + joint.Name +
                                       "' cannot be determined from the surface '" + joint.MasterRegionName +
                                       "'. The master surface must exist and be cylindrical; otherwise define the axis " +
                                       "direction.");
        }
        public void UpdateJointAutoValues(Joint joint)
        {
            // The values the master surface defines are kept in the joint, so that they can be shown and so that
            // they are the starting point when the definition is switched to user defined
            double stiffness;
            if (joint.HasSprings && joint.StiffnessDefinition == JointStiffnessDefinitionEnum.FromModel &&
                TryGetJointSpringStiffnessFromModel(joint, out stiffness))
                joint.Stiffness.SetEquationFromValue(stiffness);
            // A selected centre is resolved to coordinates as soon as the nodes the mode asks for are picked
            double[] point;
            if (joint.IsCenterFromSelection && joint.CenterCreationIds != null &&
                joint.CenterCreationIds.Length == joint.NumberOfCenterNodes &&
                (point = _mesh.GetPointFromNodeIds(joint.CenterCreationIds)) != null)
            {
                joint.X.SetEquationFromValue(point[0]);
                joint.Y.SetEquationFromValue(point[1]);
                joint.Z.SetEquationFromValue(point[2]);
            }
            //
            Dictionary<FeFaceName, List<int>> faces;
            double[] values;
            if (!TryGetJointMasterElementFaces(joint, out faces)) return;
            //
            if (joint.CenterDefinition == JointCenterDefinitionEnum.MasterSurface && TryGetFacesCenter(faces, out values))
            {
                joint.X.SetEquationFromValue(values[0]);
                joint.Y.SetEquationFromValue(values[1]);
                joint.Z.SetEquationFromValue(values[2]);
            }
            if (joint.HasAxis && joint.AxisDefinition == JointDefinitionEnum.MasterSurface &&
                TryGetFacesAxisDirection(faces, out values))
            {
                joint.N1.SetEquationFromValue(Math.Round(values[0], 6));
                joint.N2.SetEquationFromValue(Math.Round(values[1], 6));
                joint.N3.SetEquationFromValue(Math.Round(values[2], 6));
            }
        }
        public double GetJointSpringStiffness(Joint joint)
        {
            double stiffness;
            if (joint.StiffnessDefinition == JointStiffnessDefinitionEnum.UserDefined) return joint.Stiffness.Value;
            if (TryGetJointSpringStiffnessFromModel(joint, out stiffness)) return stiffness;
            //
            throw new CaeException("The spring stiffness of the joint '" + joint.Name + "' cannot be determined from the " +
                                   "model. Both regions must exist and their elements must carry a section with an elastic " +
                                   "material; otherwise define the stiffness.");
        }
        public bool TryGetJointSpringStiffnessFromModel(Joint joint, out double stiffness)
        {
            // The springs of a region together carry 100 E sqrt(A), which keeps the joint far stiffer than the regions it
            // connects whatever the mesh. E is the largest Young's modulus of the region elements; while they have no
            // section yet, which the model check reports, the largest one in the model stands in
            stiffness = 0;
            FeSurface surface;
            double area = 0;
            int nodeCount = 0;
            double youngsModulus = 0;
            foreach (string surfaceName in new string[] { joint.MasterRegionName, joint.SlaveRegionName })
            {
                if (surfaceName == null || !_mesh.Surfaces.TryGetValue(surfaceName, out surface) ||
                    surface.ElementFaces == null) return false;
                area += surface.Area;
                nodeCount += GetSurfaceNodeCount(surface);
                youngsModulus = Math.Max(youngsModulus, GetSurfaceYoungsModulus(surface));
            }
            if (area <= 0 || nodeCount == 0) return false;
            if (youngsModulus <= 0)
            {
                foreach (var entry in _materials) youngsModulus = Math.Max(youngsModulus, GetYoungsModulus(entry.Value));
            }
            if (youngsModulus <= 0) youngsModulus = 1;
            //
            stiffness = 100 * youngsModulus * Math.Sqrt(area / 2) / (nodeCount / 2.0);
            return true;
        }
        private int GetSurfaceNodeCount(FeSurface surface)
        {
            // Counted from the element faces, which a surface carries before its node set is created
            HashSet<int> nodeIds = new HashSet<int>();
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    nodeIds.UnionWith(_mesh.Elements[elementId].GetNodeIdsFromFaceName(entry.Key));
                }
            }
            return nodeIds.Count;
        }
        private double GetSurfaceYoungsModulus(FeSurface surface)
        {
            Dictionary<int, int> elementIdSectionId;
            List<Section> sections;
            GetSectionAssignments(out elementIdSectionId, out sections);
            //
            int sectionId;
            Material material;
            double youngsModulus = 0;
            if (surface.ElementFaces == null) return youngsModulus;
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    if (!elementIdSectionId.TryGetValue(elementId, out sectionId) || sectionId < 0) continue;
                    if (sections[sectionId].MaterialName == null ||
                        !_materials.TryGetValue(sections[sectionId].MaterialName, out material)) continue;
                    youngsModulus = Math.Max(youngsModulus, GetYoungsModulus(material));
                }
            }
            return youngsModulus;
        }
        private static double GetYoungsModulus(Material material)
        {
            foreach (var property in material.Properties)
            {
                if (property is Elastic elastic && elastic.YoungsPoissonsTemp.Length > 0 &&
                    elastic.YoungsPoissonsTemp[0].Length > 0) return elastic.YoungsPoissonsTemp[0][0].Value;
            }
            return 0;
        }
        private bool TryGetJointMasterElementFaces(Joint joint, out Dictionary<FeFaceName, List<int>> faces)
        {
            // The element faces of the master region, from the surface once it exists and from the selected face
            // ids while the joint is still being defined
            faces = null;
            //
            if (joint.MasterRegionType == RegionTypeEnum.SurfaceName)
            {
                FeSurface surface;
                FeElementSet elementSet;
                if (joint.MasterRegionName == null || !_mesh.Surfaces.TryGetValue(joint.MasterRegionName, out surface) ||
                    surface.ElementFaces == null) return false;
                //
                faces = new Dictionary<FeFaceName, List<int>>();
                foreach (var entry in surface.ElementFaces)
                {
                    if (!_mesh.ElementSets.TryGetValue(entry.Value, out elementSet)) return false;
                    faces.Add(entry.Key, new List<int>(elementSet.Labels));
                }
                return true;
            }
            else if (joint.MasterRegionType == RegionTypeEnum.Selection && joint.MasterCreationIds != null &&
                     joint.MasterCreationIds.Length > 0)
            {
                // A face id names an element that must exist for the face decoding not to fail
                foreach (var faceId in joint.MasterCreationIds)
                {
                    if (!_mesh.Elements.ContainsKey(faceId / 10)) return false;
                }
                _mesh.GetNodeIdsElementSetsFromFaceIds(joint.MasterCreationIds, out _, out faces, out _, out _);
                return true;
            }
            //
            return false;
        }
        private bool TryGetFacesCenter(Dictionary<FeFaceName, List<int>> faces, out double[] center)
        {
            center = new double[3];
            double area = 0;
            double faceArea;
            double[] faceCenter;
            FeElement element;
            foreach (var entry in faces)
            {
                foreach (var elementId in entry.Value)
                {
                    if (!_mesh.Elements.TryGetValue(elementId, out element)) return false;
                    faceCenter = element.GetFaceCG(entry.Key, _mesh.Nodes, out faceArea);
                    center[0] += faceCenter[0] * faceArea;
                    center[1] += faceCenter[1] * faceArea;
                    center[2] += faceCenter[2] * faceArea;
                    area += faceArea;
                }
            }
            if (area <= 0) return false;
            //
            center[0] /= area;
            center[1] /= area;
            center[2] /= area;
            return true;
        }
        private bool TryGetFacesAxisDirection(Dictionary<FeFaceName, List<int>> faces, out double[] axis)
        {
            // The axis of a cylindrical face is reported by the CAD kernel and travels with the mesh, so it is the
            // same direction whatever the mesh is. It is used whenever the surface carries it, because an axis
            // fitted from the mesh differs from surface to surface and moves when the mesh is changed
            if (TryGetFacesAxisDirectionFromGeometry(faces, out axis)) return true;
            //
            return TryFitFacesAxisDirection(faces, out axis);
        }
        private bool TryGetFacesAxisDirectionFromGeometry(Dictionary<FeFaceName, List<int>> faces, out double[] axis)
        {
            axis = null;
            //
            BasePart part;
            int faceId;
            double[] values;
            double[] faceAxis = new double[3];
            GeometryMetadataEntity face;
            FeElement element;
            foreach (var entry in faces)
            {
                foreach (var elementId in entry.Value)
                {
                    if (!_mesh.Elements.TryGetValue(elementId, out element)) return false;
                    if (!_mesh.GetFaceId(elementId, element.GetVtkCellFromFaceName(entry.Key),
                                         out part, out faceId) || faceId < 0) return false;
                    if (part.GeometryMetadata == null || part.Visualization.FaceTypes == null ||
                        faceId >= part.Visualization.FaceTypes.Length) return false;
                    if (part.Visualization.FaceTypes[faceId] != GeomFaceType.Cylinder) return false;
                    face = part.GeometryMetadata.GetEntity("face", faceId + 1);
                    if (face == null) return false;
                    if (!face.TryGetNumbers("axis", out values) || values.Length != 6) return false;
                    //
                    faceAxis[0] = values[3];
                    faceAxis[1] = values[4];
                    faceAxis[2] = values[5];
                    if (Math.Sqrt(faceAxis[0] * faceAxis[0] + faceAxis[1] * faceAxis[1] +
                                  faceAxis[2] * faceAxis[2]) <= 1E-12) return false;
                    Vec3D.Normalize(ref faceAxis);
                    if (axis == null) axis = (double[])faceAxis.Clone();
                    else if (Math.Abs(axis[0] * faceAxis[0] + axis[1] * faceAxis[1] +
                                      axis[2] * faceAxis[2]) < 1 - 1E-6)
                    {
                        axis = null;
                        return false;
                    }
                    break;
                }
            }
            return axis != null;
        }
        private bool TryFitFacesAxisDirection(Dictionary<FeFaceName, List<int>> faces, out double[] axis)
        {
            // Every face normal of a cylindrical surface is perpendicular to its axis, so the cross products of the
            // first normal with the others all point along the axis
            double area;
            double totalArea = 0;
            double[] normal;
            double[] firstNormal = null;
            double[] cross = new double[3];
            axis = new double[3];
            double[,] covariance = new double[3, 3];
            foreach (var entry in faces)
            {
                foreach (var elementId in entry.Value)
                {
                    if (!_mesh.Elements.ContainsKey(elementId)) return false;
                    _mesh.GetElementFaceNormalAndArea(elementId, entry.Key, out normal, out area);
                    totalArea += area;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++) covariance[i, j] += normal[i] * normal[j] * area;
                    }
                    if (firstNormal == null)
                    {
                        firstNormal = normal;
                        continue;
                    }
                    Vec3D.CrossProduct(firstNormal, normal, ref cross);
                    if (cross[0] * axis[0] + cross[1] * axis[1] + cross[2] * axis[2] < 0) area = -area;
                    axis[0] += cross[0] * area;
                    axis[1] += cross[1] * area;
                    axis[2] += cross[2] * area;
                }
            }
            //
            double length = Math.Sqrt(axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2]);
            if (totalArea <= 0 || length <= 1E-3 * totalArea) return false;
            Vec3D.Normalize(ref axis);
            // Least squares refinement: the axis minimizing the squared projections of the normals is the eigenvector
            // of the smallest eigenvalue of their covariance, the dominant one of its complement
            double trace = covariance[0, 0] + covariance[1, 1] + covariance[2, 2];
            double[] next = new double[3];
            for (int iteration = 0; iteration < 100; iteration++)
            {
                for (int i = 0; i < 3; i++)
                {
                    next[i] = trace * axis[i] - (covariance[i, 0] * axis[0] + covariance[i, 1] * axis[1] +
                                                 covariance[i, 2] * axis[2]);
                }
                Vec3D.Normalize(ref next);
                double change = Math.Abs(next[0] - axis[0]) + Math.Abs(next[1] - axis[1]) + Math.Abs(next[2] - axis[2]);
                Array.Copy(next, axis, 3);
                if (change < 1E-14) break;
            }
            //
            return true;
        }
        public void CheckCyclicSymmetryCompatibility(CyclicSymmetry cyclicSymmetry,
                                                     string constraintNameToIgnore)
        {
            if (cyclicSymmetry == null || !cyclicSymmetry.Active) return;
            //
            foreach (var entry in _constraints)
            {
                if (string.Equals(entry.Key, constraintNameToIgnore, StringComparison.OrdinalIgnoreCase)) continue;
                if (!(entry.Value is CyclicSymmetry other) || !other.Active) continue;
                //
                if (cyclicSymmetry.NumberOfSectors != other.NumberOfSectors)
                    throw new CaeException("Active cyclic symmetry constraints '" + cyclicSymmetry.Name + "' and '" +
                                           other.Name + "' must use the same number of sectors.");
                // The axis direction is intentionally required to be co-directed. CalculiX requires all dependent
                // surfaces to be on the same side when viewed along the cyclic symmetry axes.
                Vec3D direction = new Vec3D(cyclicSymmetry.GetAxisDirection());
                Vec3D otherDirection = new Vec3D(other.GetAxisDirection());
                direction.Normalize();
                otherDirection.Normalize();
                if ((direction - otherDirection).Len > 1E-8)
                    throw new CaeException("Active cyclic symmetry constraints '" + cyclicSymmetry.Name + "' and '" +
                                           other.Name + "' must use the same axis direction.");
                // Equivalent points may be defined anywhere along the common axis. Compare the perpendicular
                // distance between the two axis lines using a tolerance scaled to the model size.
                Vec3D point = new Vec3D(cyclicSymmetry.GetAxisPoint());
                Vec3D otherPoint = new Vec3D(other.GetAxisPoint());
                double modelSize = _mesh.BoundingBox.GetDiagonal();
                if (double.IsNaN(modelSize) || double.IsInfinity(modelSize)) modelSize = 0;
                double axisTolerance = Math.Max(1E-12, 1E-8 * modelSize);
                double axisDistance = Vec3D.CrossProduct(otherPoint - point, direction).Len;
                if (axisDistance > axisTolerance)
                    throw new CaeException("Active cyclic symmetry constraints '" + cyclicSymmetry.Name + "' and '" +
                                           other.Name + "' must use the same rotation axis.");
            }
        }
        public bool IsBoundaryConditionRegionValid(BoundaryCondition bc)
        {
            bool valid;
            //
            if (bc is FixedBC fix)
            {
                valid = (fix.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(fix.RegionName))
                        || (fix.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(fix.RegionName)))
                        || (fix.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(fix.RegionName)));
            }
            else if (bc is DisplacementRotation dr)
            {
                valid = (dr.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(dr.RegionName))
                        || (dr.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(dr.RegionName)))
                        || (dr.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(dr.RegionName)));
            }
            else if (bc is SubmodelBC sm)
            {
                valid = (sm.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(sm.RegionName))
                        || (sm.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(sm.RegionName)));
            }
            else if (bc is TemperatureBC tmp)
            {
                valid = (tmp.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(tmp.RegionName))
                        || (tmp.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(tmp.RegionName)));
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsLoadRegionValid(Load load)
        {
            bool valid;
            FeSurface s;
            //
            if (load is CLoad cl)
            {
                valid = (cl.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(cl.RegionName))
                        || (cl.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(cl.RegionName)));
            }
            else if (load is MomentLoad ml)
            {
                valid = (ml.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(ml.RegionName))
                        || (ml.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(ml.RegionName)));
            }
            else if (load is DLoad dl)
            {
                valid = (_mesh.Surfaces.TryGetValue(dl.RegionName, out s) && s.Valid);
            }
            else if (load is HydrostaticPressure hpl)
            {
                valid = (_mesh.Surfaces.TryGetValue(hpl.SurfaceName, out s) && s.Valid);
            }
            else if (load is ImportedPressure ip)
            {
                valid = (_mesh.Surfaces.TryGetValue(ip.SurfaceName, out s) && s.Valid);
            }
            else if (load is STLoad stl)
            {
                valid = (_mesh.Surfaces.TryGetValue(stl.SurfaceName, out s) && s.Valid);
            }
            else if (load is ImportedSTLoad istl)
            {
                valid = (_mesh.Surfaces.TryGetValue(istl.SurfaceName, out s) && s.Valid);
            }
            else if (load is ShellEdgeLoad sel)
            {
                valid = (_mesh.Surfaces.TryGetValue(sel.SurfaceName, out s) && s.Valid);
            }
            else if (load is GravityLoad gl)
            {
                valid = (gl.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(gl.RegionName)) ||
                        (gl.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(gl.RegionName)) ||
                        (gl.RegionType == RegionTypeEnum.MassSection && _sections.ContainsValidKey(gl.RegionName)) ||
                        (gl.RegionType == RegionTypeEnum.Selection && _mesh.GetPartNamesFromPartIds(gl.CreationIds) != null &&
                        _mesh.GetPartNamesFromPartIds(gl.CreationIds).Length == gl.CreationIds.Length);
            }
            else if (load is CentrifLoad cf)
            {
                valid = (cf.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(cf.RegionName)) ||
                        (cf.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(cf.RegionName)) ||
                        (cf.RegionType == RegionTypeEnum.MassSection && _sections.ContainsValidKey(cf.RegionName)) ||
                        (cf.RegionType == RegionTypeEnum.Selection && _mesh.GetPartNamesFromPartIds(cf.CreationIds) != null &&
                        _mesh.GetPartNamesFromPartIds(cf.CreationIds).Length == cf.CreationIds.Length);
            }
            else if (load is PreTensionLoad ptl)
            {
                valid = (_mesh.Surfaces.TryGetValue(ptl.SurfaceName, out s) && s.Valid && s.Type == FeSurfaceType.Element);
            }
            // Thermal
            else if (load is CFlux cfl)
            {
                valid = (cfl.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(cfl.RegionName));
            }
            else if (load is DFlux df)
            {
                valid = (_mesh.Surfaces.TryGetValue(df.SurfaceName, out s) && s.Valid);
            }
            else if (load is BodyFlux bf)
            {
                valid = (bf.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(bf.RegionName))
                        || (bf.RegionType == RegionTypeEnum.ElementSetName
                        && _mesh.ElementSets.ContainsValidKey(bf.RegionName)
                        || (bf.RegionType == RegionTypeEnum.Selection && _mesh.GetPartNamesFromPartIds(bf.CreationIds) != null &&
                           _mesh.GetPartNamesFromPartIds(bf.CreationIds).Length == bf.CreationIds.Length));
            }
            else if (load is FilmHeatTransfer fht)
            {
                valid = (_mesh.Surfaces.TryGetValue(fht.SurfaceName, out s) && s.Valid);
            }
            else if (load is RadiationHeatTransfer rht)
            {
                valid = (_mesh.Surfaces.TryGetValue(rht.SurfaceName, out s) && s.Valid);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsDefinedFieldRegionValid(DefinedField definedField)
        {
            bool valid;
            //
            if (definedField is DefinedTemperature dt)
            {
                if (dt.Type == DefinedTemperatureTypeEnum.ByValue)
                {
                    valid = (dt.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(dt.RegionName)) ||
                            (dt.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(dt.RegionName));
                }
                else if (dt.Type == DefinedTemperatureTypeEnum.FromFile)
                {
                    // Defined field created from file needs no selection
                    valid = true;
                }
                else throw new NotSupportedException();
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        private void SetItemValidity(string stepName, NamedClass item, bool validity, List<Tuple<NamedClass, string>> items)
        {
            if (item.Valid != validity)
            {
                item.Valid = validity;
                items.Add(new Tuple<NamedClass, string>(item, stepName));
            }
        }
        public bool RegionValid(IMultiRegion multiRegion)
        {
            if (multiRegion.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsKey(multiRegion.RegionName))
                return _mesh.NodeSets[multiRegion.RegionName].Valid;
            else if (multiRegion.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsKey(multiRegion.RegionName))
                return _mesh.ElementSets[multiRegion.RegionName].Valid;
            else if (multiRegion.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsKey(multiRegion.RegionName))
                return _mesh.Surfaces[multiRegion.RegionName].Valid;
            else if (multiRegion.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsKey(multiRegion.RegionName))
                return _mesh.Parts[multiRegion.RegionName].Valid;
            //
            return false;
        }
        //
        public int[] GetSectionAssignments(out Dictionary<int, int> elementIdSectionId)
        {
            return GetSectionAssignments(out elementIdSectionId, out _);
        }
        public int[] GetSectionAssignments(out Dictionary<int, int> elementIdSectionId, out List<Section> assignedSections)
        {
            UpdateSectionAssignments();
            // The cached collections must stay unchanged, so the caller gets its own copies
            elementIdSectionId = new Dictionary<int, int>(_elementIdSectionId);
            assignedSections = new List<Section>(_assignedSections);
            return _unAssignedElementIds.ToArray();
        }
        private void UpdateSectionAssignments()
        {
            // The map is rebuilt only after the mesh, the sections, the parts or the element sets change
            object[] sectionAssignmentsState = GetSectionAssignmentsState();
            if (AreSectionAssignmentStatesEqual(sectionAssignmentsState, _sectionAssignmentsState)) return;
            //
            ComputeSectionAssignments();
            _sectionAssignmentsState = sectionAssignmentsState;
        }
        private void ComputeSectionAssignments()
        {
            int sectionId = 0;
            Dictionary<int, int> elementIdSectionId = new Dictionary<int, int>();
            // The section id is the index in assignedSections; mass and inactive sections are skipped and get no id
            List<Section> assignedSections = new List<Section>();
            //
            foreach (var entry in _sections)
            {
                if (entry.Value is MassSection || !entry.Value.Active) continue;
                //
                if (entry.Value.RegionType == RegionTypeEnum.PartName)
                {
                    foreach (var elementId in _mesh.Parts[entry.Value.RegionName].Labels)
                        elementIdSectionId[elementId] = sectionId;
                }
                else if (entry.Value.RegionType == RegionTypeEnum.ElementSetName)
                {
                    if (entry.Value is SolidSection)
                    {
                        FeElementSet elementSet = _mesh.ElementSets[entry.Value.RegionName];
                        if (elementSet.CreatedFromParts)
                        {
                            string[] partNames = _mesh.GetPartNamesFromPartIds(elementSet.Labels);
                            foreach (var partName in partNames)
                            {
                                foreach (var elementId in _mesh.Parts[partName].Labels)
                                    elementIdSectionId[elementId] = sectionId;
                            }
                        }
                        else
                        {
                            foreach (var elementId in elementSet.Labels) elementIdSectionId[elementId] = sectionId;
                        }
                    }
                    else if (entry.Value is ShellSection || entry.Value is MembraneSection)
                    {
                        FeElementSet elementSet = _mesh.ElementSets[entry.Value.RegionName];
                        foreach (var elementId in elementSet.Labels) elementIdSectionId[elementId] = sectionId;
                    }
                    else throw new NotSupportedException();
                }
                else throw new NotSupportedException();
                //
                assignedSections.Add(entry.Value);
                sectionId++;
            }
            // Not active
            HashSet<int> inactiveElementIds = new HashSet<int>();
            foreach (var entry in _mesh.Parts)
                if (!entry.Value.Active) inactiveElementIds.UnionWith(entry.Value.Labels);
            // Not assigned
            List<int> unAssignedElementIds = new List<int>();
            foreach (int elementId in _mesh.Elements.Keys)
            {
                if (elementIdSectionId.ContainsKey(elementId) || inactiveElementIds.Contains(elementId)) continue;
                //
                unAssignedElementIds.Add(elementId);
            }
            foreach (int elementId in unAssignedElementIds) elementIdSectionId.Add(elementId, -1);
            //
            _elementIdSectionId = elementIdSectionId;
            _assignedSections = assignedSections;
            _unAssignedElementIds = unAssignedElementIds.ToArray();
        }
        private object[] GetSectionAssignmentsState()
        {
            // Everything the map is built from; the model objects and the label arrays are compared by reference.
            // Renumbering changes the labels in place but always replaces the element dictionary, so that is included too
            List<object> state = new List<object>();
            state.Add(_mesh);
            state.Add(_mesh.Elements);
            state.Add(_mesh.Elements.Count);
            state.Add(_mesh.MaxElementId);
            foreach (var entry in _sections)
            {
                state.Add(entry.Key);
                state.Add(entry.Value);
                state.Add(entry.Value.Active);
                state.Add(entry.Value.RegionType);
                state.Add(entry.Value.RegionName);
            }
            foreach (var entry in _mesh.Parts)
            {
                state.Add(entry.Key);
                state.Add(entry.Value);
                state.Add(entry.Value.Active);
                state.Add(entry.Value.Labels);
            }
            foreach (var entry in _mesh.ElementSets)
            {
                state.Add(entry.Key);
                state.Add(entry.Value);
                state.Add(entry.Value.CreatedFromParts);
                state.Add(entry.Value.Labels);
            }
            return state.ToArray();
        }
        private static bool AreSectionAssignmentStatesEqual(object[] state1, object[] state2)
        {
            if (state1 == null || state2 == null || state1.Length != state2.Length) return false;
            //
            for (int i = 0; i < state1.Length; i++)
            {
                if (ReferenceEquals(state1[i], state2[i])) continue;
                if (state1[i] == null || state2[i] == null) return false;
                // Only the value types and the strings may differ in identity and still describe the same state
                if ((state1[i] is ValueType || state1[i] is string) && state1[i].Equals(state2[i])) continue;
                //
                return false;
            }
            return true;
        }
        public void GetMaterialAssignments(out Dictionary<int, int> elementIdMaterialId)
        {
            // Get element section ids
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            // Get material ids
            int count = 0;
            Dictionary<string, int> materialId = new Dictionary<string, int>();
            foreach (var entry in _materials) materialId.Add(entry.Value.Name, count++);
            // Get a map of section materials
            int id;
            Dictionary<int, int> sectionIdMaterialId = new Dictionary<int, int>();
            sectionIdMaterialId.Add(-1, -1);    // add missing section
            for (int i = 0; i < assignedSections.Count; i++)
            {
                if (assignedSections[i].MaterialName == null ||
                    !materialId.TryGetValue(assignedSections[i].MaterialName, out id)) id = -1;
                sectionIdMaterialId.Add(i, id);
            }
            // Use the map
            elementIdMaterialId = new Dictionary<int, int>();
            foreach (var entry in elementIdSectionId) elementIdMaterialId.Add(entry.Key, sectionIdMaterialId[entry.Value]);
        }
        public void GetNodalSafetyFactorLimits()
        {
            Dictionary<int, int> elementIdMaterialId;
            GetMaterialAssignments(out elementIdMaterialId);
            //
            int count = 0;
            Dictionary<int, double> materialIdSafetyFactor = new Dictionary<int, double>();
            foreach (var entry in _materials)
            {
                //if (safetyFactor != null) materialIdSafetyFactor.Add(count++, safetyFactor.SafetyFactorLimitValue);
                //else materialIdSafetyFactor.Add(count++, double.NaN);
            }
            //
            Dictionary<int, double> nodeIdSafetyFactorLimit = new Dictionary<int, double>();
        }
        public bool AreSlipWearCoefficientsDefined(out Dictionary<int, double> materialIdCoefficient)
        {
            int count = 0;
            bool containsWear = false;
            double coefficient;
            materialIdCoefficient = new Dictionary<int, double>();
            // For each material check if the material has wear coefficients defined
            foreach (var entry in _materials)
            {
                coefficient = 0;
                foreach (var property in entry.Value.Properties)
                {
                    if (property is SlipWear sw)
                    {
                        coefficient = sw.WearCoefficient.Value / sw.Hardness.Value * _properties.CyclesIncrement.Value;
                        containsWear = true;
                        break;
                    }
                }
                //
                materialIdCoefficient.Add(count++, coefficient);
            }
            return containsWear;
        }
        public Dictionary<int, double> GetNodalSlipWearCoefficients()
        {
            double coefficient;
            Dictionary<int, double> materialIdCoefficient;
            bool containsWear = AreSlipWearCoefficientsDefined(out materialIdCoefficient);
            // If wear coefficients are defined
            if (containsWear)
            {
                Dictionary<int, int> elementIdMaterialId;
                GetMaterialAssignments(out elementIdMaterialId);
                // Get wear coefficient for each element
                Dictionary<int, double> elementIdCoefficient = new Dictionary<int, double>();
                foreach (var entry in elementIdMaterialId)
                {
                    elementIdCoefficient.Add(entry.Key, materialIdCoefficient[entry.Value]);
                }
                // Get wear coefficients for each node
                HashSet<double> allCoefficients;
                Dictionary<int, HashSet<double>> nodeIdAllCoefficients = new Dictionary<int, HashSet<double>>();
                FeElement element;
                foreach (var entry in elementIdCoefficient)
                {
                    element = _mesh.Elements[entry.Key];
                    foreach (var nodeId in element.NodeIds)
                    {
                        if (nodeIdAllCoefficients.TryGetValue(nodeId, out allCoefficients)) allCoefficients.Add(entry.Value);
                        else nodeIdAllCoefficients.Add(nodeId, new HashSet<double>() { entry.Value });
                    }
                }
                // Compute the average
                Dictionary<int, double> nodeIdCoefficient = new Dictionary<int, double>();
                foreach (var entry in nodeIdAllCoefficients)
                {
                    coefficient = 0;
                    foreach (var value in entry.Value) coefficient += value;
                    coefficient /= entry.Value.Count();
                    //
                    nodeIdCoefficient.Add(entry.Key, coefficient);
                }
                //
                return nodeIdCoefficient;
            }
            else return null;
        }
        public void GetSectionThicknessAssignments(out Dictionary<int, int> elementIdSectionThicknessId)
        {
            // Get element section ids
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            // Get thicknesses
            int count = 0;
            double thickness;
            List<int> sectionIds;
            Dictionary<double, List<int>> thicknessSectionIds = new Dictionary<double, List<int>>();
            foreach (var section in assignedSections)
            {
                thickness = GetSectionThickness(section);
                //
                if (thicknessSectionIds.TryGetValue(thickness, out sectionIds)) sectionIds.Add(count);
                else thicknessSectionIds.Add(thickness, new List<int>() { count });
                count++;
            }
            double[] sortedThicknesses = thicknessSectionIds.Keys.ToArray();
            Array.Sort(sortedThicknesses);
            // Get a map of section thicknesses
            Dictionary<int, int> sectionIdSectionThicknessId = new Dictionary<int, int>();
            sectionIdSectionThicknessId.Add(-1, -1);
            if (sortedThicknesses.Length > 0 && sortedThicknesses[0] == -1) count = -1;
            else count = 0;
            foreach (var sortedThickness in sortedThicknesses)
            {
                foreach (var sectionId in thicknessSectionIds[sortedThickness])
                {
                    sectionIdSectionThicknessId.Add(sectionId, count);
                }
                count++;
            }
            // Use the map
            elementIdSectionThicknessId = new Dictionary<int, int>();
            foreach (var entry in elementIdSectionId)
                elementIdSectionThicknessId.Add(entry.Key, sectionIdSectionThicknessId[entry.Value]);
        }
        public double GetSectionThickness(Section section)
        {
            if (_properties.ModelSpace.IsTwoD() && section is SolidSection sos) return sos.Thickness.Value;
            else if (section is ShellSection shs) return shs.Thickness.Value;
            else if (section is MembraneSection ms) return ms.Thickness.Value;
            else return -1;
        }
        public bool TryComputePartMassProperties(BasePart part, out double mass, out double[] centerOfMass,
                                                 out string[] warnings)
        {
            UpdateSectionAssignments();
            //
            mass = 0;
            double[] firstMoment = new double[3];
            List<string> warningList = new List<string>();
            AddElementMassProperties(part.Labels, _elementIdSectionId, _assignedSections, ref mass, firstMoment,
                                     warningList);
            //
            warnings = warningList.ToArray();
            centerOfMass = GetCenterOfMass(mass, firstMoment);
            return mass > 0;
        }
        public bool TryComputeModelMassProperties(out double mass, out double[] centerOfMass, out string[] warnings)
        {
            UpdateSectionAssignments();
            // Only active parts are analysed
            List<int> elementIds = new List<int>();
            foreach (var entry in _mesh.Parts) if (entry.Value.Active) elementIds.AddRange(entry.Value.Labels);
            //
            mass = 0;
            double[] firstMoment = new double[3];
            List<string> warningList = new List<string>();
            AddElementMassProperties(elementIds.ToArray(), _elementIdSectionId, _assignedSections, ref mass, firstMoment,
                                     warningList);
            AddMassSectionMassProperties(ref mass, firstMoment, warningList);
            //
            warnings = warningList.ToArray();
            centerOfMass = GetCenterOfMass(mass, firstMoment);
            return mass > 0;
        }
        private double[] GetCenterOfMass(double mass, double[] firstMoment)
        {
            if (mass <= 0) return null;
            //
            return new double[] { firstMoment[0] / mass, firstMoment[1] / mass, firstMoment[2] / mass };
        }
        private void AddElementMassProperties(int[] elementIds, Dictionary<int, int> elementIdSectionId,
                                              List<Section> assignedSections, ref double mass, double[] firstMoment,
                                              List<string> warnings)
        {
            int sectionId;
            int missingSectionCount = 0;
            int unsupportedElementCount = 0;
            double density;
            double thickness;
            double elementSize;
            double elementMass;
            double[] cg;
            Section section;
            FeElement element;
            bool axisymmetric = _properties.ModelSpace == ModelSpaceEnum.Axisymmetric;
            HashSet<string> offsetSectionNames = new HashSet<string>();
            HashSet<string> missingDensitySectionNames = new HashSet<string>();
            HashSet<string> temperatureDependentMaterialNames = new HashSet<string>();
            // The section values are parsed from equations and units, which is far too slow to repeat for each element
            int numOfSections = assignedSections.Count;
            bool[] sectionHasDensity = new bool[numOfSections];
            bool[] sectionHasOffset = new bool[numOfSections];
            double[] sectionDensity = new double[numOfSections];
            double[] sectionThickness = new double[numOfSections];
            string[] sectionTemperatureDependentMaterialNames = new string[numOfSections];
            for (int i = 0; i < numOfSections; i++)
            {
                sectionHasDensity[i] = TryGetSectionDensity(assignedSections[i], out sectionDensity[i],
                                                            out sectionTemperatureDependentMaterialNames[i]);
                sectionThickness[i] = GetSectionThickness(assignedSections[i]);
                if (assignedSections[i] is ShellSection shs) sectionHasOffset[i] = shs.Offset.Value != 0;
                else if (assignedSections[i] is MembraneSection mes) sectionHasOffset[i] = mes.Offset.Value != 0;
            }
            //
            foreach (int elementId in elementIds)
            {
                if (!elementIdSectionId.TryGetValue(elementId, out sectionId) || sectionId < 0)
                {
                    missingSectionCount++;
                    continue;
                }
                section = assignedSections[sectionId];
                if (!sectionHasDensity[sectionId])
                {
                    missingDensitySectionNames.Add(section.Name);
                    continue;
                }
                if (sectionTemperatureDependentMaterialNames[sectionId] != null)
                    temperatureDependentMaterialNames.Add(sectionTemperatureDependentMaterialNames[sectionId]);
                density = sectionDensity[sectionId];
                //
                element = _mesh.Elements[elementId];
                cg = element.GetCG(_mesh.Nodes, out elementSize);
                //
                if (element is FeElement3D) elementMass = density * elementSize;
                else if (element is FeElement2D)
                {
                    // The axisymmetric quantities are defined for the full revolution - the same as for the pressure loads
                    if (axisymmetric) elementMass = density * elementSize * 2 * Math.PI * cg[0];
                    else
                    {
                        thickness = sectionThickness[sectionId];
                        if (thickness < 0)
                        {
                            unsupportedElementCount++;
                            continue;
                        }
                        elementMass = density * elementSize * thickness;
                        // The offset moves the reference surface which cannot be accounted for on curved parts
                        if (sectionHasOffset[sectionId]) offsetSectionNames.Add(section.Name);
                    }
                }
                else
                {
                    unsupportedElementCount++;
                    continue;
                }
                //
                mass += elementMass;
                // The revolved body has its center of mass on the axis of revolution
                if (axisymmetric) firstMoment[1] += elementMass * cg[1];
                else
                {
                    firstMoment[0] += elementMass * cg[0];
                    firstMoment[1] += elementMass * cg[1];
                    firstMoment[2] += elementMass * cg[2];
                }
            }
            //
            if (missingSectionCount > 0)
                warnings.Add("Warning: " + missingSectionCount + " elements are missing a section assignment and are " +
                             "excluded from the mass.");
            if (unsupportedElementCount > 0)
                warnings.Add("Warning: " + unsupportedElementCount + " elements are of an unsupported type and are " +
                             "excluded from the mass.");
            if (missingDensitySectionNames.Count > 0)
                warnings.Add("Warning: No density is defined for the material of the section(s): " +
                             string.Join(", ", missingDensitySectionNames) + ". The elements are excluded from the mass.");
            if (temperatureDependentMaterialNames.Count > 0)
                warnings.Add("Warning: The first density value is used for the temperature dependent material(s): " +
                             string.Join(", ", temperatureDependentMaterialNames) + ".");
            if (offsetSectionNames.Count > 0)
                warnings.Add("Warning: The offset is ignored in the mass computation of the section(s): " +
                             string.Join(", ", offsetSectionNames) + ".");
        }
        private void AddMassSectionMassProperties(ref double mass, double[] firstMoment, List<string> warnings)
        {
            double nodalMass;
            double[] coor;
            FeNodeSet nodeSet;
            FeSurface surface;
            FeReferencePoint referencePoint;
            Dictionary<int, double> nodalValues;
            HashSet<string> invalidSectionNames = new HashSet<string>();
            //
            foreach (var entry in _sections)
            {
                if (!(entry.Value is MassSection ms) || !ms.Active) continue;
                //
                if (ms is PointMassSection pms)
                {
                    // The mass is added to each node of the region
                    if (pms.RegionType == RegionTypeEnum.NodeSetName &&
                        _mesh.NodeSets.TryGetValue(pms.RegionName, out nodeSet))
                    {
                        foreach (int nodeId in nodeSet.Labels)
                            AddPointMass(pms.Mass.Value, _mesh.Nodes[nodeId].Coor, ref mass, firstMoment);
                    }
                    else if (pms.RegionType == RegionTypeEnum.ReferencePointName &&
                             _mesh.ReferencePoints.TryGetValue(pms.RegionName, out referencePoint))
                    {
                        AddPointMass(pms.Mass.Value, referencePoint.Coor(), ref mass, firstMoment);
                    }
                    else invalidSectionNames.Add(pms.Name);
                }
                else if (ms is DistributedMassSection dms)
                {
                    // The mass is distributed over the region
                    if (dms.RegionType == RegionTypeEnum.SurfaceName &&
                        _mesh.Surfaces.TryGetValue(dms.RegionName, out surface) && surface.NodeSetName != null)
                    {
                        double area = 0;
                        nodalValues = null;
                        // The distribution needs the section thicknesses of the shell edge faces
                        try { GetDistributedNodalValuesFromSurface(surface.Name, out nodalValues, out area); }
                        catch { area = 0; }
                        //
                        if (nodalValues != null && area > 0)
                        {
                            foreach (var nodalEntry in nodalValues)
                            {
                                nodalMass = nodalEntry.Value / area * dms.Mass.Value;
                                coor = _mesh.Nodes[nodalEntry.Key].Coor;
                                AddPointMass(nodalMass, coor, ref mass, firstMoment);
                            }
                        }
                        else invalidSectionNames.Add(dms.Name);
                    }
                    else invalidSectionNames.Add(dms.Name);
                }
                else throw new NotSupportedException();
            }
            //
            if (invalidSectionNames.Count > 0)
                warnings.Add("Warning: The region is not defined for the mass section(s): " +
                             string.Join(", ", invalidSectionNames) + ". They are excluded from the mass.");
        }
        private void AddPointMass(double pointMass, double[] coor, ref double mass, double[] firstMoment)
        {
            mass += pointMass;
            firstMoment[0] += pointMass * coor[0];
            firstMoment[1] += pointMass * coor[1];
            firstMoment[2] += pointMass * coor[2];
        }
        private bool TryGetSectionDensity(Section section, out double density,
                                          out string temperatureDependentMaterialName)
        {
            density = 0;
            temperatureDependentMaterialName = null;
            //
            Material material;
            if (section.MaterialName == null || !_materials.TryGetValue(section.MaterialName, out material)) return false;
            //
            foreach (var property in material.Properties)
            {
                if (property is Density den)
                {
                    if (den.DensityTemp.Length > 1) temperatureDependentMaterialName = material.Name;
                    density = den.DensityTemp[0][0].Value;
                    return true;
                }
                else if (property is ElasticWithDensity ewd)
                {
                    density = ewd.Density.Value;
                    return true;
                }
            }
            return false;
        }
        //
        public void UpdateUserKeywordStates(bool remove, out int suppresedCount, out int unsuppresedCount)
        {
            FileInOut.Output.CalculixFileWriter.UpdateUserKeywordStates(this, remove, out suppresedCount, out unsuppresedCount);
        }
        public void UpdateUserKeywordStates(bool remove, out int suppresedCount, out int unsuppresedCount,
                                            out int removedCount)
        {
            FileInOut.Output.CalculixFileWriter.UpdateUserKeywordStates(this, remove, out suppresedCount,
                                                                        out unsuppresedCount, out removedCount);
        }
        // Import                                                                                   
        public string[] ImportGeometryFromStlFile(string fileName)
        {
            FeMesh mesh = StlFileReader.Read(fileName);
            // Shading
            foreach (var entry in mesh.Parts) entry.Value.SmoothShaded = true;
            //
            string[] addedPartNames = ImportGeometry(mesh, GetReservedPartNames());
            //
            return addedPartNames;
        }        
        public string[] ImportGeometryFromBrepFile(string visFileName, string brepFileName)
        {
            return ImportGeometryFromBrepFile(VisFileReader.Read(visFileName), brepFileName);
        }
        public string[] ImportGeometryFromBrepFile(FeMesh mesh, string brepFileName, bool updateNodeIdElementIds = true)
        {
            if (mesh.Parts.GetValueByIndex(0) is GeometryPart gp)
            {
                gp.CADFileDataFromFile(brepFileName);
                gp.SmoothShaded = true;
            }
            //
            string[] addedPartNames = ImportGeometry(mesh, GetReservedPartNames(), updateNodeIdElementIds);
            //
            return addedPartNames;
        }
        public void ImportMeshFromVolFile(string fileName)
        {
            FeMesh mesh = NetgenVolFileReader.Read(fileName, ElementsToImport.Shell | ElementsToImport.Solid);
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
        }
        public bool ImportMeshFromMmgFile(string fileName,
                                          ElementsToImport elementsToImport = ElementsToImport.All,
                                          bool convertToSecondOrder = false)
        {
            FeMesh mesh = MmgFileReader.Read3D(fileName, elementsToImport, MeshRepresentation.Mesh, convertToSecondOrder);
            //
            bool noErrors = true;
            foreach (var entry in mesh.Parts)
            {
                if (entry.Value is GeometryPart gp && gp.HasErrors)
                {
                    noErrors = false;
                    break;
                }
            }
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
            //
            return noErrors;
        }
        
        public FeMesh PrepareGeneratedMeshFromMeshFile(string fileName, BasePart part, bool convertToSecondOrder,
                                                       bool splitCompoundMesh, bool mergeCompoundParts)
        {
            return PrepareGeneratedMeshFromMeshFile(fileName, part, convertToSecondOrder, splitCompoundMesh,
                                                    mergeCompoundParts, true, out _);
        }
        public FeMesh PrepareGeneratedMeshFromMeshFile(string fileName, BasePart part, bool convertToSecondOrder,
                                                       bool splitCompoundMesh, bool mergeCompoundParts,
                                                       bool mergeCells,
                                                       out Dictionary<int, string> elementIdMaterialName,
                                                       SweepMesh symmetricSweep = null)
        {
            elementIdMaterialName = null;
            ElementsToImport elementsToImport;
            GeometryPart subPart;
            if (part.PartType == PartType.SolidAsShell) elementsToImport = ElementsToImport.Solid;
            else if (part.PartType == PartType.Shell) elementsToImport = ElementsToImport.Shell;
            else if (part.PartType == PartType.Compound)
            {
                subPart = _geometry.Parts[(part as CompoundGeometryPart).SubPartNames[0]] as GeometryPart;
                if (subPart.PartType == PartType.SolidAsShell) elementsToImport = ElementsToImport.Solid;
                else if (subPart.PartType == PartType.Shell) elementsToImport = ElementsToImport.Shell;
                else throw new NotSupportedException();
            }
            else throw new NotSupportedException();
            // Get part names - determine, if one part or multiple parts are needed
            string[] prevPartNames;
            if (part is CompoundGeometryPart cgp)
            {
                if (mergeCompoundParts) prevPartNames = new string[] { part.Name };
                else prevPartNames = cgp.SubPartNames.ToArray();
            }
            else prevPartNames = new string[] { part.Name };
            // Called after meshing in PrePoMax - the parts are sorted by id
            FeMesh mesh;
            if (Path.GetExtension(fileName) == ".vol")
                mesh = NetgenVolFileReader.Read(fileName, elementsToImport, convertToSecondOrder);
            else if (Path.GetExtension(fileName) == ".mesh")
                mesh = MmgFileReader.Read(fileName, elementsToImport, MeshRepresentation.Mesh, convertToSecondOrder);
            else if (Path.GetExtension(fileName) == ".inp")
                mesh = InpFileReader.ReadMesh(fileName, elementsToImport, convertToSecondOrder, _geometry, part);
            else if (Path.GetExtension(fileName) == ".obj")
            {
                mesh = ObjFileReader.Read(fileName, convertToSecondOrder, out elementIdMaterialName);
                mesh.FixShellPartOrientation(_geometry, part);
            }
            else throw new NotSupportedException();
            //
            if (mesh == null)
                throw new CaeException("The generated mesh file could not be read: " + fileName);
            if (mesh.Nodes == null || mesh.Nodes.Count == 0)
                throw new CaeException("The generated mesh file contains no nodes: " + fileName);
            if (mesh.Elements == null || mesh.Elements.Count == 0)
                throw new CaeException("The generated mesh file contains no requested elements: " + fileName);
            if (elementsToImport == ElementsToImport.Shell &&
                !mesh.Elements.Values.Any(element => element is FeElement2D))
                throw new CaeException("The generated mesh file contains no shell elements: " + fileName);
            if (elementsToImport == ElementsToImport.Solid &&
                !mesh.Elements.Values.Any(element => element is FeElement3D))
                throw new CaeException("The generated mesh file contains no solid elements: " + fileName);
            // A symmetric sweep meshed only the cells of one symmetric piece, and the others take their mirror images
            if (symmetricSweep != null && symmetricSweep.HasSymmetry && part is CompoundGeometryPart symmetricCompound)
                CreateMirroredCellMeshes(mesh, symmetricCompound, symmetricSweep);
            // Merge parts if only one part is expected
            string[] partNames = mesh.Parts.Keys.ToArray();
            if (prevPartNames.Length != partNames.Length)
            {
                if (prevPartNames.Length == 1 && partNames.Length > 1)
                {
                    // The pieces of a compound meshed as one part are its sub parts in order, so each takes
                    // the geometry metadata of its sub part first and the merge keeps what it can of it
                    if (part is CompoundGeometryPart mergedCompoundPart &&
                        mergedCompoundPart.SubPartNames.Length == partNames.Length)
                    {
                        for (int i = 0; i < partNames.Length; i++)
                            FillMissingGeometryDataOfMeshPart(mesh, partNames[i], mergedCompoundPart.SubPartNames[i]);
                    }
                    mesh.MergeMeshParts(mesh.Parts.Keys.ToArray(), out _, out _);
                    partNames = mesh.Parts.Keys.ToArray();
                }
                else throw new NotSupportedException();
            }
            // Rename the imported part/s
            BasePart[] importedParts = new BasePart[partNames.Length];
            for (int i = 0; i < partNames.Length; i++)
            {
                importedParts[i] = mesh.Parts[partNames[i]];
                importedParts[i].Name = prevPartNames[i];
            }
            mesh.Parts.Clear();
            for (int i = 0; i < partNames.Length; i++) mesh.Parts.Add(importedParts[i].Name, importedParts[i]);
            // The cells of a split compound are meshed together, so their meshes share the faces between them,
            // and each part the split cut becomes one mesh part again unless its cells are kept apart
            if (part is CompoundGeometryPart splitCompoundPart && splitCompoundPart.CreatedBySplit &&
                !mergeCompoundParts && mergeCells)
                MergeSplitCellMeshParts(mesh, splitCompoundPart);
            // Split compound mesh - after the cells are merged, so only the parts the split cut are separated
            if (splitCompoundMesh) mesh.SplitSolidCompoundMesh();
            // Splitting, merging and revolving the mesh redefines the visualization faces and edges of the
            // affected parts - recognize the types of the parts which lost them
            mesh.DetectFaceTypes(false);
            mesh.DetectEdgeTypes(false);
            //
            return mesh;
        }
        /// <summary>
        /// Completes the mesh of a compound a symmetric sweep meshed one symmetric piece of. The imported parts are the
        /// swept cells, each recognized by the box it fills; each mirror image of them the other cells need is
        /// made by mirroring copies across the planes one at a time, the nodes the images share on the planes are
        /// merged, and every image is fitted to the faces and edges of the cell it stands for. The parts end in the
        /// order of the sub-parts, as an ordinary compound mesh does.
        /// </summary>
        private void CreateMirroredCellMeshes(FeMesh mesh, CompoundGeometryPart compoundPart, SweepMesh sweepMesh)
        {
            Dictionary<int, string> partIdName = new Dictionary<int, string>();
            foreach (var subPartName in compoundPart.SubPartNames) partIdName.Add(_geometry.Parts[subPartName].PartId, subPartName);
            // An item may sweep several compounds; only the rows of this one count
            int[][] rows = sweepMesh.SymmetryPartIds.Where(row => partIdName.ContainsKey(row[0]) &&
                                                                  partIdName.ContainsKey(row[1])).ToArray();
            if (rows.Length == 0) return;
            HashSet<int> mirroredPartIds = new HashSet<int>(rows.Select(row => row[0]));
            string[] masterNames = compoundPart.SubPartNames.Where(name => !mirroredPartIds.Contains(_geometry.Parts[name].PartId))
                                                            .ToArray();
            if (mesh.Parts.Count != masterNames.Length)
                throw new CaeException("The symmetric sweep mesh of the part " + compoundPart.Name + " has " + mesh.Parts.Count +
                                       " cells instead of the " + masterNames.Length + " that are swept.");
            // The imported cells are the swept ones, but gmsh renumbers the volumes left when the mirrored cells are
            // removed, so each is named after the swept cell whose box it fills
            BasePart[] importedParts = mesh.Parts.Values.ToArray();
            string[] importedMasterNames = GetMasterNamesByBox(mesh, importedParts, masterNames);
            if (importedMasterNames == null)
                throw new CaeException("The cells of the symmetric sweep mesh of the part " + compoundPart.Name +
                                       " do not fill the swept cells.");
            mesh.Parts.Clear();
            for (int i = 0; i < importedParts.Length; i++)
            {
                importedParts[i].Name = importedMasterNames[i];
                mesh.Parts.Add(importedMasterNames[i], importedParts[i]);
            }
            // The images, made plane by plane from the images before them, only those some cell needs
            List<Tuple<string, int>> needed = rows.Select(row => new Tuple<string, int>(partIdName[row[1]], row[2])).ToList();
            Dictionary<Tuple<string, int>, string> images = masterNames.ToDictionary(name => new Tuple<string, int>(name, 0),
                                                                                     name => name);
            HashSet<string> reservedNames = new HashSet<string>(_geometry.Parts.Keys.Concat(mesh.Parts.Keys));
            HashSet<int> reservedIds = new HashSet<int>(_geometry.Parts.Values.Select(item => item.PartId)
                                                                         .Concat(mesh.Parts.Values.Select(item => item.PartId)));
            for (int p = 0; p < sweepMesh.SymmetryPlanes.Length; p++)
            {
                int mask = (1 << (p + 1)) - 1;
                List<KeyValuePair<Tuple<string, int>, string>> toMirror = images
                    .Where(entry => needed.Any(image => image.Item1 == entry.Key.Item1 &&
                                                        (image.Item2 & mask) == (entry.Key.Item2 | (1 << p))))
                    .ToList();
                if (toMirror.Count == 0) continue;
                //
                double[] plane = sweepMesh.SymmetryPlanes[p];
                string[] copyNames = mesh.MirrorParts(toMirror.Select(entry => entry.Value).ToArray(),
                                                      new double[] { plane[0], plane[1], plane[2] },
                                                      new double[] { plane[3], plane[4], plane[5] }, true,
                                                      reservedNames, reservedIds);
                for (int i = 0; i < copyNames.Length; i++)
                {
                    images[new Tuple<string, int>(toMirror[i].Key.Item1, toMirror[i].Key.Item2 | (1 << p))] = copyNames[i];
                    reservedNames.Add(copyNames[i]);
                    reservedIds.Add(mesh.Parts[copyNames[i]].PartId);
                }
            }
            // Every image takes the name of the cell it stands for
            List<string> mirroredNames = new List<string>();
            foreach (var row in rows)
            {
                string imageName;
                if (!images.TryGetValue(new Tuple<string, int>(partIdName[row[1]], row[2]), out imageName))
                    throw new CaeException("The mirror image of the part " + partIdName[row[1]] + " for the part " +
                                           partIdName[row[0]] + " could not be made.");
                BasePart image = mesh.Parts[imageName];
                mesh.Parts.Remove(imageName);
                image.Name = partIdName[row[0]];
                mesh.Parts.Add(image.Name, image);
                mirroredNames.Add(image.Name);
            }
            // The images share their nodes on the planes with the cells beside them
            string nodeSetName = mesh.NodeSets.GetNextNumberedKey(Globals.InternalSelectionName + "_" +
                                                                  Globals.MergedCoincidentNodes);
            FeNodeSet nodeSet = new FeNodeSet(nodeSetName, mesh.GetNodeIdsFromParts(mesh.Parts.Keys.ToArray()));
            nodeSet.Internal = true;
            mesh.NodeSets.Add(nodeSetName, nodeSet);
            try
            {
                MergeCoincidentNodes mergeCoincidentNodes = new MergeCoincidentNodes();
                mergeCoincidentNodes.Tolerance = Math.Max(1E-12, 1E-6 * mesh.BoundingBox.GetDiagonal());
                mergeCoincidentNodes.NodesToKeep = NodesToKeepEnum.SmallerID;
                mesh.MergeCoincidentNodes(nodeSetName, mergeCoincidentNodes);
            }
            finally
            {
                mesh.NodeSets.Remove(nodeSetName);
            }
            // Each image has the faces and edges of the cell it stands for
            foreach (var mirroredName in mirroredNames)
                mesh.FitPartToGeometryPart(mesh.Parts[mirroredName], _geometry.Parts[mirroredName], _geometry);
            // The parts in the order of the sub-parts
            BasePart[] orderedParts = compoundPart.SubPartNames.Select(name => mesh.Parts[name]).ToArray();
            mesh.Parts.Clear();
            foreach (var orderedPart in orderedParts) mesh.Parts.Add(orderedPart.Name, orderedPart);
        }
        /// <summary>
        /// The name of the geometry part every mesh part fills, matched by their boxes one to one, or null when a mesh
        /// part fills none of them. The geometry box is taken without the offset an exploded view puts on the part,
        /// since the mesh is generated without it, and from the nodes of the part as the mesh box is: the bounds of the
        /// CAD part are loose round a curved face, and a small cell's reached 2.86 past a side it ends on, more than
        /// the tenth of its size the boxes may differ by.
        /// </summary>
        private string[] GetMasterNamesByBox(FeMesh mesh, BasePart[] meshParts, string[] geometryPartNames)
        {
            double[][] geometryBoxes = geometryPartNames.Select(name =>
            {
                BasePart geometryPart = _geometry.Parts[name];
                double[] offset = geometryPart.Offset != null ? geometryPart.Offset : new double[3];
                double[] box = new double[] { double.MaxValue, double.MaxValue, double.MaxValue,
                                              -double.MaxValue, -double.MaxValue, -double.MaxValue };
                foreach (var nodeId in geometryPart.NodeLabels)
                {
                    double[] coor = _geometry.Nodes[nodeId].Coor;
                    for (int i = 0; i < 3; i++)
                    {
                        box[i] = Math.Min(box[i], coor[i] - offset[i]);
                        box[i + 3] = Math.Max(box[i + 3], coor[i] - offset[i]);
                    }
                }
                return box;
            }).ToArray();
            double[][] meshBoxes = meshParts.Select(meshPart =>
            {
                double[] box = new double[] { double.MaxValue, double.MaxValue, double.MaxValue,
                                              -double.MaxValue, -double.MaxValue, -double.MaxValue };
                foreach (var nodeId in meshPart.NodeLabels)
                {
                    double[] coor = mesh.Nodes[nodeId].Coor;
                    for (int i = 0; i < 3; i++)
                    {
                        box[i] = Math.Min(box[i], coor[i]);
                        box[i + 3] = Math.Max(box[i + 3], coor[i]);
                    }
                }
                return box;
            }).ToArray();
            // The closest pairs first, each part taken once. A coarse mesh cuts the chords of curved faces, so its box
            // may fall short of the part's by the sag of a chord
            List<Tuple<double, int, int>> pairs = new List<Tuple<double, int, int>>();
            for (int i = 0; i < meshBoxes.Length; i++)
            {
                for (int j = 0; j < geometryBoxes.Length; j++)
                {
                    double distance = 0;
                    for (int k = 0; k < 6; k++) distance = Math.Max(distance, Math.Abs(meshBoxes[i][k] - geometryBoxes[j][k]));
                    double size = Math.Sqrt(Enumerable.Range(0, 3).Sum(k => Math.Pow(geometryBoxes[j][k + 3] - geometryBoxes[j][k], 2)));
                    if (distance <= 0.1 * size) pairs.Add(new Tuple<double, int, int>(distance, i, j));
                }
            }
            string[] names = new string[meshParts.Length];
            HashSet<int> takenGeometryIds = new HashSet<int>();
            foreach (var pair in pairs.OrderBy(pair => pair.Item1))
            {
                if (names[pair.Item2] != null || takenGeometryIds.Contains(pair.Item3)) continue;
                names[pair.Item2] = geometryPartNames[pair.Item3];
                takenGeometryIds.Add(pair.Item3);
            }
            return names.Contains(null) ? null : names;
        }
        private void MergeSplitCellMeshParts(FeMesh mesh, CompoundGeometryPart splitCompoundPart)
        {
            foreach (string subPartName in splitCompoundPart.GetSplitSubPartNames())
            {
                string[] cellNames = splitCompoundPart.GetCellNames(subPartName)
                                                      .Where(cellName => mesh.Parts.ContainsKey(cellName)).ToArray();
                if (cellNames.Length == 0) continue;
                //
                // A cell takes the geometry metadata of its geometry part while it still has its name, so the
                // merge can carry it to the part the cells become
                foreach (string cellName in cellNames) FillMissingGeometryDataOfMeshPart(mesh, cellName, cellName);
                //
                BasePart subPartMesh;
                if (cellNames.Length == 1) subPartMesh = mesh.Parts[cellNames[0]];
                else
                {
                    mesh.MergeMeshParts(cellNames, out MeshPart mergedPart, out _);
                    subPartMesh = mergedPart;
                }
                mesh.Parts.Remove(subPartMesh.Name);
                subPartMesh.Name = subPartName;
                mesh.Parts.Add(subPartMesh.Name, subPartMesh);
            }
        }
        private void FillMissingGeometryDataOfMeshPart(FeMesh mesh, string meshPartName, string geometryPartName)
        {
            BasePart meshPart;
            BasePart geometryPart;
            if (mesh.Parts.TryGetValue(meshPartName, out meshPart) &&
                _geometry.Parts.TryGetValue(geometryPartName, out geometryPart))
                FillMissingGeometryData(meshPart, mesh, geometryPart);
        }
        private void FillMissingGeometryData(BasePart meshPart, FeMesh mesh, BasePart geometryPart)
        {
            GeometryMetadata matchedMetadata = GeometryMetadataMatcher.Match(geometryPart, _geometry.Nodes,
                                                                             meshPart, mesh.Nodes);
            meshPart.FillMissingGeometryDataFrom(geometryPart, matchedMetadata);
        }
        public void ImportGeneratedMesh(FeMesh mesh, bool updateNodeIdElementIds = true)
        {
            ImportMesh(mesh, null, false, true, updateNodeIdElementIds);
            // Recolor parts at the end of the import
            foreach (var importedPart in mesh.Parts.Values)
            {
                if (_geometry.Parts.ContainsKey(importedPart.Name) && _mesh.Parts.ContainsKey(importedPart.Name))
                    _mesh.Parts[importedPart.Name].Color = _geometry.Parts[importedPart.Name].Color;
            }
        }
        public static bool DeepCompare(object obj, object another)
        {
            if (ReferenceEquals(obj, another))
                return true;
            if ((obj == null) || (another == null))
                return false;
            //Compare two object's class, return false if they are difference
            if (obj.GetType() != another.GetType())
                return false;

            var result = true;
            //Get all properties of obj
            //And compare each other
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                if (!objValue.Equals(anotherValue))
                    result = false;
            }

            return result;
        }

        public static bool CompareEx(object obj, object another)
        {
            if (ReferenceEquals(obj, another))
                return true;
            if ((obj == null) || (another == null))
                return false;
            if (obj.GetType() != another.GetType())
                return false;

            //properties: int, double, DateTime, etc, not class
            if (!obj.GetType().IsClass) return obj.Equals(another);

            var result = true;
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                //Recursion
                if (!DeepCompare(objValue, anotherValue))
                    result = false;
            }
            return result;
        }
        public void ImportGeneratedRemeshFromMeshFile(string fileName, int[] elementIds, BasePart part,
                                                      bool convertToSecondOrder, Dictionary<(int, int), FeNode> midNodes)
        {
            // Remove elements from the mesh
            HashSet<int> possiblyUnrefNodeIds = new HashSet<int>();
            foreach (var elId in elementIds) possiblyUnrefNodeIds.UnionWith(_mesh.Elements[elId].NodeIds);  // contains midside nodes
            HashSet<int> removedNodeIds =
                _mesh.RemoveElementsByIds(new HashSet<int>(elementIds), possiblyUnrefNodeIds, false, false, true);
            HashSet<int> borderNodeIds = new HashSet<int>(possiblyUnrefNodeIds.Except(removedNodeIds));     // contains midside nodes
            Dictionary<int, FeNode> borderNodes = new Dictionary<int, FeNode>();
            foreach (var ndId in borderNodeIds) borderNodes.Add(ndId, _mesh.Nodes[ndId]);                   // contains midside nodes
            HashSet<int> remainingNodeIds = new HashSet<int>(_mesh.Nodes.Keys.Except(removedNodeIds));
            // Read the mmg file and renumber nodes and elements
            double epsilon = 1E-6;
            double max = part.BoundingBox.GetDiagonal();
            Dictionary<string, Dictionary<int, int>> partNameNewSurfIdOldSurfId = new Dictionary<string, Dictionary<int, int>>();
            Dictionary<string, Dictionary<int, int>> partNameNewEdgeIdOldEdgeId = new Dictionary<string, Dictionary<int, int>>();
            FeMesh mmgMesh = MmgFileReader.Read(fileName, ElementsToImport.Shell,
                                                                MeshRepresentation.Mesh, convertToSecondOrder,
                                                                _mesh.MaxNodeId + 1, _mesh.MaxElementId + 1,
                                                                borderNodes, midNodes,
                                                                epsilon * max,
                                                                partNameNewSurfIdOldSurfId, partNameNewEdgeIdOldEdgeId);
            // Get surface nodes before modification
            Dictionary<int, HashSet<int>> surfaceIdNodeIds = part.Visualization.GetSurfaceIdNodeIds();
            foreach (var entry in surfaceIdNodeIds) entry.Value.ExceptWith(removedNodeIds);
            // Get edge nodes before modification
            Dictionary<int, HashSet<int>> edgeIdNodeIds = part.Visualization.GetEdgeIdNodeIds();
            foreach (var entry in edgeIdNodeIds) entry.Value.ExceptWith(removedNodeIds);
            // Add elements to mesh                                                                                     
            FeElement element;
            HashSet<Type> elementTypes = new HashSet<Type>();
            foreach (var entry in mmgMesh.Elements)
            {
                element = entry.Value;
                element.PartId = part.PartId;
                elementTypes.Add(element.GetType());
                _mesh.Elements.Add(entry.Key, element);
            }
            // Add elements to part
            HashSet<int> newPartElementIds = new HashSet<int>(part.Labels);
            newPartElementIds.UnionWith(mmgMesh.Elements.Keys);
            part.Labels = newPartElementIds.ToArray();
            Array.Sort(part.Labels);
            // Add element types to part
            if (part is MeshPart mp) mp.AddElementTypes(elementTypes.ToArray());
            // Add nodes to mesh                                                                                        
            foreach (var entry in mmgMesh.Nodes)
            {
                if (!_mesh.Nodes.ContainsKey(entry.Key)) _mesh.Nodes.Add(entry.Key, entry.Value);
            }
            // Add nodes to part
            HashSet<int> newPartNodeIds = new HashSet<int>(part.NodeLabels);
            newPartNodeIds.UnionWith(mmgMesh.Nodes.Keys);
            part.NodeLabels = newPartNodeIds.ToArray();
            Array.Sort(part.NodeLabels);
            // Update node ids
            _mesh.UpdateMaxNodeAndElementIds();
            _mesh.UpdateNodeIdElementIds();
            // Model vertices                                                                                           
            HashSet<int> vertexNodeIds = new HashSet<int>();
            // Get vertices from part - only those that were not removed
            foreach (var nodeId in part.Visualization.VertexNodeIds)
            {
                if (!removedNodeIds.Contains(nodeId)) vertexNodeIds.Add(nodeId);
            }
            // Get vertices from mmgPart
            foreach (var entry in mmgMesh.Parts) vertexNodeIds.UnionWith(entry.Value.Visualization.VertexNodeIds);
            // Model edges                                                                                              
            int elementId = mmgMesh.MaxElementId + 1;
            LinearBeamElement beamElement;
            List<FeElement1D> edgeElements = new List<FeElement1D>();
            // Get model edges from part - only edges that are not completely removed
            int[] key;
            CompareIntArray comparer = new CompareIntArray();
            HashSet<int[]> edgeKeys = new HashSet<int[]>(comparer);
            //
            foreach (var edgeCell in part.Visualization.EdgeCells)
            {
                if (remainingNodeIds.Contains(edgeCell[0]) && remainingNodeIds.Contains(edgeCell[1]))
                {
                    key = Tools.GetSortedKey(edgeCell[0], edgeCell[1]);
                    if (!edgeKeys.Contains(key))
                    {
                        beamElement = new LinearBeamElement(elementId++, key);
                        edgeElements.Add(beamElement);
                        edgeKeys.Add(key);
                    }
                }
            }
            // Get model edges from mmgPart
            foreach (var entry in mmgMesh.Parts)
            {
                foreach (var edgeCell in entry.Value.Visualization.EdgeCells)
                {
                    key = Tools.GetSortedKey(edgeCell[0], edgeCell[1]);
                    if (!edgeKeys.Contains(key))
                    {
                        beamElement = new LinearBeamElement(elementId++, key);
                        edgeElements.Add(beamElement);
                        edgeKeys.Add(key);
                    }
                }
            }
            // Add edge elements to mesh
            foreach (var edgeElement in edgeElements) _mesh.Elements.Add(edgeElement.Id, edgeElement);
            // Extract visualization                                                                                    
            _mesh.ExtractShellPartVisualization(part, false, -1);
            //
            _mesh.ConvertLineFeElementsToEdges(vertexNodeIds, false, part.Name);
            // Renumber surfaces                                                                                        
            BasePart mmgPart;
            Dictionary<int, HashSet<int>> itemIdNodeIds;
            foreach (var partNewSurfIdOldSurfId in partNameNewSurfIdOldSurfId)
            {
                mmgPart = mmgMesh.Parts[partNewSurfIdOldSurfId.Key];
                itemIdNodeIds = mmgPart.Visualization.GetSurfaceIdNodeIds();
                //
                foreach (var newSurfIdOldSurfId in partNewSurfIdOldSurfId.Value)
                {
                    surfaceIdNodeIds[newSurfIdOldSurfId.Value].UnionWith(itemIdNodeIds[newSurfIdOldSurfId.Key]);
                    surfaceIdNodeIds[newSurfIdOldSurfId.Value].IntersectWith(part.NodeLabels);
                }
            }
            _mesh.RenumberVisualizationSurfaces(part, surfaceIdNodeIds);
            // Renumber edges                                                                                           
            foreach (var partNewEdgeIdOldEdgeId in partNameNewEdgeIdOldEdgeId)
            {
                if (mmgMesh.Parts.TryGetValue(partNewEdgeIdOldEdgeId.Key, out mmgPart))
                {
                    itemIdNodeIds = mmgPart.Visualization.GetEdgeIdNodeIds();
                    //
                    foreach (var newEdgeIdOldEdgeId in partNewEdgeIdOldEdgeId.Value)
                    {
                        edgeIdNodeIds[newEdgeIdOldEdgeId.Value].UnionWith(itemIdNodeIds[newEdgeIdOldEdgeId.Key]);
                        edgeIdNodeIds[newEdgeIdOldEdgeId.Value].IntersectWith(part.NodeLabels);
                    }
                }
            }
            _mesh.RenumberPartVisualizationEdges(part, edgeIdNodeIds);
            // Recognize the face and the edge types from the mesh
            if (FaceTypeRecognition.AutoDetect) _mesh.DetectPartFaceTypes(part, true);
            if (EdgeTypeRecognition.AutoDetect) _mesh.DetectPartEdgeTypes(part, true);
            //
            _mesh.RemoveElementsByType<FeElement1D>();
            //
            _mesh.ComputeVolumeArea(part);
            //
            UpdateMeshPartsElementTypes(false);
        }
        public List<string> ImportModelFromInpFile(string fileName, Action<string> WriteDataToOutput)
        {
            OrderedDictionary<int[], Calculix.CalculixUserKeyword> indexedUserKeywords;
            InpFileReader.Read(fileName,
                               ElementsToImport.Solid | ElementsToImport.Shell, 
                               this,
                               WriteDataToOutput,
                               out indexedUserKeywords);
            //
            CalculixUserKeywords = indexedUserKeywords;
            //
            return InpFileReader.Errors;
        }
        public List<string> ImportMaterialsFromInpFile(string fileName, Action<string> WriteDataToOutput)
        {
            FeModel model = new FeModel("Imported Materials", _unitSystem, false);
            model.Properties.ModelSpace = _properties.ModelSpace;
            // Add existing model materials to account for indexing of material user keywords
            string existingMaterialName;
            foreach (var entry in _materials)
            {
                existingMaterialName = "_internal_for_indices_" + entry.Key;
                model.Materials.Add(existingMaterialName, entry.Value.DeepClone());
            }
            List<string> existingMaterialNames = model.Materials.Keys.ToList();
            //
            InpFileReader.Read(fileName,
                               ElementsToImport.Solid | ElementsToImport.Shell,
                               model,
                               WriteDataToOutput,
                               out OrderedDictionary<int[], Calculix.CalculixUserKeyword>  indexedUserKeywords);
            //
            CalculixUserKeywords = indexedUserKeywords;
            // Remove existing model materials
            foreach (var materialName in existingMaterialNames) model.Materials.Remove(materialName);
            //
            string name;
            foreach (var entry in model.Materials)
            {
                name = entry.Key;
                if (_materials.ContainsKey(name))
                {
                    name += "_Imported";
                    if (_materials.ContainsKey(name))
                    {
                        name = _materials.GetNextNumberedKey(name);
                    }
                }
                entry.Value.Name = name;
                _materials.Add(name, entry.Value);
            }
            //
            return InpFileReader.Errors;
        }
        public void ImportMeshFromUnvFile(string fileName)
        {
            FeMesh mesh = UnvFileReader.Read(fileName, ElementsToImport.Shell | ElementsToImport.Solid);
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
        }
        public void ImportMeshFromObjFile(string fileName)
        {
            FeMesh mesh = ObjFileReader.Read(fileName);
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
        }
        //
        private string[] ImportGeometry(FeMesh mesh, ICollection<string> reservedPartNames,
                                        bool updateNodeIdElementIds = true)
        {
            Check2D(mesh);
            //
            if (_geometry == null)
            {                
                _geometry = new FeMesh(MeshRepresentation.Geometry);
                mesh.ResetPartsColor();
            }
            //
            string[] addedPartNames = _geometry.AddMesh(mesh, reservedPartNames, GetReservedPartIds(), true, true,
                                                        updateNodeIdElementIds);
            return addedPartNames;
        }
        public void ImportMesh(FeMesh mesh, ICollection<string> reservedPartNames, bool forceRenameParts = true,
                               bool renumberNodesAndElements = true)
        {
            ImportMesh(mesh, reservedPartNames, forceRenameParts, renumberNodesAndElements, true);
        }
        public void ImportMesh(FeMesh mesh, ICollection<string> reservedPartNames, bool forceRenameParts,
                               bool renumberNodesAndElements, bool updateNodeIdElementIds)
        {
            Check2D(mesh);
            //
            if (_mesh == null)
            {
                _mesh = new FeMesh(MeshRepresentation.Mesh);
                mesh.ResetPartsColor();
            }
            _mesh.AddMesh(mesh, reservedPartNames, GetReservedPartIds(), forceRenameParts, renumberNodesAndElements,
                          updateNodeIdElementIds);
        }
        // Update                                                                                   
        public void UpdateGeometryPartMassPropertiesFromVisFile(string visFileName, string partName)
        {
            FeMesh mesh = VisFileReader.Read(visFileName);
            //
            if (mesh.Parts.Count == 1)
            {
                BasePart part = _geometry.Parts[partName];
                BasePart updatedPart = mesh.Parts.First().Value;
                //part.Visualization = updatedPart.Visualization;
                part.MassProperties = updatedPart.MassProperties;
                part.GeometryMetadata = updatedPart.GeometryMetadata?.DeepCopy();
                GeometryMetadataReader.ApplyCanonicalMeasurements(part);
            }
        }
        public void TransferMissingGeometryDataToMesh(BasePart requestedGeometryPart, FeMesh targetMesh)
        {
            if (requestedGeometryPart == null || targetMesh == null || _geometry?.Parts == null) return;
            // A split compound produces mesh parts named after its geometry subparts. A merged compound
            // has no unambiguous face-to-face correspondence and therefore receives no entity metadata.
            foreach (BasePart targetPart in targetMesh.Parts.Values)
            {
                BasePart sourcePart;
                if (!_geometry.Parts.TryGetValue(targetPart.Name, out sourcePart))
                {
                    if (targetPart.Name == requestedGeometryPart.Name) sourcePart = requestedGeometryPart;
                    else continue;
                }
                FillMissingGeometryData(targetPart, targetMesh, sourcePart);
            }
        }
        // Setters                                                                                  
        public void SetMesh(FeMesh mesh)
        {
            _mesh = mesh;
        }
        // Getters                                                                                  
        public string[] GetAllMeshEntityNames()
        {
            if (_mesh != null) return _mesh.GetAllMeshEntityNames();
            return new string[0];
        }
        public HashSet<string> GetReservedPartNames()
        {
            HashSet<string> reservedPartNames = GetPartAndPartGroupNames();
            reservedPartNames.UnionWith(GetAllMeshEntityNames());
            return reservedPartNames;
        }
        public HashSet<string> GetPartAndPartGroupNames(Guid? excludedLinkedGroupId = null)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_geometry?.Parts != null)
            {
                foreach (var entry in _geometry.Parts)
                {
                    if (excludedLinkedGroupId.HasValue &&
                        entry.Value is CompoundGeometryPart compoundPart &&
                        compoundPart.LinkedGroupId == excludedLinkedGroupId.Value)
                        continue;
                    names.Add(entry.Key);
                    if (entry.Value is CompoundGeometryPart splitCompoundPart)
                        names.UnionWith(splitCompoundPart.GetSplitSubPartNames());
                }
                AddOrderedDictionaryGroupNames(_geometry.Parts.GetItemLayoutSnapshot().Nodes, names,
                                               excludedLinkedGroupId);
            }
            if (_mesh?.Parts != null)
            {
                names.UnionWith(_mesh.Parts.Keys);
                AddOrderedDictionaryGroupNames(_mesh.Parts.GetItemLayoutSnapshot().Nodes, names,
                                               excludedLinkedGroupId);
            }
            return names;
        }
        private static void AddOrderedDictionaryGroupNames(
            IEnumerable<OrderedDictionaryItemNode<string>> nodes, HashSet<string> names,
            Guid? excludedLinkedGroupId)
        {
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (!node.IsGroup) continue;
                if (!excludedLinkedGroupId.HasValue || node.Id != excludedLinkedGroupId.Value)
                    names.Add(node.Name);
                AddOrderedDictionaryGroupNames(node.Children, names, excludedLinkedGroupId);
            }
        }
        public HashSet<int> GetReservedPartIds()
        {
            HashSet<int> reservedPartIds = new HashSet<int>();
            if (_geometry != null && _geometry.Parts != null)
            {
                reservedPartIds.UnionWith(_geometry.GetAllPartIds());
                foreach (var entry in _geometry.Parts)
                {
                    if (entry.Value is CompoundGeometryPart compoundPart)
                        reservedPartIds.UnionWith(compoundPart.GetSplitSubPartIds());
                }
            }
            if (_mesh != null && _mesh.Parts != null) reservedPartIds.UnionWith(_mesh.GetAllPartIds());
            if (_heldPartIds != null) reservedPartIds.UnionWith(_heldPartIds);
            return reservedPartIds;
        }
        /// <summary>
        /// Keeps the part ids reserved while the parts that held them are removed and their replacements are
        /// imported, so that the replacements can take them back instead of a part imported in between.
        /// </summary>
        public void HoldPartIds(IEnumerable<int> partIds)
        {
            _heldPartIds = new HashSet<int>(partIds.Where(id => id > 0));
        }
        public void ReleaseHeldPartIds()
        {
            _heldPartIds = null;
        }
        public int GetNumberOfUnsuppresedUserKeywords()
        {
            int count = 0;
            foreach (var entry in _calculixUserKeywords) if (!entry.Value.IsSuppressed) count++;
            return count;
        }
        public int GetNumberOfUnsuppressedKeywordOverrides()
        {
            int count = 0;
            if (_calculixKeywordOverrides != null)
            {
                foreach (var keywordOverride in _calculixKeywordOverrides)
                {
                    if (keywordOverride != null && !keywordOverride.IsSuppressed) count++;
                }
            }
            return count;
        }
        // Mesh setup items
        /// <summary>
        /// Recognizes the most structured mesh every geometry part supports and keeps it on the part. What a
        /// part supports follows from its geometry alone, so the recognition survives until the geometry
        /// changes. Every cell of a compound is recognized alone, without its neighbours, so that a cell shown
        /// as tetrahedral is one that has to be split. Whether the sweeps of the cells fit together is decided
        /// only when the compound is meshed: a compound with a tetrahedral cell is meshed by tetrahedra, and one
        /// whose cells can all be swept but not together fails to mesh.
        /// </summary>
        public void DetectMeshTypes()
        {
            if (_geometry == null || _geometry.Parts == null) return;
            //
            string stamp = GetMeshTypesStamp();
            DetectMeshTypesOfParts();
            _meshTypesStamp = stamp;
        }
        private void DetectMeshTypesOfParts()
        {
            GeometryPart part;
            HashSet<int> solidPartIds = new HashSet<int>();
            foreach (var entry in _geometry.Parts)
            {
                part = entry.Value as GeometryPart;
                if (part == null || part is CompoundGeometryPart) continue;
                // A CAD shell is meshed by the shell gmsh mesher, which lays a structured grid face by face, so
                // which of its faces are structured is told by the faces and not by the part
                if (part.IsCADPart && part.PartType == PartType.Shell)
                {
                    part.MeshType = MeshTypeEnum.Free;
                    continue;
                }
                // An stl part is meshed by the stl meshers: a closed one is filled by tetrahedra and an open one
                // is meshed as a shell without a structured grid
                if (!part.IsCADPart)
                {
                    if (part.PartType == PartType.SolidAsShell) part.MeshType = MeshTypeEnum.Tetrahedral;
                    else if (part.PartType == PartType.Shell) part.MeshType = MeshTypeEnum.Free;
                    else part.MeshType = MeshTypeEnum.Other;
                    continue;
                }
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                {
                    part.MeshType = MeshTypeEnum.Other;
                    continue;
                }
                part.MeshType = IsPartTransfinite(part, false) == null ? MeshTypeEnum.Transfinite
                                                                      : MeshTypeEnum.Tetrahedral;
                solidPartIds.Add(part.PartId);
            }
            if (solidPartIds.Count == 0) return;
            //
            GeometryPart[] tetrahedralParts = solidPartIds.Select(id => (GeometryPart)_geometry.GetPartFromId(id))
                                                          .Where(solidPart => solidPart.MeshType == MeshTypeEnum.Tetrahedral)
                                                          .ToArray();
            Parallel.ForEach(tetrahedralParts, tetrahedralPart =>
            {
                SweepDetectionData detection = GetCellSweepDetection(tetrahedralPart);
                if (detection.Error == null)
                    tetrahedralPart.MeshType = detection.IsRevolved ? MeshTypeEnum.Revolve : MeshTypeEnum.Sweep;
            });
        }
        /// <summary>
        /// What the automatic detection finds for one part alone - a single part, or one cell of a compound taken
        /// without its neighbours - detected the first time it is asked for and kept on the part until its
        /// geometry changes. For a single part it is the detection its sweep uses.
        /// </summary>
        private SweepDetectionData GetCellSweepDetection(GeometryPart part)
        {
            GeometryPart[] cell = new GeometryPart[] { part };
            string stamp = SweepDetectionData.GetStamp(cell);
            //
            SweepDetectionData detection = part.SweepDetection;
            if (detection != null && detection.Stamp == stamp) return detection;
            //
            detection = DetectSweepSources(cell, stamp);
            part.SweepDetection = detection;
            //
            return detection;
        }
        /// <summary>
        /// Whether the mesh types kept on the parts were recognized from the geometry the parts have now. An
        /// exploded view only moves the parts and leaves the recognition standing.
        /// </summary>
        public bool AreMeshTypesDetected()
        {
            if (_geometry == null || _geometry.Parts == null) return true;
            if (_meshTypesStamp == null) return false;
            //
            foreach (var entry in _geometry.Parts)
            {
                if (entry.Value is GeometryPart part && !(part is CompoundGeometryPart) &&
                    part.MeshType == MeshTypeEnum.Unknown) return false;
            }
            return _meshTypesStamp == GetMeshTypesStamp();
        }
        private string GetMeshTypesStamp()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in _geometry.Parts)
            {
                sb.Append(entry.Key);
                sb.Append('|');
                sb.Append(entry.Value.PartType);
                if (entry.Value is GeometryPart part) sb.Append(part.IsCADPart);
                // The box of a compound follows its sub-parts when an exploded view moves them, while the
                // compound keeps no offset of its own, so a compound is stamped by its sub-parts alone
                if (entry.Value is CompoundGeometryPart compoundPart)
                    sb.Append(string.Join("|", compoundPart.SubPartNames));
                else sb.Append(SweepDetectionData.GetStamp(new BasePart[] { entry.Value }));
            }
            return sb.ToString();
        }
        /// <summary>
        /// Why a solid part - a cell of a compound taken alone - cannot be swept, or null when it can be or when the
        /// sweep was never tried on it.
        /// </summary>
        public string GetMeshTypeReason(GeometryPart part)
        {
            if (part == null || part.SweepDetection == null) return null;
            //
            return part.SweepDetection.Error;
        }
        public string IsMeshSetupItemProperlyDefined(MeshSetupItem meshSetupItem)
        {
            if (meshSetupItem is MeshingParameters) return null;
            if (meshSetupItem is LocalMeshSize) return null;
            //
            string error;
            if (meshSetupItem is ShellMmg smmg) error = IsShellMmgProperlyDefined(smmg);
            else if (meshSetupItem is ShellGmsh sg) error = IsShellGmshProperlyDefined(sg);
            else if (meshSetupItem is ShellQuadWild sqw) error = IsShellQuadWildProperlyDefined(sqw);
            else if (meshSetupItem is ThickenShellMesh tsm) error = IsThickenShellMeshProperlyDefined(tsm);
            else if (meshSetupItem is NetgenMesh nm) error = IsNetgenMeshProperlyDefined(nm);
            else if (meshSetupItem is TetrahedralGmsh tg) error = IsTetrahedralGmshProperlyDefined(tg);
            else if (meshSetupItem is TransfiniteMesh tm) error = IsTransfiniteMeshProperlyDefined(tm);
            else if (meshSetupItem is SweepMesh sm) error = IsSweepMeshProperlyDefined(sm);
            else throw new NotSupportedException("MeshSetupItemTypeException");
            if (error != null) return error;
            // A sweep has found its blocks by now, so the coverage asked for here is the coverage the
            // mesher will use to decide which item meshes which part
            return IsMeshSetupItemCoveringAPart(meshSetupItem);
        }
        /// <summary>
        /// Whether a mesh setup item covers a part - whether it is the item that meshes it. A sweep mesh is
        /// defined by source surfaces lying on a single sub-part of a compound part and derives the other
        /// sub-volumes from them, so it covers a part as soon as its selection lies on that part - or names
        /// the compound part itself, when the source surfaces are found automatically. One item may also span
        /// parts that stand apart from each other, and then it covers each of them in turn. Every other item
        /// has to be selected on the whole part.
        /// </summary>
        public bool DoesMeshSetupItemCoverPart(MeshSetupItem meshSetupItem, HashSet<int> selectedPartIds,
                                               int[] meshablePartIds)
        {
            if (meshSetupItem is SweepMesh sweepMesh)
            {
                // The item sweeps the parts its blocks lie on and their mirrored images. A part the
                // selection reached but the sweep left out is meshed by an item of its own
                if (sweepMesh.BlockSourceSurfaceIds != null && sweepMesh.BlockSourceSurfaceIds.Length > 0)
                {
                    HashSet<int> blockPartIds = new HashSet<int>();
                    foreach (var blockSourceSurfaceIds in sweepMesh.BlockSourceSurfaceIds)
                    {
                        foreach (var gmshTopologyId in blockSourceSurfaceIds)
                            blockPartIds.Add(FeMesh.GetPartIdFromGmshTopologyId(gmshTopologyId));
                    }
                    if (sweepMesh.SymmetryPartIds != null)
                    {
                        foreach (var row in sweepMesh.SymmetryPartIds)
                        {
                            if (blockPartIds.Contains(row[1])) blockPartIds.Add(row[0]);
                        }
                    }
                    return blockPartIds.Overlaps(meshablePartIds);
                }
                //
                HashSet<int> sweptPartIds = new HashSet<int>();
                foreach (var partId in selectedPartIds)
                {
                    if (_geometry.GetPartFromId(partId) is CompoundGeometryPart compoundPart)
                        sweptPartIds.UnionWith(_geometry.GetPartIdsFomPartNames(compoundPart.SubPartNames));
                    else sweptPartIds.Add(partId);
                }
                return sweptPartIds.Overlaps(meshablePartIds);
            }
            else return selectedPartIds.Intersect(meshablePartIds).Count() == meshablePartIds.Length;
        }
        /// <summary>
        /// Whether the item covers any part at all, asked by the same rule that decides which item meshes a
        /// part. An item covering none of them is applied to nothing, and is refused rather than recorded and
        /// left in the model tree looking active. What it catches is an item defined on some of the sub-parts
        /// of a compound and not the others, since a compound is meshed as a whole. A sweep passes the same
        /// rule rather than an exception to it, covering a compound as soon as it reaches one sub-part.
        /// </summary>
        private string IsMeshSetupItemCoveringAPart(MeshSetupItem meshSetupItem)
        {
            if (meshSetupItem.CreationIds == null || meshSetupItem.CreationIds.Length == 0) return null;
            //
            GeometryPart part;
            int[] meshablePartIds;
            HashSet<int> selectedPartIds = new HashSet<int>(FeMesh.GetPartIdsFromGeometryIds(meshSetupItem.CreationIds));
            foreach (var entry in _geometry.Parts)
            {
                part = entry.Value as GeometryPart;
                if (part == null || GetCompoundPartOfSubPart(part.Name) != null) continue;
                //
                meshablePartIds = _geometry.GetPartIdsFomPartNames(
                                      _geometry.GetMeshablePartNames(new string[] { part.Name }));
                if (DoesMeshSetupItemCoverPart(meshSetupItem, selectedPartIds, meshablePartIds)) return null;
            }
            return "The mesh setup item covers no part. A compound part is meshed as a whole, so an item " +
                   "defined on some of its sub-parts and not the others meshes nothing: define it on all of " +
                   "them, or on none of them.";
        }
        private string IsShellMmgProperlyDefined(ShellMmg shellMmg)
        {
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(shellMmg.CreationIds);
            foreach (int partId in partIds)
            {
                GeometryPart part = _geometry.GetPartFromId(partId) as GeometryPart;
                if (part == null || part.PartType != PartType.Shell || part.IsCADPart)
                    return "The shell MMG mesh setup item can only be defined on STL shell parts.";
                if (part.HasErrors || part.Visualization.IsNonManifold())
                    return "The shell MMG mesh setup item can only be defined on manifold shell parts. " +
                           "Use the Netgen or Gmsh shell mesher for the part " + part.Name + ".";
            }
            return null;
        }
        private string IsShellGmshProperlyDefined(ShellGmsh shellGmsh)
        {
            BasePart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(shellGmsh.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = _geometry.GetPartFromId(partId);
                //
                if (part.PartType != PartType.Shell)
                    return "The shell gmsh setup item can only be defined on shell parts.";
            }
            return null;
        }
        private string IsShellQuadWildProperlyDefined(ShellQuadWild shellQuadWild)
        {
            if (shellQuadWild.TriangulationMesher != MesherTypeEnum.Gmsh &&
                shellQuadWild.TriangulationMesher != MesherTypeEnum.Netgen &&
                shellQuadWild.TriangulationMesher != MesherTypeEnum.Mmg)
                return "The QuadWild triangulation mesher must be Gmsh, Netgen, or MMG.";
            if (Double.IsNaN(shellQuadWild.Alpha) || Double.IsInfinity(shellQuadWild.Alpha) ||
                shellQuadWild.Alpha < 0 || shellQuadWild.Alpha > 1)
                return "The QuadWild alpha value must be a finite number from 0 to 1.";
            //
            GeometryPart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(shellQuadWild.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = (GeometryPart)_geometry.GetPartFromId(partId);
                //
                if (part.PartType != PartType.Shell)
                    return "The shell QuadWild mesh setup item can only be defined on shell parts.";
                if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Mmg &&
                    (part.HasErrors || part.Visualization.IsNonManifold()))
                    return "The QuadWild MMG triangulation can only be used on manifold shell parts. " +
                           "Use the Netgen or Gmsh triangulation for the part " + part.Name + ".";
            }
            return null;
        }
        private string IsThickenShellMeshProperlyDefined(ThickenShellMesh thickenShellMesh)
        {
            HashSet<string> partNames = new HashSet<string>(_mesh.GetPartNamesFromGeometryIds(thickenShellMesh.CreationIds));
            partNames.UnionWith(_geometry.GetPartNamesFromGeometryIds(thickenShellMesh.CreationIds));
            //
            if (partNames != null && partNames.Count > 0)
            {
                BasePart part;
                string orientationError;
                foreach (var partName in partNames)
                {
                    _geometry.Parts.TryGetValue(partName, out part);
                    // Is mesh setup item defined on the mesh part
                    if (part == null) _mesh.Parts.TryGetValue(partName, out part);
                    if (part == null)
                        return "The part " + partName + " cannot be found neither in geometry parts neither in model parts.";
                    //
                    if (part.PartType != PartType.Shell)
                        return "The thicken shell feature can only be used on shell parts.";
                    if (part.Visualization.IsNonManifold())
                        return "The thicken shell feature can only be used on manifold shell parts.";
                    //
                    orientationError = FeMesh.GetThickenShellMeshOrientationError(part);
                    if (orientationError != null) return orientationError;
                }
            }
            else return "No parts are defined for the thicken shell mesh setup item.";
            //
            return null;
        }
        private string IsNetgenMeshProperlyDefined(NetgenMesh netgenMesh)
        {
            BasePart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(netgenMesh.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = _geometry.GetPartFromId(partId);
                //
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell &&
                    part.PartType != PartType.Shell)
                    return "The Netgen mesh setup item can only be defined on solid and shell parts.";
            }
            return null;
        }
        private string IsTetrahedralGmshProperlyDefined(TetrahedralGmsh tetrahedralGmsh)
        {
            BasePart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(tetrahedralGmsh.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = _geometry.GetPartFromId(partId);
                //
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    return "The tetrahedral gmsh setup item can only be defined on solid parts.";
            }
            return null;
        }
        private string IsTransfiniteMeshProperlyDefined(TransfiniteMesh transfiniteMesh)
        {
            string error;
            GeometryPart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(transfiniteMesh.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = (GeometryPart)_geometry.GetPartFromId(partId);
                //
                error = IsPartTransfinite(part, transfiniteMesh.AllowPyramidElements);
                if (error != null) return error;
            }
            return null;
        }
        /// <summary>
        /// Whether one part can be meshed by the transfinite algorithm, or why it cannot. Without pyramid
        /// elements the part has to be a five or six sided solid whose faces are 3-sided and 4-sided, which
        /// is the shape gmsh can lay a structured grid over. A face is counted by the number of edges it
        /// carries, which a face with a hole in it passes although gmsh cannot lay a grid over it: the end
        /// face of a hollow cylinder whose walls are split in two carries four edges in two loops, and gmsh
        /// meshes it with the unstructured algorithm and then the volume with tetrahedrons.
        /// </summary>
        private string IsPartTransfinite(GeometryPart part, bool allowPyramidElements)
        {
            if (!part.IsCADPart)
                return "The transfinite gmsh setup item cannot be defined on a stl based part.";
            if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                return "The transfinite gmsh setup item can only be defined on solid parts.";
            //
            if (allowPyramidElements) return null;
            //
            if (part.Visualization.FaceCount != 5 && part.Visualization.FaceCount != 6)
                return part.Name + ": the transfinite gmsh setup item can only be defined on solid parts " +
                       "with 5 or 6 faces.";
            //
            int num3sided = 0;
            int num4sided = 0;
            HashSet<int> triSurfaceEdgeIds = new HashSet<int>();
            for (int i = 0; i < part.Visualization.FaceCount; i++)
            {
                if (part.Visualization.GetFreeEdgeLoops(new int[] { i }).Length != 1)
                    return part.Name + ": the transfinite gmsh setup item can only be defined on solid parts " +
                           "with faces bounded by a single edge loop. Split the faces with a hole in them.";
                //
                if (part.Visualization.FaceEdgeIds[i].Length == 3)
                {
                    num3sided++;
                    triSurfaceEdgeIds.UnionWith(part.Visualization.FaceEdgeIds[i]);
                }
                else if (part.Visualization.FaceEdgeIds[i].Length == 4) num4sided++;
            }
            if (!(num4sided == 6 || (num4sided == 3 && num3sided == 2)))
                return part.Name + ": the transfinite gmsh setup item can only be defined on solid parts " +
                       "with 3-sided and 4-sided faces.";
            if (num4sided == 3 && num3sided == 2 && triSurfaceEdgeIds.Count != 6)
                return part.Name + ": the transfinite gmsh setup item can only be defined on solid parts " +
                       "with non touching 3-sided faces.";
            //
            return null;
        }
        private string IsSweepMeshProperlyDefined(SweepMesh sweepMesh)
        {
            string error;
            sweepMesh.BlockSourceSurfaceIds = null;
            sweepMesh.BlockSideSurfaceIds = null;
            sweepMesh.BlockTargetSurfaceIds = null;
            sweepMesh.LayerGroupEdgeIds = null;
            sweepMesh.BlockArrowStarts = null;
            sweepMesh.BlockArrowDirections = null;
            sweepMesh.BlockRevolveAxes = null;
            sweepMesh.SymmetryPlanes = null;
            sweepMesh.SymmetryPartIds = null;
            sweepMesh.SymmetryLocalMeshSizeNames = null;
            sweepMesh.SymmetryLocalMeshSizeIds = null;
            sweepMesh.SymmetryNotUsedReasons = null;
            sweepMesh.SymmetryNotUsedPartIds = null;
            sweepMesh.UnsweptCellReasons = null;
            sweepMesh.UnsweptCellPartIds = null;
            //
            if (sweepMesh.ElementSizeType == CaeMesh.ElementSizeTypeEnum.MultiLayerd &&
                (sweepMesh.LayerSizes.Length != sweepMesh.NumOfElementsPerLayer.Length))
                return "The number of layers must be equal for layer sizes and number of elements per layer.";
            // The selection: the source surfaces, grouped by the part they lie on, or the parts whose source
            // surfaces are found automatically
            int partId;
            bool partsSelected = false;
            HashSet<int> surfaceIds;
            HashSet<int> selectedPartIds = new HashSet<int>();
            Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < sweepMesh.CreationIds.Length; i++)
            {
                partId = FeMesh.GetPartIdFromGeometryId(sweepMesh.CreationIds[i]);
                selectedPartIds.Add(partId);
                if (FeMesh.IsPartGeometryId(sweepMesh.CreationIds[i]))
                {
                    partsSelected = true;
                    continue;
                }
                if (!partIdSelectedSurfaceIds.TryGetValue(partId, out surfaceIds))
                {
                    surfaceIds = new HashSet<int>();
                    partIdSelectedSurfaceIds.Add(partId, surfaceIds);
                }
                surfaceIds.Add(FeMesh.GetItemIdFromGeometryId(sweepMesh.CreationIds[i]));
            }
            // One solid CAD part, or the sub-parts of one compound part, each of them swept as a block of its own.
            // Parts standing apart from each other form groups that are swept independently
            GeometryPart[][] sweptPartGroups;
            if (sweepMesh.SourceType == SweepSourceTypeEnum.Automatic)
            {
                // The dialog and the API select parts for an automatic sweep; the surfaces a revolve item of an older
                // file selected name the parts they lie on
                error = GetSweptPartGroups(selectedPartIds, out sweptPartGroups);
                if (error == null) error = CreateSweepBlocksForGroups(sweepMesh, sweptPartGroups, null);
            }
            else
            {
                if (partsSelected) return "Select the source surfaces of the sweep, or let them be found automatically.";
                error = GetSweptPartGroups(selectedPartIds, out sweptPartGroups);
                if (error == null) error = CreateSweepBlocksForGroups(sweepMesh, sweptPartGroups, partIdSelectedSurfaceIds);
            }
            //
            return error == null ? null : error + " The swept mesh cannot be created.";
        }
        /// <summary>
        /// Collects the selected parts into the groups that are swept independently of each other: one solid
        /// CAD part, or one compound part of solids, which the selection may name by its own part id or by
        /// any of its sub-parts.
        /// </summary>
        private string GetSweptPartGroups(IEnumerable<int> selectedPartIds, out GeometryPart[][] sweptPartGroups)
        {
            sweptPartGroups = null;
            bool sweepable;
            string error = null;
            GeometryPart part;
            GeometryPart[] subParts;
            CompoundGeometryPart compoundPart;
            HashSet<string> groupNames = new HashSet<string>();
            List<GeometryPart[]> groups = new List<GeometryPart[]>();
            // A selection made by dragging a box reaches whatever stands in it, so a part that cannot be
            // swept is left out and meshed by an item of its own, rather than refusing the parts that can be
            // swept beside it. Only a selection in which nothing can be swept is refused, with the reason
            foreach (var partId in selectedPartIds)
            {
                part = _geometry.GetPartFromId(partId) as GeometryPart;
                if (part == null)
                {
                    error = "The selection does not lie on a geometry part.";
                    continue;
                }
                // A compound part stands for its sub-parts, which are swept together, so the selection may
                // name the compound, or span several of its sub-parts
                compoundPart = part as CompoundGeometryPart;
                if (compoundPart == null) compoundPart = GetCompoundPartOfSubPart(part.Name);
                //
                if (compoundPart != null)
                {
                    if (!groupNames.Add(compoundPart.Name)) continue;
                    //
                    sweepable = true;
                    subParts = new GeometryPart[compoundPart.SubPartNames.Length];
                    for (int i = 0; i < subParts.Length; i++)
                    {
                        subParts[i] = (GeometryPart)_geometry.Parts[compoundPart.SubPartNames[i]];
                        if (!subParts[i].IsCADPart || (subParts[i].PartType != PartType.Solid &&
                                                       subParts[i].PartType != PartType.SolidAsShell))
                        {
                            error = "The sweep mesh setup item can only be defined on a compound part of CAD solids.";
                            sweepable = false;
                            break;
                        }
                    }
                    if (sweepable) groups.Add(subParts);
                }
                else
                {
                    if (!part.IsCADPart)
                    {
                        error = "The sweep mesh setup item cannot be defined on a stl based part.";
                        continue;
                    }
                    if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    {
                        error = "The sweep mesh setup item cannot be defined on a shell part.";
                        continue;
                    }
                    if (!groupNames.Add(part.Name)) continue;
                    groups.Add(new GeometryPart[] { part });
                }
            }
            if (groups.Count == 0) return error != null ? error : "No source surface is selected.";
            //
            sweptPartGroups = groups.ToArray();
            return null;
        }
        /// <summary>
        /// Sweeps every group of parts on its own and keeps the blocks of all the groups on the item. Every
        /// id of a block carries the part it lies on, so the arrays of the groups are simply concatenated.
        ///
        /// The cells of a compound that cannot be swept are left to tetrahedra rather than refusing the compound:
        /// a cell the sweep of the others does not reach is left out of it, and a compound that cannot be swept
        /// with all its cells is swept without the ones that cannot be swept even on their own. The mesher meshes
        /// those cells by tetrahedra, with a pyramid on every quadrilateral a swept block leaves on the face they
        /// share. A compound all of whose cells are swept is swept exactly as before.
        /// </summary>
        private string CreateSweepBlocksForGroups(SweepMesh sweepMesh, GeometryPart[][] sweptPartGroups,
                                                  Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds)
        {
            string error;
            string errorPartName;
            string lastError = null;
            bool namePart;
            bool isCompound;
            SweepBlocks blocks;
            SweepSymmetry symmetry;
            List<double[]> symmetryPlanes = new List<double[]>();
            List<int[]> symmetryPartIds = new List<int[]>();
            Dictionary<string, HashSet<int>> symmetryLocalMeshSizeIds = new Dictionary<string, HashSet<int>>();
            string symmetryNotUsedReason;
            List<string> symmetryNotUsedReasons = new List<string>();
            List<int> symmetryNotUsedPartIds = new List<int>();
            List<string> unsweptCellReasons = new List<string>();
            List<int> unsweptCellPartIds = new List<int>();
            List<SweepBlocks> sweptGroupBlocks = new List<SweepBlocks>();
            foreach (var sweptParts in sweptPartGroups)
            {
                namePart = sweptPartGroups.Length > 1 || sweptParts.Length > 1;
                isCompound = sweptParts.Length > 1;
                // A compound whose cells are mirror images across the planes it is split on sweeps one symmetric
                // piece of its cells, and the mesh is mirrored onto the others. The piece is swept on its own, the
                // faces on the planes being its outer faces, which lets two blocks meet on a plane: each is the
                // mirror image of the other, so their nodes on it coincide. A cell of the piece the sweep does not
                // reach is meshed by tetrahedra and mirrored as a swept one is, so the images of a cell are meshed
                // alike whichever way it is meshed. A piece that cannot be swept on its own leaves the compound to be
                // swept whole, as without the symmetry
                symmetry = null;
                symmetryNotUsedReason = null;
                blocks = null;
                error = null;
                errorPartName = null;
                if (sweepMesh.Symmetry == SweepSymmetryEnum.Automatic && isCompound)
                {
                    symmetry = FindSweepSymmetry(sweepMesh, sweptParts, partIdSelectedSurfaceIds,
                                                 out symmetryNotUsedReason);
                    if (symmetry != null)
                    {
                        blocks = CreateSweepBlocksForCells(symmetry.MasterParts, partIdSelectedSurfaceIds, out error,
                                                           out errorPartName);
                        if (error != null)
                        {
                            symmetry = null;
                            symmetryNotUsedReason = "its symmetric piece cannot be swept on its own: " +
                                                    (errorPartName != null ? "Part " + errorPartName + ": " : "") +
                                                    error;
                        }
                    }
                }
                if (symmetry == null)
                {
                    if (isCompound) blocks = CreateSweepBlocksForCells(sweptParts, partIdSelectedSurfaceIds, out error,
                                                                       out errorPartName);
                    else blocks = CreateSweepBlocksForGroup(sweptParts, partIdSelectedSurfaceIds, false, out error,
                                                            out errorPartName);
                }
                // A group that cannot be swept is left out the way a part that cannot be swept is, so that
                // the groups that can be swept still are. Only an item that sweeps nothing at all is refused
                if (error != null)
                {
                    lastError = namePart && errorPartName != null ? "Part " + errorPartName + ": " + error : error;
                    continue;
                }
                sweptGroupBlocks.Add(blocks);
                if (symmetryNotUsedReason != null)
                {
                    symmetryNotUsedReasons.Add("the mirror symmetry of " + GetCompoundName(sweptParts) +
                                               " is not used: " + symmetryNotUsedReason.TrimEnd('.') + ".");
                    symmetryNotUsedPartIds.Add(sweptParts[0].PartId);
                }
                // The planes of every group in one table, the bits of a group shifted past the planes before it
                if (symmetry != null)
                {
                    int offset = symmetryPlanes.Count;
                    symmetryPlanes.AddRange(symmetry.Planes);
                    foreach (var row in symmetry.PartIds)
                        symmetryPartIds.Add(new int[] { row[0], row[1], row[2] << offset });
                    // A local mesh size selecting only some of the images of an entity is given to all of them
                    foreach (var entry in symmetry.LocalMeshSizeSelections)
                    {
                        if (!symmetryLocalMeshSizeIds.ContainsKey(entry.Key))
                            symmetryLocalMeshSizeIds.Add(entry.Key, new HashSet<int>());
                        symmetryLocalMeshSizeIds[entry.Key].UnionWith(entry.Value);
                    }
                }
                // A cell of the group the sweep leaves to tetrahedra, with why, for the output window
                AddUnsweptCellReasons(sweptParts, blocks, symmetry, unsweptCellReasons, unsweptCellPartIds);
            }
            if (sweptGroupBlocks.Count == 0) return lastError;
            //
            SweepBlocks.Merge(sweptGroupBlocks).CopyToSweepMesh(sweepMesh);
            if (symmetryNotUsedReasons.Count > 0)
            {
                sweepMesh.SymmetryNotUsedReasons = symmetryNotUsedReasons.ToArray();
                sweepMesh.SymmetryNotUsedPartIds = symmetryNotUsedPartIds.ToArray();
            }
            if (symmetryPlanes.Count > 0)
            {
                sweepMesh.SymmetryPlanes = symmetryPlanes.ToArray();
                sweepMesh.SymmetryPartIds = symmetryPartIds.ToArray();
                sweepMesh.SymmetryLocalMeshSizeNames = symmetryLocalMeshSizeIds.Keys.ToArray();
                sweepMesh.SymmetryLocalMeshSizeIds = symmetryLocalMeshSizeIds.Values.Select(ids => ids.OrderBy(id => id)
                                                                                                 .ToArray()).ToArray();
            }
            if (unsweptCellReasons.Count > 0)
            {
                sweepMesh.UnsweptCellReasons = unsweptCellReasons.ToArray();
                sweepMesh.UnsweptCellPartIds = unsweptCellPartIds.ToArray();
            }
            //
            return null;
        }
        /// <summary>
        /// The cells of a group the sweep leaves to tetrahedra, each with why: a cell that can be swept on its own
        /// is kept from it by the others, which the detection of the group tells, and a cell that cannot be swept
        /// at all says so itself. A cell meshed as the mirror image of a swept cell is swept.
        /// </summary>
        private void AddUnsweptCellReasons(GeometryPart[] cells, SweepBlocks blocks, SweepSymmetry symmetry,
                                           List<string> reasons, List<int> partIds)
        {
            if (cells.Length < 2) return;
            //
            HashSet<int> sweptPartIds = new HashSet<int>();
            foreach (var blockSourceSurfaceIds in blocks.SourceSurfaceIds)
            {
                foreach (var gmshTopologyId in blockSourceSurfaceIds)
                    sweptPartIds.Add(FeMesh.GetPartIdFromGmshTopologyId(gmshTopologyId));
            }
            if (symmetry != null)
            {
                foreach (var row in symmetry.PartIds)
                {
                    if (sweptPartIds.Contains(row[1])) sweptPartIds.Add(row[0]);
                }
            }
            if (cells.All(cell => sweptPartIds.Contains(cell.PartId))) return;
            //
            Dictionary<int, string> groupReasons = GetSweepDetection(cells).UnreachedPartReasons;
            foreach (var cell in cells)
            {
                if (sweptPartIds.Contains(cell.PartId)) continue;
                //
                string reason;
                if (!groupReasons.TryGetValue(cell.PartId, out reason)) reason = GetCellSweepDetection(cell).Error;
                if (reason == null) continue;
                //
                reasons.Add("the part " + cell.Name + " is meshed by tetrahedra: " + reason);
                partIds.Add(cell.PartId);
            }
        }
        /// <summary>
        /// The blocks of cells of one compound - all its cells, or the symmetric piece of them - leaving the cells the
        /// sweep does not reach to tetrahedra. Cells that cannot all be swept together are swept once more without
        /// the ones that cannot be swept even on their own, which then stand in the way of none of the others.
        ///
        /// A revolved cell sets the direction of the cells it touches: a cell that can be swept and revolved alike,
        /// whose sweep meets a revolved cell in a way the revolved block cannot take, is revolved about the same axis
        /// instead, and the cells it touches in turn follow it, so that a stack of cells about one axis is revolved
        /// as a whole rather than left to tetrahedra.
        /// </summary>
        private SweepBlocks CreateSweepBlocksForCells(GeometryPart[] cells,
                                                      Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                                      out string error, out string errorPartName)
        {
            SweepBlocks blocks = CreatePrismBlocksForCells(cells, partIdSelectedSurfaceIds, out error, out errorPartName);
            SweepBlocks revolvedBlocks;
            Dictionary<int, SweepBlocks> followerBlocks = new Dictionary<int, SweepBlocks>();
            while (true)
            {
                SweepBlocks sweptBlocks = null;
                if (error == null)
                {
                    sweptBlocks = GetSweepBlocksWithoutParts(blocks, new HashSet<int>(followerBlocks.Keys));
                    sweptBlocks = GetSweptBlocksTurnedToRevolvedCells(cells, sweptBlocks, followerBlocks);
                }
                Dictionary<int, double[]> blockingPartIdAxis = new Dictionary<int, double[]>();
                revolvedBlocks = CreateRevolvedBlocksForCells(cells, sweptBlocks, followerBlocks, blockingPartIdAxis);
                if (error == null) blocks = sweptBlocks;
                // A swept cell standing in the way of a revolved one follows it when it is revolved about the same axis
                bool followed = false;
                foreach (var entry in blockingPartIdAxis)
                {
                    GeometryPart cell = cells.First(part => part.PartId == entry.Key);
                    SweepBlocks cellBlocks = CreateRevolvedBlocks(cell, out _);
                    if (cellBlocks == null || !AreRevolveAxesEqual(entry.Value, cellBlocks.RevolveAxes[0], cell, false))
                        continue;
                    followerBlocks.Add(entry.Key, cellBlocks);
                    followed = true;
                }
                if (!followed) break;
            }
            if (revolvedBlocks == null) return blocks;
            if (blocks != null && blocks.SourceSurfaceIds.Length == 0) return revolvedBlocks;
            //
            if (error != null)
            {
                error = null;
                errorPartName = null;
                return revolvedBlocks;
            }
            return SweepBlocks.Merge(new SweepBlocks[] { blocks, revolvedBlocks });
        }
        /// <summary>
        /// The blocks without the ones lying on the given parts. The layer edges of the blocks are kept by the parts
        /// their edges lie on.
        /// </summary>
        private static SweepBlocks GetSweepBlocksWithoutParts(SweepBlocks blocks, HashSet<int> partIds)
        {
            if (partIds.Count == 0) return blocks;
            //
            int[] kept = Enumerable.Range(0, blocks.SourceSurfaceIds.Length)
                                   .Where(i => !partIds.Contains(FeMesh.GetPartIdFromGmshTopologyId(
                                                                 blocks.SourceSurfaceIds[i][0]))).ToArray();
            int[][][] layers = blocks.LayerGroupEdgeIds
                                     .Where(layer => !layer.Any(edgeIds => edgeIds.Any(edgeId =>
                                            partIds.Contains(FeMesh.GetPartIdFromGmshTopologyId(edgeId))))).ToArray();
            return new SweepBlocks(kept.Select(i => blocks.SourceSurfaceIds[i]).ToArray(),
                                   kept.Select(i => blocks.SideSurfaceIds[i]).ToArray(),
                                   kept.Select(i => blocks.TargetSurfaceIds[i]).ToArray(), layers,
                                   kept.Select(i => blocks.ArrowStarts[i]).ToArray(),
                                   kept.Select(i => blocks.ArrowDirections[i]).ToArray(),
                                   kept.Select(i => blocks.RevolveAxes[i]).ToArray());
        }
        /// <summary>
        /// The swept blocks of the cells of a compound, every block that ends on the target of a revolved cell turned
        /// round to start from it. A swept block may end on the source of a revolved block, which then reads what the
        /// sweep leaves there, but two targets cannot meet, since both blocks build theirs. The sweep of a block
        /// between two ends finds its source on its own and may run either way, as two straight pipes swept from their
        /// free ends towards the elbow between them do. A block is turned only when it runs from one surface to one
        /// other in one layer and its source, which becomes its target, is shared with nothing or with the source of
        /// a revolved cell, as in a ring of swept and revolved cells all turned one way round.
        /// </summary>
        private SweepBlocks GetSweptBlocksTurnedToRevolvedCells(GeometryPart[] cells, SweepBlocks blocks,
                                                                Dictionary<int, SweepBlocks> followerBlocks)
        {
            if (cells.Length < 2 || blocks == null) return blocks;
            //
            HashSet<int> sweptPartIds = new HashSet<int>(blocks.SourceSurfaceIds.Select(surfaceIds =>
                                                         FeMesh.GetPartIdFromGmshTopologyId(surfaceIds[0])));
            Dictionary<int, SweepBlocks> alignedBlocks = GetAlignedRevolvedBlocks(cells, sweptPartIds, followerBlocks);
            HashSet<int> revolvedTargetIds = new HashSet<int>(alignedBlocks.Values.SelectMany(revolved =>
                                                                                   revolved.TargetSurfaceIds[0]));
            HashSet<int> revolvedSourceIds = new HashSet<int>(alignedBlocks.Values.SelectMany(revolved =>
                                                                                   revolved.SourceSurfaceIds[0]));
            if (revolvedTargetIds.Count == 0) return blocks;
            //
            int twinId;
            bool turned = false;
            Dictionary<int, int> twinSurfaceIds = _geometry.GetSharedSurfaceIds(cells);
            int[][] sourceSurfaceIds = blocks.SourceSurfaceIds.ToArray();
            int[][] targetSurfaceIds = blocks.TargetSurfaceIds.ToArray();
            double[][] arrowStarts = blocks.ArrowStarts.ToArray();
            double[][] arrowDirections = blocks.ArrowDirections.ToArray();
            for (int i = 0; i < sourceSurfaceIds.Length; i++)
            {
                if (blocks.RevolveAxes[i] != null) continue;
                if (sourceSurfaceIds[i].Length != 1 || targetSurfaceIds[i].Length != 1) continue;
                if (twinSurfaceIds.TryGetValue(sourceSurfaceIds[i][0], out twinId) && !revolvedSourceIds.Contains(twinId))
                    continue;
                if (!twinSurfaceIds.TryGetValue(targetSurfaceIds[i][0], out twinId) ||
                    !revolvedTargetIds.Contains(twinId)) continue;
                //
                int partId = FeMesh.GetPartIdFromGmshTopologyId(sourceSurfaceIds[i][0]);
                int[][][] layers = blocks.LayerGroupEdgeIds
                                         .Where(layer => layer.Any(edgeIds => edgeIds.Any(edgeId =>
                                                FeMesh.GetPartIdFromGmshTopologyId(edgeId) == partId))).ToArray();
                if (layers.Length != 1) continue;
                //
                GeometryPart part = cells.First(cell => cell.PartId == partId);
                int[][][] layerItemIds = new int[][][] { layers[0].Select(edgeIds => edgeIds.Select(edgeId =>
                                                         FeMesh.GetItemIdFromGmshTopologyId(edgeId)).ToArray()).ToArray() };
                HashSet<int> newSourceItemIds = new HashSet<int>() {
                    FeMesh.GetItemIdFromGmshTopologyId(targetSurfaceIds[i][0]) };
                GetBlockArrow(part, newSourceItemIds, layerItemIds, out arrowStarts[i], out arrowDirections[i]);
                (sourceSurfaceIds[i], targetSurfaceIds[i]) = (targetSurfaceIds[i], sourceSurfaceIds[i]);
                turned = true;
            }
            if (!turned) return blocks;
            return new SweepBlocks(sourceSurfaceIds, blocks.SideSurfaceIds, targetSurfaceIds, blocks.LayerGroupEdgeIds,
                                   arrowStarts, arrowDirections, blocks.RevolveAxes);
        }
        /// <summary>
        /// The blocks the revolved cells the swept blocks leave out were found revolved by, by their part, every
        /// cell turned the same way round about one axis, so that the target of one is the source of the next. Every
        /// cell found its source on its own, and one found from its other end is reversed. The cells following a
        /// revolved neighbour are revolved by the given blocks.
        /// </summary>
        private Dictionary<int, SweepBlocks> GetAlignedRevolvedBlocks(GeometryPart[] cells, HashSet<int> sweptPartIds,
                                                                      Dictionary<int, SweepBlocks> followerBlocks)
        {
            double[] referenceAxis = null;
            Dictionary<int, SweepBlocks> partIdBlocks = new Dictionary<int, SweepBlocks>();
            foreach (var cell in cells)
            {
                SweepBlocks cellBlocks;
                if (sweptPartIds.Contains(cell.PartId)) continue;
                if (!followerBlocks.TryGetValue(cell.PartId, out cellBlocks))
                {
                    if (!GetCellSweepDetection(cell).IsRevolved) continue;
                    cellBlocks = GetCellSweepDetection(cell).Blocks;
                }
                double[] axis = cellBlocks.RevolveAxes[0];
                if (referenceAxis == null) referenceAxis = axis;
                else if (axis[3] * referenceAxis[3] + axis[4] * referenceAxis[4] + axis[5] * referenceAxis[5] < 0)
                    cellBlocks = GetReversedRevolvedBlocks(cellBlocks);
                partIdBlocks.Add(cell.PartId, cellBlocks);
            }
            return partIdBlocks;
        }
        /// <summary>
        /// The revolved blocks of the cells of a compound the swept blocks leave to tetrahedra: a cell the sweep
        /// between two ends cannot mesh and that is revolved about an axis it touches. A revolved block rebuilds its
        /// walls and its target, so a surface it shares with a swept block has to be the source of that block, which
        /// reads it back from there, a wall of both, whose grid the swept block reads as the revolved block leaves
        /// it, its counts settled between the two, or the target of the swept block and the source of the revolved
        /// one, which reads it as the sweep leaves it; the blocks are built in the order they feed each other. A
        /// surface it shares with another revolved block, turned about the same axis through the same angle, has to
        /// be a wall of both, or the target of one and the source of the other, which is then built after it. A cell
        /// that shares a surface in any other way is left to tetrahedra, and a swept cell standing in its way is
        /// given with the axis of the cell. Null when no cell is revolved.
        /// </summary>
        private SweepBlocks CreateRevolvedBlocksForCells(GeometryPart[] cells, SweepBlocks sweptBlocks,
                                                        Dictionary<int, SweepBlocks> followerBlocks,
                                                        Dictionary<int, double[]> blockingPartIdAxis)
        {
            if (cells.Length < 2) return null;
            //
            int twinId;
            int[][] partBlock;
            // The swept blocks by the part they lie on
            Dictionary<int, int[][]> partIdSweptBlock = new Dictionary<int, int[][]>();
            if (sweptBlocks != null)
            {
                for (int i = 0; i < sweptBlocks.SourceSurfaceIds.Length; i++)
                {
                    int partId = FeMesh.GetPartIdFromGmshTopologyId(sweptBlocks.SourceSurfaceIds[i][0]);
                    partIdSweptBlock[partId] = new int[][] { sweptBlocks.SourceSurfaceIds[i],
                                                             sweptBlocks.SideSurfaceIds[i],
                                                             sweptBlocks.TargetSurfaceIds[i] };
                }
            }
            Dictionary<int, SweepBlocks> alignedBlocks =
                GetAlignedRevolvedBlocks(cells, new HashSet<int>(partIdSweptBlock.Keys), followerBlocks);
            if (alignedBlocks.Count == 0) return null;
            //
            Dictionary<int, int> twinSurfaceIds = _geometry.GetSharedSurfaceIds(cells);
            Dictionary<int, SweepBlocks> partIdRevolved = new Dictionary<int, SweepBlocks>();
            foreach (var cell in cells.Where(cell => alignedBlocks.ContainsKey(cell.PartId)))
            {
                SweepBlocks cellBlocks = alignedBlocks[cell.PartId];
                HashSet<int> ownSideIds = new HashSet<int>(cellBlocks.SideSurfaceIds[0]);
                HashSet<int> ownSourceIds = new HashSet<int>(cellBlocks.SourceSurfaceIds[0]);
                HashSet<int> ownTargetIds = new HashSet<int>(cellBlocks.TargetSurfaceIds[0]);
                bool revolvable = true;
                foreach (var gmshTopologyId in cellBlocks.SourceSurfaceIds[0].Concat(cellBlocks.SideSurfaceIds[0])
                                                                            .Concat(cellBlocks.TargetSurfaceIds[0]))
                {
                    if (!twinSurfaceIds.TryGetValue(gmshTopologyId, out twinId)) continue;
                    //
                    int neighbourPartId = FeMesh.GetPartIdFromGmshTopologyId(twinId);
                    if (partIdSweptBlock.TryGetValue(neighbourPartId, out partBlock))
                    {
                        bool sweptSource = partBlock[0].Contains(twinId);
                        bool bothWalls = ownSideIds.Contains(gmshTopologyId) && partBlock[1].Contains(twinId);
                        bool fed = ownSourceIds.Contains(gmshTopologyId) && partBlock[2].Contains(twinId);
                        if (!sweptSource && !bothWalls && !fed)
                        {
                            revolvable = false;
                            blockingPartIdAxis[neighbourPartId] = cellBlocks.RevolveAxes[0];
                        }
                    }
                    else if (partIdRevolved.TryGetValue(neighbourPartId, out SweepBlocks neighbourBlocks))
                    {
                        bool bothWalls = ownSideIds.Contains(gmshTopologyId) &&
                                         neighbourBlocks.SideSurfaceIds[0].Contains(twinId);
                        bool feeds = (ownTargetIds.Contains(gmshTopologyId) &&
                                      neighbourBlocks.SourceSurfaceIds[0].Contains(twinId)) ||
                                     (ownSourceIds.Contains(gmshTopologyId) &&
                                      neighbourBlocks.TargetSurfaceIds[0].Contains(twinId));
                        // Two cells sharing a wall turn through the same angle; one feeding the other only about the
                        // same axis
                        if (!(bothWalls || feeds) ||
                            !AreRevolveAxesEqual(cellBlocks.RevolveAxes[0], neighbourBlocks.RevolveAxes[0], cell, bothWalls))
                            revolvable = false;
                    }
                    if (!revolvable) break;
                }
                if (revolvable) partIdRevolved.Add(cell.PartId, cellBlocks);
            }
            if (partIdRevolved.Count == 0) return null;
            return SweepBlocks.Merge(partIdRevolved.Values);
        }
        /// <summary>
        /// The revolved block turned the other way round: from its target onto its source, about the reversed axis.
        /// </summary>
        private static SweepBlocks GetReversedRevolvedBlocks(SweepBlocks blocks)
        {
            double[] axis = blocks.RevolveAxes[0];
            double[] reversedAxis = new double[] { axis[0], axis[1], axis[2], -axis[3], -axis[4], -axis[5], axis[6] };
            // The arrow starts on the new source, where the old one was turned to, and points back
            double[] arrowStart = null;
            double[] arrowDirection = null;
            if (blocks.ArrowStarts[0] != null && blocks.ArrowDirections[0] != null)
            {
                Vec3D direction = new Vec3D(axis[3], axis[4], axis[5]);
                direction.Normalize();
                Vec3D start = RotateAboutAxis(new Vec3D(blocks.ArrowStarts[0]), new Vec3D(axis[0], axis[1], axis[2]),
                                              direction, axis[6] * Math.PI / 180);
                Vec3D tangent = Vec3D.CrossProduct(-1 * direction, start - new Vec3D(axis[0], axis[1], axis[2]));
                if (tangent.Len > 0)
                {
                    tangent.Normalize();
                    arrowStart = start.Coor;
                    arrowDirection = tangent.Coor;
                }
            }
            return new SweepBlocks(blocks.TargetSurfaceIds, blocks.SideSurfaceIds, blocks.SourceSurfaceIds,
                                   blocks.LayerGroupEdgeIds, new double[][] { arrowStart },
                                   new double[][] { arrowDirection }, new double[][] { reversedAxis });
        }
        private static bool AreRevolveAxesEqual(double[] axis1, double[] axis2, GeometryPart part, bool sameAngle)
        {
            double tolerance = 1E-4 * part.BoundingBox.GetDiagonal();
            Vec3D direction1 = new Vec3D(axis1[3], axis1[4], axis1[5]);
            Vec3D direction2 = new Vec3D(axis2[3], axis2[4], axis2[5]);
            if (Vec3D.DotProduct(direction1, direction2) < 1 - 1E-6) return false;
            if (sameAngle && Math.Abs(axis1[6] - axis2[6]) > 1E-3) return false;
            //
            double radius;
            double height;
            GetAxialCoordinates(new double[] { axis2[0], axis2[1], axis2[2] }, new Vec3D(axis1[0], axis1[1], axis1[2]),
                                direction1, out radius, out height);
            return radius <= tolerance;
        }
        private SweepBlocks CreatePrismBlocksForCells(GeometryPart[] cells,
                                                      Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                                      out string error, out string errorPartName)
        {
            SweepBlocks blocks = CreateSweepBlocksForGroup(cells, partIdSelectedSurfaceIds, true, out error,
                                                           out errorPartName);
            if (error == null && blocks.SourceSurfaceIds.Length == cells.Length) return blocks;
            //
            GeometryPart[] cellsToSweep = GetCellsToSweep(cells, partIdSelectedSurfaceIds);
            if (cellsToSweep.Length == 0 || cellsToSweep.Length == cells.Length) return blocks;
            //
            string cellsError;
            string cellsErrorPartName;
            SweepBlocks cellBlocks = CreateSweepBlocksForGroup(cellsToSweep, partIdSelectedSurfaceIds, true,
                                                               out cellsError, out cellsErrorPartName);
            if (cellsError != null || (error == null &&
                                       cellBlocks.SourceSurfaceIds.Length <= blocks.SourceSurfaceIds.Length))
                return blocks;
            //
            error = null;
            errorPartName = null;
            return cellBlocks;
        }
        /// <summary>
        /// The blocks of one group. With cellsMayBeLeftOut the cells of a compound the sweep does not reach are left
        /// out of it rather than refusing the group, which is the only failure that leaves them out: every other
        /// refusal still refuses the group.
        /// </summary>
        private SweepBlocks CreateSweepBlocksForGroup(GeometryPart[] sweptParts,
                                                      Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                                      bool cellsMayBeLeftOut, out string error, out string errorPartName)
        {
            if (partIdSelectedSurfaceIds == null)
            {
                // The sources a group is swept from depend on the geometry alone, so they are detected
                // once and kept on the part leading the group
                SweepDetectionData detection = GetSweepDetection(sweptParts);
                error = detection.Error;
                errorPartName = detection.ErrorPartName;
                if (error != null && cellsMayBeLeftOut && detection.PartialBlocks != null)
                {
                    error = null;
                    errorPartName = null;
                    return detection.PartialBlocks;
                }
                return detection.Blocks;
            }
            HashSet<int> surfaceIds;
            Dictionary<int, HashSet<int>> groupSelectedSurfaceIds = new Dictionary<int, HashSet<int>>();
            foreach (var sweptPart in sweptParts)
            {
                if (partIdSelectedSurfaceIds.TryGetValue(sweptPart.PartId, out surfaceIds))
                    groupSelectedSurfaceIds.Add(sweptPart.PartId, surfaceIds);
            }
            bool onlyUnreached;
            HashSet<int> sweptPartIds;
            SweepBlocks blocks = CreateSweepBlocks(sweptParts, groupSelectedSurfaceIds, null, null, new SweepPartMaps(),
                                                   out error, out errorPartName, out sweptPartIds, out onlyUnreached);
            if (error != null && cellsMayBeLeftOut && onlyUnreached && blocks != null && sweptPartIds.Count > 0)
            {
                error = null;
                errorPartName = null;
            }
            return blocks;
        }
        /// <summary>
        /// The cells of a compound that can be swept on their own - or that carry a selected source surface - which
        /// are the ones tried when the compound cannot be swept with all of its cells. A single part is tried
        /// whatever it is.
        /// </summary>
        private GeometryPart[] GetCellsToSweep(GeometryPart[] groupParts,
                                               Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds)
        {
            if (groupParts.Length == 1) return groupParts;
            //
            List<GeometryPart> cells = new List<GeometryPart>();
            foreach (var part in groupParts)
            {
                if ((partIdSelectedSurfaceIds != null && partIdSelectedSurfaceIds.ContainsKey(part.PartId)) ||
                    IsPartTransfinite(part, false) == null ||
                    (GetCellSweepDetection(part).Error == null && !GetCellSweepDetection(part).IsRevolved))
                    cells.Add(part);
            }
            return cells.ToArray();
        }
        /// <summary>
        /// The mirror symmetry of a compound's cells the sweep can use, or null. Every other active mesh setup item
        /// has to select symmetrically, and so has the item itself when it names its source surfaces.
        /// </summary>
        private SweepSymmetry FindSweepSymmetry(SweepMesh sweepMesh, GeometryPart[] sweptParts,
                                                Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                                out string reason)
        {
            List<MeshSetupItem> items = new List<MeshSetupItem>();
            foreach (var entry in _geometry.MeshSetupItems)
            {
                if (entry.Value.Active && entry.Value.Name != sweepMesh.Name) items.Add(entry.Value);
            }
            int[] ownSelection = partIdSelectedSurfaceIds != null ? sweepMesh.CreationIds : null;
            return SweepSymmetry.Find(_geometry, sweptParts, items, ownSelection, out reason);
        }
        private string GetCompoundName(GeometryPart[] cells)
        {
            foreach (var entry in _geometry.Parts)
            {
                if (entry.Value is CompoundGeometryPart compound && compound.SubPartNames.Contains(cells[0].Name))
                    return compound.Name;
            }
            return string.Join(", ", cells.Select(cell => cell.Name));
        }
        /// <summary>
        /// What the automatic detection finds for one group of parts, detected the first time it is asked for
        /// and reused until the geometry of the group changes. What is found depends on the parts swept and nothing
        /// else, so a single part and a whole compound keep it on the part, and a compound keeps what it finds for
        /// some of its cells - its symmetric piece, the cells it sweeps without the others - by the parts they are,
        /// so that asking for one does not throw another away.
        /// </summary>
        private SweepDetectionData GetSweepDetection(GeometryPart[] sweptParts)
        {
            GeometryPart groupPart = GetSweepGroupPart(sweptParts);
            bool someCells = groupPart is CompoundGeometryPart compoundPart &&
                             sweptParts.Length < compoundPart.SubPartNames.Length;
            string stamp = SweepDetectionData.GetStamp(sweptParts);
            //
            SweepDetectionData detection;
            if (someCells) groupPart.CellsSweepDetections.TryGetValue(stamp, out detection);
            else detection = groupPart.SweepDetection;
            if (detection != null && detection.Stamp == stamp) return detection;
            //
            detection = DetectSweepSources(sweptParts, stamp);
            if (someCells)
            {
                // A changed geometry changes the stamps, so what was found for the geometry before is let go
                if (groupPart.CellsSweepDetections.Count >= 16) groupPart.CellsSweepDetections.Clear();
                groupPart.CellsSweepDetections[stamp] = detection;
            }
            else groupPart.SweepDetection = detection;
            //
            return detection;
        }
        /// <summary>
        /// The part a group's detection is kept on: the compound part the group stands for, or the single
        /// part the group is made of.
        /// </summary>
        private GeometryPart GetSweepGroupPart(GeometryPart[] sweptParts)
        {
            CompoundGeometryPart compoundPart = GetCompoundPartOfSubPart(sweptParts[0].Name);
            return compoundPart != null ? (GeometryPart)compoundPart : sweptParts[0];
        }
        /// <summary>
        /// Finds the source surfaces of a sweep, weighing every way every part can be swept against the ways of the
        /// parts it touches. A single part no way sweeps between two ends may still be revolved.
        /// </summary>
        private SweepDetectionData DetectSweepSources(GeometryPart[] sweptParts, string stamp)
        {
            SweepDetectionData detection = DetectPrismSweepSources(sweptParts, stamp);
            if (detection.Error == null || sweptParts.Length != 1) return detection;
            // A part the sweep between two ends cannot mesh may still be revolved about an axis it touches, or about one
            // it stands clear of - a bend whose inner and outer sides differ in length too much for one layer count
            // to suit both, which a revolution turns through equal angles instead
            SweepBlocks blocks = CreateRevolvedBlocks(sweptParts[0], out _) ??
                                 CreateRevolvedBlocks(sweptParts[0], out _, true);
            if (blocks == null) return detection;
            //
            return new SweepDetectionData(blocks, null, null, new HashSet<int>() { sweptParts[0].PartId }, stamp, null,
                                          new SweepBlocks[] { blocks }, new double[] { 0 });
        }
        /// <summary>
        /// The block of a solid part that is revolved about an axis its source touches: one block turning the source
        /// onto the target, with the axis and the angle it is turned through. A part that stands clear of the axis is
        /// taken only when clearOfAxis is set; otherwise it is left to the sweep between two ends. Null, with the
        /// reason, for a part that is not revolved so.
        /// </summary>
        private SweepBlocks CreateRevolvedBlocks(GeometryPart part, out string error, bool clearOfAxis = false)
        {
            HashSet<int> sourceSurfaceIds;
            HashSet<int> targetSurfaceIds;
            double angleDeg;
            double[] axisCenter;
            double[] axisDirection;
            bool reversed;
            if (!part.IsCADPart || (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell))
            {
                error = "The part " + part.Name + " is not a CAD solid.";
                return null;
            }
            error = DetectRevolveSourceSurfaces(part, out sourceSurfaceIds, out targetSurfaceIds, out _, out angleDeg,
                                                out axisCenter, out axisDirection, out reversed, clearOfAxis);
            if (error != null) return null;
            //
            int partId = part.PartId;
            double[] offset = part.Offset != null ? part.Offset : new double[3];
            Vec3D direction = new Vec3D(axisDirection);
            direction.Normalize();
            if (reversed) direction = -1 * direction;
            double[] revolveAxis = new double[] { axisCenter[0] - offset[0], axisCenter[1] - offset[1],
                                                  axisCenter[2] - offset[2], direction.X, direction.Y, direction.Z,
                                                  angleDeg };
            HashSet<int> sideSurfaceIds = new HashSet<int>(Enumerable.Range(0, part.Visualization.FaceCount));
            sideSurfaceIds.ExceptWith(sourceSurfaceIds);
            sideSurfaceIds.ExceptWith(targetSurfaceIds);
            // The arrow starts in the middle of the source vertices and points the way the source is turned
            Vec3D center = new Vec3D();
            HashSet<int> sourceVertexIds = part.Visualization.GetVertexNodeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            foreach (var vertexId in sourceVertexIds) center = center + new Vec3D(_geometry.Nodes[vertexId].Coor);
            center = (1.0 / sourceVertexIds.Count) * center;
            Vec3D tangent = Vec3D.CrossProduct(direction, center - new Vec3D(axisCenter));
            double[] arrowStart = null;
            double[] arrowDirection = null;
            if (tangent.Len > 0)
            {
                tangent.Normalize();
                arrowStart = new double[] { center.X - offset[0], center.Y - offset[1], center.Z - offset[2] };
                arrowDirection = tangent.Coor;
            }
            //
            return new SweepBlocks(new int[][] { ToGmshTopologyIds(sourceSurfaceIds.OrderBy(id => id), partId) },
                                   new int[][] { ToGmshTopologyIds(sideSurfaceIds.OrderBy(id => id), partId) },
                                   new int[][] { ToGmshTopologyIds(targetSurfaceIds.OrderBy(id => id), partId) },
                                   new int[0][][], new double[][] { arrowStart }, new double[][] { arrowDirection },
                                   new double[][] { revolveAxis });
        }
        /// <summary>
        /// The sources of a part or a compound, found by weighing every way every cell can be swept against the ways
        /// of its neighbours. A cell has the ways its own seeds give it - every surface of it, grown over its plane as
        /// a selected surface is - and the ways its neighbours can give it - from a target or a source they share
        /// with it, or across a shared wall, from the surfaces continuing their source - and the ways those give in
        /// turn; each is a block of the cell validated as a selected source is. Two cells agree on a surface they
        /// share as SweepCellCandidate tells, and SweepDirectionProblem finds the ways that sweep the most cells,
        /// then continue one another across the most shared surfaces, and then score best, a cell it leaves out
        /// being meshed by tetrahedra. The blocks are those of the chosen ways, in the order the cells feed one
        /// another. A single part takes its best scored way, every way it has being kept as its directions. Cells
        /// may feed one another round a ring, as the cells of a stack about an axis do, when the block feeding back
        /// into the first cell of the ring has no other target, so that the mesher can close the ring on it; a
        /// choice that cannot be ordered so is excluded and the problem solved again. A cell left unswept is named
        /// in the error, with the blocks of the cells that are swept and what keeps every other cell out of the
        /// sweep.
        /// </summary>
        private SweepDetectionData DetectPrismSweepSources(GeometryPart[] sweptParts, string stamp)
        {
            int numOfCells = sweptParts.Length;
            string groupName = GetSweepGroupPart(sweptParts).Name;
            Dictionary<int, int> partIdCell = new Dictionary<int, int>();
            for (int i = 0; i < numOfCells; i++) partIdCell.Add(sweptParts[i].PartId, i);
            // The surfaces and the edges the cells share
            Dictionary<int, int> twinSurfaceIds = new Dictionary<int, int>();
            Dictionary<int, int> twinEdgeIds = new Dictionary<int, int>();
            if (numOfCells > 1)
            {
                twinSurfaceIds = _geometry.GetSharedSurfaceIds(sweptParts);
                twinEdgeIds = _geometry.GetSharedEdgeIds(sweptParts);
            }
            // The maps of every part, which every way of it asks for again
            SweepPartMaps maps = new SweepPartMaps();
            // The ways of every cell, grown from the seeds and from every way found until no new one appears
            int twinId;
            int neighbour;
            string error;
            Dictionary<int, GeomFaceType> faceTypes = new Dictionary<int, GeomFaceType>();
            List<SweepCellCandidate>[] candidates = new List<SweepCellCandidate>[numOfCells];
            HashSet<string> triedSources = new HashSet<string>();
            Queue<Tuple<int, HashSet<int>, bool>> pendingSources = new Queue<Tuple<int, HashSet<int>, bool>>();
            // What the seeds of every cell ran into, and a defect of the geometry a way ran into, which tells the user
            // more than that no source was found
            List<KeyValuePair<HashSet<int>, string>>[] seedErrors =
                new List<KeyValuePair<HashSet<int>, string>>[numOfCells];
            string geometryDefect = null;
            // The first refusal of a seed of every cell that found its walls and target, which names what is wrong
            // with the block rather than that there is none
            bool reachedTarget;
            string[] reachedTargetErrors = new string[numOfCells];
            for (int i = 0; i < numOfCells; i++)
            {
                candidates[i] = new List<SweepCellCandidate>();
                seedErrors[i] = new List<KeyValuePair<HashSet<int>, string>>();
                foreach (var seedSurfaceIds in GetSeedSurfaceIds(sweptParts[i]))
                    pendingSources.Enqueue(new Tuple<int, HashSet<int>, bool>(i, seedSurfaceIds, false));
            }
            while (pendingSources.Count > 0)
            {
                Tuple<int, HashSet<int>, bool> pending = pendingSources.Dequeue();
                int cell = pending.Item1;
                GeometryPart part = sweptParts[cell];
                string key = cell + (pending.Item3 ? "d:" : "s:") + string.Join(",", pending.Item2.OrderBy(id => id));
                if (!triedSources.Add(key)) continue;
                if (triedSources.Count > _maxCompoundSweepSources)
                {
                    return new SweepDetectionData(null, "The part " + groupName + " can be swept in more ways than " +
                                                  "the detection weighs. Select the source surfaces by hand.", null,
                                                  new HashSet<int>(), stamp);
                }
                //
                SweepCellCandidate candidate = CreateSweepCellCandidate(part, pending.Item2, pending.Item3,
                                                                        twinSurfaceIds, maps, faceTypes, out error,
                                                                        out reachedTarget);
                if (candidate == null)
                {
                    if (!pending.Item3 && reachedTarget && reachedTargetErrors[cell] == null)
                        reachedTargetErrors[cell] = error;
                    if (geometryDefect == null && error.StartsWith(_splitBoreRimErrorStart)) geometryDefect = error;
                    if (!pending.Item3)
                        seedErrors[cell].Add(new KeyValuePair<HashSet<int>, string>(pending.Item2, error));
                    continue;
                }
                int same = candidates[cell].FindIndex(other =>
                    other.SourceSurfaceIds.SetEquals(candidate.SourceSurfaceIds) &&
                    other.TargetSurfaceIds.SetEquals(candidate.TargetSurfaceIds));
                if (same >= 0) continue;
                candidates[cell].Add(candidate);
                // The ways this one gives its neighbours: from a cap they share, and across a wall from the surfaces
                // continuing its source
                foreach (var surfaceId in candidate.SourceSurfaceIds.Concat(candidate.TargetSurfaceIds))
                {
                    if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(surfaceId, part.PartId), out twinId) ||
                        !partIdCell.TryGetValue(FeMesh.GetPartIdFromGmshTopologyId(twinId), out neighbour)) continue;
                    pendingSources.Enqueue(new Tuple<int, HashSet<int>, bool>(neighbour,
                        new HashSet<int>() { FeMesh.GetItemIdFromGmshTopologyId(twinId) }, true));
                }
                foreach (var sideSurfaceId in candidate.SideSurfaceIds)
                {
                    if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(sideSurfaceId, part.PartId), out twinId) ||
                        !partIdCell.TryGetValue(FeMesh.GetPartIdFromGmshTopologyId(twinId), out neighbour)) continue;
                    int neighbourSideSurfaceId = FeMesh.GetItemIdFromGmshTopologyId(twinId);
                    HashSet<int> continuingIds = GetSourceContinuingAcrossWall(part, candidate.SourceSurfaceIds,
                                                                               sideSurfaceId, sweptParts[neighbour],
                                                                               neighbourSideSurfaceId, twinEdgeIds);
                    if (continuingIds.Count > 0)
                        pendingSources.Enqueue(new Tuple<int, HashSet<int>, bool>(neighbour, continuingIds, true));
                }
            }
            if (numOfCells == 1 && candidates[0].Count > 0)
            {
                // The best scored way, the first found among equals
                List<SweepCellCandidate> ways = candidates[0].Select((candidate, index) => new { candidate, index })
                                                             .OrderByDescending(entry => entry.candidate.Score)
                                                             .ThenBy(entry => entry.index)
                                                             .Select(entry => entry.candidate).ToList();
                return new SweepDetectionData(ways[0].Blocks, null, null, new HashSet<int>() { sweptParts[0].PartId },
                                              stamp, null, ways.Select(way => way.Blocks).ToArray(),
                                              ways.Select(way => way.Score).ToArray());
            }
            // The ways two cells sharing a surface agree on
            BoundingBox box = new BoundingBox();
            foreach (var part in sweptParts) box.IncludeBox(part.BoundingBox);
            double tolerance = 1E-3 * box.GetDiagonal();
            double[][] scores = candidates.Select(cellCandidates => cellCandidates.Select(c => c.Score).ToArray())
                                          .ToArray();
            SweepDirectionProblem problem = new SweepDirectionProblem(scores);
            foreach (var entry in candidates.Any(cellCandidates => cellCandidates.Count > 0)
                                  ? twinSurfaceIds : new Dictionary<int, int>())
            {
                int cellA;
                int cellB;
                if (!partIdCell.TryGetValue(FeMesh.GetPartIdFromGmshTopologyId(entry.Key), out cellA) ||
                    !partIdCell.TryGetValue(FeMesh.GetPartIdFromGmshTopologyId(entry.Value), out cellB) ||
                    cellA >= cellB) continue;
                //
                int surfaceA = FeMesh.GetItemIdFromGmshTopologyId(entry.Key);
                int surfaceB = FeMesh.GetItemIdFromGmshTopologyId(entry.Value);
                bool[,] compatible = new bool[candidates[cellA].Count + 1, candidates[cellB].Count + 1];
                bool[,] continuing = new bool[candidates[cellA].Count + 1, candidates[cellB].Count + 1];
                for (int a = 0; a <= candidates[cellA].Count; a++)
                {
                    SweepCellCandidate candidateA = a < candidates[cellA].Count ? candidates[cellA][a] : null;
                    for (int b = 0; b <= candidates[cellB].Count; b++)
                    {
                        SweepCellCandidate candidateB = b < candidates[cellB].Count ? candidates[cellB][b] : null;
                        SweepCellConflictEnum conflict = SweepCellCandidate.GetConflict(candidateA, surfaceA,
                                                                                        candidateB, surfaceB,
                                                                                        tolerance, out _);
                        compatible[a, b] = conflict == SweepCellConflictEnum.None;
                        continuing[a, b] = SweepCellCandidate.Continue(candidateA, surfaceA, candidateB, surfaceB,
                                                                      tolerance);
                    }
                }
                problem.AddSurface(cellA, cellB, compatible, continuing);
            }
            // The blocks in the order the cells feed one another: a cell swept from the target of another after it
            int[] choices = Enumerable.Repeat(-1, numOfCells).ToArray();
            List<int> order = new List<int>();
            int[] unorderedCells;
            int numOfSolves = 0;
            while (candidates.Any(cellCandidates => cellCandidates.Count > 0))
            {
                choices = problem.Solve();
                if (choices == null)
                {
                    return new SweepDetectionData(null, "lp_solve found no way of sweeping the part " + groupName +
                                                  ". Select the source surfaces by hand.", null, new HashSet<int>(),
                                                  stamp);
                }
                order = GetCompoundSweepOrder(sweptParts, candidates, choices, twinSurfaceIds, partIdCell,
                                              out unorderedCells);
                if (order != null) break;
                if (++numOfSolves > _maxCompoundSweepSolves)
                {
                    return new SweepDetectionData(null, "The parts of " + groupName + " cannot be swept in an order " +
                                                  "the mesher can follow. Select the source surfaces by hand.", null,
                                                  new HashSet<int>(), stamp);
                }
                problem.AddExclusion(unorderedCells, unorderedCells.Select(cell => choices[cell]).ToArray());
            }
            //
            SweepBlocks blocks = order.Count > 0
                                 ? SweepBlocks.Merge(order.Select(cell => candidates[cell][choices[cell]].Blocks))
                                 : null;
            double score = order.Sum(cell => candidates[cell][choices[cell]].Score);
            HashSet<int> sweptPartIds = new HashSet<int>(order.Select(cell => sweptParts[cell].PartId));
            if (order.Count == numOfCells)
                return new SweepDetectionData(blocks, null, null, sweptPartIds, stamp, null,
                                              new SweepBlocks[] { blocks }, new double[] { score });
            // The cells left to tetrahedra, with what keeps the ways they have on their own out of the sweep
            Dictionary<int, string> reasons = new Dictionary<int, string>();
            GeometryPart firstUnswept = null;
            for (int i = 0; i < numOfCells; i++)
            {
                if (choices[i] >= 0) continue;
                if (firstUnswept == null) firstUnswept = sweptParts[i];
                if (numOfCells == 1) continue;
                string reason = GetCompoundSweepConflict(sweptParts, i, candidates, choices, twinSurfaceIds, partIdCell,
                                                         tolerance) ??
                                GetUnsweptCellReason(sweptParts[i], seedErrors[i]);
                if (reason != null) reasons.Add(sweptParts[i].PartId, reason);
            }
            // A single part is told what the first of its sources that found a block ran into, unless a defect of the
            // geometry says more
            string partReason = geometryDefect;
            if (partReason == null && numOfCells == 1) partReason = reachedTargetErrors[0];
            return new SweepDetectionData(null, "No sweep source surface could be found automatically for the part " +
                                          firstUnswept.Name + ". Select the source surfaces by hand." +
                                          (partReason != null ? " " + partReason : ""), null,
                                          sweptPartIds, stamp, blocks, null, null, reasons);
        }
        /// <summary>
        /// Why a cell with no way among its neighbours is not swept although it can be on its own: what the source it
        /// is swept from alone, or a seed that source grew from, ran into against its neighbours. A cell that cannot
        /// be swept on its own has its own reason.
        /// </summary>
        private string GetUnsweptCellReason(GeometryPart part, List<KeyValuePair<HashSet<int>, string>> seedErrors)
        {
            if (seedErrors.Count == 0) return null;
            //
            SweepDetectionData cellDetection = GetCellSweepDetection(part);
            if (cellDetection.Error != null) return null;
            foreach (var direction in cellDetection.Directions)
            {
                HashSet<int> sourceSurfaceIds = new HashSet<int>(direction.SourceSurfaceIds.SelectMany(ids => ids)
                                                                 .Select(FeMesh.GetItemIdFromGmshTopologyId));
                string reason = seedErrors.Where(seedError => seedError.Key.IsSubsetOf(sourceSurfaceIds))
                                          .Select(seedError => seedError.Value).FirstOrDefault();
                if (reason != null) return reason;
            }
            return null;
        }
        /// <summary>
        /// The block of a cell swept from the given source, built as a selected source is - the source grown over the
        /// surfaces in its plane, and a source the cell is given by a neighbour over the surfaces leaning on it -
        /// with the role every edge of its walls plays, or null with the reason when the block is refused.
        /// </summary>
        private SweepCellCandidate CreateSweepCellCandidate(GeometryPart part, HashSet<int> seedSurfaceIds,
                                                            bool derived, Dictionary<int, int> twinSurfaceIds,
                                                            SweepPartMaps maps, Dictionary<int, GeomFaceType> faceTypes,
                                                            out string error, out bool reachedTarget)
        {
            VisualizationData vis = part.Visualization;
            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds = maps.GetSurfaceIdSurfaceNeighbourIds(part);
            HashSet<int> sourceSurfaceIds = new HashSet<int>(seedSurfaceIds);
            sourceSurfaceIds.UnionWith(maps.GetExtendedSourceSurfaceIds(part, sourceSurfaceIds, derived, () =>
            {
                HashSet<int> extendedSourceSurfaceIds = new HashSet<int>(sourceSurfaceIds);
                ExtendSourceOverCoplanarSurfaces(part, extendedSourceSurfaceIds, surfaceIdSurfaceNeighbourIds, maps);
                if (derived)
                {
                    ExtendDerivedSourceOverLeaningSurfaces(part, extendedSourceSurfaceIds, surfaceIdSurfaceNeighbourIds,
                                                           maps);
                }
                return extendedSourceSurfaceIds;
            }));
            SweepBlockValidation validation = maps.GetSweepBlockValidation(part, sourceSurfaceIds, true,
                () => ValidateSweepBlock(part, sourceSurfaceIds, surfaceIdSurfaceNeighbourIds, maps, twinSurfaceIds));
            error = validation.Error;
            reachedTarget = validation.ReachedTarget;
            if (error != null) return null;
            //
            int[] sideSurfaceIds;
            int[] targetSurfaceIds;
            List<int[]> layerSideSurfaceIds;
            int[] directionEdgeIds;
            validation.CopyTo(out sideSurfaceIds, out targetSurfaceIds, out layerSideSurfaceIds, out directionEdgeIds);
            HashSet<int> wallEdgeIds = validation.GetWallEdgeIds(() => vis.GetEdgeIdsForSurfaceIds(sideSurfaceIds));
            bool prismatic = validation.IsPrismatic(() =>
                IsPrismaticSweepBlock(part, sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds));
            int[][][] layerGroupEdgeIds = validation.GetLayerGroupEdgeIds(() =>
                directionEdgeIds != null ? new int[][][] { new int[][] { directionEdgeIds } }
                                         : GetDirectionEdges(vis, surfaceIdSurfaceNeighbourIds, sideSurfaceIds,
                                                             layerSideSurfaceIds));
            double[] arrowStart;
            double[] arrowDirection;
            validation.GetArrow(() =>
            {
                double[] start;
                double[] direction;
                GetBlockArrow(part, sourceSurfaceIds, layerGroupEdgeIds, out start, out direction);
                return new double[][] { start, direction };
            }, out arrowStart, out arrowDirection);
            // The sides of every wall as the mesher sorts them: the edges on the source side, those on the target side
            // and the chains of edges along the sweep, a wall that meets neither the source nor the target being
            // sided from the walls between it and the source
            HashSet<int> alongEdgeIds = new HashSet<int>(layerGroupEdgeIds.SelectMany(groups => groups)
                                                                          .SelectMany(edgeIds => edgeIds));
            Dictionary<int, int[]> surfaceIdEdgeIds = new Dictionary<int, int[]>();
            for (int i = 0; i < vis.FaceEdgeIds.Length; i++) surfaceIdEdgeIds.Add(i, vis.FaceEdgeIds[i]);
            Dictionary<int, double[][][]> wallIdSideCentroids = new Dictionary<int, double[][][]>();
            foreach (var wall in CaeMesh.Sweep.SweepWallTopology.Classify(sourceSurfaceIds, sideSurfaceIds,
                                                                         targetSurfaceIds, surfaceIdEdgeIds,
                                                                         alongEdgeIds))
            {
                List<double[][]> sides = new List<double[][]>();
                sides.Add(wall.SourceEdgeIds.Select(id => _geometry.ComputeEdgeCentroid(vis, id)).ToArray());
                sides.Add(wall.TargetEdgeIds.Select(id => _geometry.ComputeEdgeCentroid(vis, id)).ToArray());
                foreach (var chain in GetConnectedEdgeChains(vis, wall.DirectionEdgeIds.ToList()))
                    sides.Add(chain.Select(id => _geometry.ComputeEdgeCentroid(vis, id)).ToArray());
                wallIdSideCentroids[wall.SurfaceId] = sides.ToArray();
            }
            // The block in gmsh numbering
            int partId = part.PartId;
            int[][][] gmshLayerGroupEdgeIds = layerGroupEdgeIds.Select(groups =>
                groups.Select(edgeIds => ToGmshTopologyIds(edgeIds, partId)).ToArray()).ToArray();
            SweepBlocks blocks = new SweepBlocks(new int[][] { ToGmshTopologyIds(sourceSurfaceIds, partId) },
                                                 new int[][] { ToGmshTopologyIds(sideSurfaceIds, partId) },
                                                 new int[][] { ToGmshTopologyIds(targetSurfaceIds, partId) },
                                                 gmshLayerGroupEdgeIds, new double[][] { arrowStart },
                                                 new double[][] { arrowDirection });
            double score = ScoreSweepBlocks(blocks, faceTypes, maps);
            //
            return new SweepCellCandidate(partId, sourceSurfaceIds.ToArray(), sideSurfaceIds, targetSurfaceIds,
                                          prismatic, wallEdgeIds, vis.FaceEdgeIds, wallIdSideCentroids, blocks, score);
        }
        /// <summary>
        /// The edges split into chains of edges that meet at a vertex.
        /// </summary>
        private static List<List<int>> GetConnectedEdgeChains(VisualizationData vis, List<int> edgeIds)
        {
            List<List<int>> chains = new List<List<int>>();
            List<int> left = new List<int>(edgeIds);
            while (left.Count > 0)
            {
                List<int> chain = new List<int>() { left[0] };
                HashSet<int> vertexIds = new HashSet<int>(vis.GetVertexNodeIdsForEdgeId(left[0]));
                left.RemoveAt(0);
                bool grown = true;
                while (grown)
                {
                    grown = false;
                    for (int i = 0; i < left.Count; i++)
                    {
                        int[] edgeVertexIds = vis.GetVertexNodeIdsForEdgeId(left[i]);
                        if (!edgeVertexIds.Any(vertexIds.Contains)) continue;
                        //
                        chain.Add(left[i]);
                        vertexIds.UnionWith(edgeVertexIds);
                        left.RemoveAt(i);
                        grown = true;
                        break;
                    }
                }
                chains.Add(chain);
            }
            return chains;
        }
        /// <summary>
        /// The surfaces of a neighbour across a shared wall that continue the source over the wall: those on the
        /// twins of the wall edges the source touches, as the propagation gives them to a neighbour swept alongside.
        /// </summary>
        private HashSet<int> GetSourceContinuingAcrossWall(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                                           int sideSurfaceId, GeometryPart neighbour,
                                                           int neighbourSideSurfaceId, Dictionary<int, int> twinEdgeIds)
        {
            int twinId;
            VisualizationData vis = part.Visualization;
            VisualizationData neighbourVis = neighbour.Visualization;
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            sourceEdgeIds.IntersectWith(vis.FaceEdgeIds[sideSurfaceId]);
            //
            HashSet<int> continuingIds = new HashSet<int>();
            foreach (var edgeId in sourceEdgeIds)
            {
                if (!twinEdgeIds.TryGetValue(FeMesh.GmshTopologyId(edgeId, part.PartId), out twinId) ||
                    FeMesh.GetPartIdFromGmshTopologyId(twinId) != neighbour.PartId) continue;
                //
                int neighbourEdgeId = FeMesh.GetItemIdFromGmshTopologyId(twinId);
                for (int i = 0; i < neighbourVis.FaceEdgeIds.Length; i++)
                {
                    if (i != neighbourSideSurfaceId && neighbourVis.FaceEdgeIds[i].Contains(neighbourEdgeId))
                        continuingIds.Add(i);
                }
            }
            return continuingIds;
        }
        /// <summary>
        /// The swept cells in the order they are meshed: a cell after every cell whose target it is swept from, and
        /// otherwise in the order of the cells. Cells feeding one another round a ring are taken from the first cell
        /// of the ring, and the cell feeding back into it closes the ring: the mesher keeps the target of a block
        /// whose targets are all sources of blocks meshed before it. A block feeding back that has other targets
        /// as well cannot close the ring, and null comes back, with unorderedCells the block and the cells it feeds
        /// back into, a combination of choices that cannot be meshed.
        /// </summary>
        private static List<int> GetCompoundSweepOrder(GeometryPart[] sweptParts, List<SweepCellCandidate>[] candidates,
                                                       int[] choices, Dictionary<int, int> twinSurfaceIds,
                                                       Dictionary<int, int> partIdCell, out int[] unorderedCells)
        {
            unorderedCells = null;
            int numOfCells = sweptParts.Length;
            List<int>[] fedCells = new List<int>[numOfCells];
            int[] numOfFeeders = new int[numOfCells];
            for (int i = 0; i < numOfCells; i++) fedCells[i] = new List<int>();
            for (int i = 0; i < numOfCells; i++)
            {
                if (choices[i] < 0) continue;
                SweepCellCandidate candidate = candidates[i][choices[i]];
                foreach (var surfaceId in candidate.TargetSurfaceIds)
                {
                    int twinId;
                    int neighbour;
                    int gmshSurfaceId = FeMesh.GmshTopologyId(surfaceId, sweptParts[i].PartId);
                    if (!twinSurfaceIds.TryGetValue(gmshSurfaceId, out twinId) ||
                        !partIdCell.TryGetValue(FeMesh.GetPartIdFromGmshTopologyId(twinId), out neighbour) ||
                        choices[neighbour] < 0 || fedCells[i].Contains(neighbour)) continue;
                    int twinSurfaceId = FeMesh.GetItemIdFromGmshTopologyId(twinId);
                    if (!candidates[neighbour][choices[neighbour]].SourceSurfaceIds.Contains(twinSurfaceId)) continue;
                    fedCells[i].Add(neighbour);
                    numOfFeeders[neighbour]++;
                }
            }
            List<int> order = new List<int>();
            bool[] ordered = new bool[numOfCells];
            int numOfSwept = choices.Count(choice => choice >= 0);
            while (order.Count < numOfSwept)
            {
                int next = -1;
                for (int i = 0; i < numOfCells && next < 0; i++)
                {
                    if (choices[i] >= 0 && !ordered[i] && numOfFeeders[i] == 0) next = i;
                }
                // A ring is entered at its first cell
                for (int i = 0; i < numOfCells && next < 0; i++)
                {
                    if (choices[i] >= 0 && !ordered[i]) next = i;
                }
                //
                ordered[next] = true;
                order.Add(next);
                foreach (var fed in fedCells[next]) numOfFeeders[fed]--;
            }
            // A block feeding a cell meshed before it closes a ring, which takes every target of it to be a source
            // of a block meshed before it
            HashSet<int> earlierSourceIds = new HashSet<int>();
            for (int k = 0; k < order.Count; k++)
            {
                int cell = order[k];
                SweepCellCandidate candidate = candidates[cell][choices[cell]];
                int partId = sweptParts[cell].PartId;
                List<int> fedBackCells = fedCells[cell].Where(fed => order.IndexOf(fed) < k).ToList();
                if (fedBackCells.Count > 0 &&
                    !candidate.TargetSurfaceIds.All(id =>
                        earlierSourceIds.Contains(FeMesh.GmshTopologyId(id, partId)) ||
                        IsSharedSourceOfEarlier(id, partId, twinSurfaceIds, earlierSourceIds)))
                {
                    fedBackCells.Insert(0, cell);
                    unorderedCells = fedBackCells.ToArray();
                    return null;
                }
                foreach (var id in candidate.SourceSurfaceIds) earlierSourceIds.Add(FeMesh.GmshTopologyId(id, partId));
            }
            return order;
        }
        private static bool IsSharedSourceOfEarlier(int surfaceId, int partId, Dictionary<int, int> twinSurfaceIds,
                                                    HashSet<int> earlierSourceIds)
        {
            int twinId;
            return twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(surfaceId, partId), out twinId) &&
                   earlierSourceIds.Contains(twinId);
        }
        /// <summary>
        /// Why a cell left to tetrahedra is not swept although it has ways of its own: the first surface on which its
        /// best way does not agree with the way a neighbour is swept.
        /// </summary>
        private string GetCompoundSweepConflict(GeometryPart[] sweptParts, int cell,
                                                List<SweepCellCandidate>[] candidates, int[] choices,
                                                Dictionary<int, int> twinSurfaceIds, Dictionary<int, int> partIdCell,
                                                double tolerance)
        {
            if (candidates[cell].Count == 0) return null;
            //
            GeometryPart part = sweptParts[cell];
            foreach (var candidate in candidates[cell].OrderByDescending(c => c.Score))
            {
                for (int surfaceId = 0; surfaceId < part.Visualization.FaceCount; surfaceId++)
                {
                    int twinId;
                    int neighbour;
                    if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(surfaceId, part.PartId), out twinId) ||
                        !partIdCell.TryGetValue(FeMesh.GetPartIdFromGmshTopologyId(twinId), out neighbour)) continue;
                    //
                    GeometryPart neighbourPart = sweptParts[neighbour];
                    int neighbourSurfaceId = FeMesh.GetItemIdFromGmshTopologyId(twinId);
                    SweepCellCandidate neighbourCandidate = choices[neighbour] >= 0
                                                            ? candidates[neighbour][choices[neighbour]] : null;
                    bool firstSplit;
                    SweepCellConflictEnum conflict = SweepCellCandidate.GetConflict(candidate, surfaceId,
                                                                                    neighbourCandidate,
                                                                                    neighbourSurfaceId, tolerance,
                                                                                    out firstSplit);
                    string surfaceName = GetSweepSurfaceName(part, surfaceId) + ", shared with the part " +
                                         neighbourPart.Name;
                    switch (conflict)
                    {
                        case SweepCellConflictEnum.None:
                            continue;
                        case SweepCellConflictEnum.TargetSplitInside:
                            GeometryPart splitPart = firstSplit ? part : neighbourPart;
                            SweepCellCandidate splitCandidate = firstSplit ? candidate : neighbourCandidate;
                            int splitSurfaceId = firstSplit ? surfaceId : neighbourSurfaceId;
                            int edgeId = splitCandidate.GetEdgeSplittingTarget(splitSurfaceId);
                            return "The " + GetSweepSurfaceName(splitPart, splitSurfaceId) + ", shared by the parts " +
                                   part.Name + " and " + neighbourPart.Name + ", ends inside the target of the part " +
                                   splitPart.Name + " at " + GetSweepEdgeName(splitPart, edgeId) + ". The part " +
                                   splitPart.Name + " is not a prism, so the mesh swept onto its target cannot " +
                                   "follow the split.";
                        case SweepCellConflictEnum.TargetsMeet:
                            return "The sweeps of the parts " + part.Name + " and " + neighbourPart.Name +
                                   " both end on the " + surfaceName + ".";
                        case SweepCellConflictEnum.CapAgainstWall:
                            return "The " + surfaceName + ", is a source or a target of one part and a wall of the " +
                                   "other.";
                        case SweepCellConflictEnum.WallSidesDiffer:
                            return "The parts " + part.Name + " and " + neighbourPart.Name + " would be swept along " +
                                   "their shared wall, the " + GetSweepSurfaceName(part, surfaceId) + ", in " +
                                   "different directions.";
                        default:
                            return "The " + surfaceName + ", is not a source, a target or a wall of the sweep.";
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// The source selections tried on a part: every surface on its own, every group of surfaces that
        /// continue one another smoothly, which is what one click of the angle picker selects, and every
        /// surface together with the neighbours that lean on it at a shallow angle. A bent plate is swept
        /// from a front face made of a plane, the cylinder of the bend and a plane again, and the extension
        /// over coplanar surfaces does not reach across the bend. The leaning neighbours are what carries a
        /// chamfer around a hole into the source, where the sweep meshes it on the face itself; left in the
        /// target the chamfer is flattened onto the face the projection lands on and is lost. They are taken
        /// one face out and not followed on, so the chamfer joins the end face it cuts without the bore it
        /// runs into joining with it, and a chamfer that leans more towards the bore than towards the end
        /// face reaches no source at all.
        /// </summary>
        private List<HashSet<int>> GetSeedSurfaceIds(GeometryPart part)
        {
            List<HashSet<int>> seedSurfaceIds = new List<HashSet<int>>();
            for (int surfaceId = 0; surfaceId < part.Visualization.FaceEdgeIds.Length; surfaceId++)
                seedSurfaceIds.Add(new HashSet<int>() { surfaceId });
            //
            foreach (var group in _geometry.GetSmoothSurfaceIdGroups(part, Globals.EdgeAngle))
            {
                if (group.Count > 1) seedSurfaceIds.Add(group);
            }
            //
            Dictionary<int, HashSet<int>> leaningSurfaceIds = new Dictionary<int, HashSet<int>>();
            foreach (var entry in _geometry.GetSurfaceIdPairAngles(part))
            {
                if (entry.Value[0] > Globals.SweepSourceAngle) continue;
                AddLeaningSurfaceId(leaningSurfaceIds, entry.Key[0], entry.Key[1]);
                AddLeaningSurfaceId(leaningSurfaceIds, entry.Key[1], entry.Key[0]);
            }
            foreach (var entry in leaningSurfaceIds)
            {
                entry.Value.Add(entry.Key);
                if (!seedSurfaceIds.Any(ids => ids.SetEquals(entry.Value))) seedSurfaceIds.Add(entry.Value);
            }
            return seedSurfaceIds;
        }
        private static void AddLeaningSurfaceId(Dictionary<int, HashSet<int>> leaningSurfaceIds, int surfaceId,
                                                int neighbourId)
        {
            HashSet<int> neighbourIds;
            if (leaningSurfaceIds.TryGetValue(surfaceId, out neighbourIds)) neighbourIds.Add(neighbourId);
            else leaningSurfaceIds.Add(surfaceId, new HashSet<int>() { neighbourId });
        }
        /// <summary>
        /// Ranks a set of sweep blocks, complete or partial. A source with many edges carries the features of the cross
        /// section, so sweeping it meshes them once in 2D and extrudes them; a wall that is neither planar
        /// nor cylindrical strains the projection of the target layer.
        /// </summary>
        /// <summary>
        /// How good a sweep the blocks make, higher being better. A source with more edges gives the sweep
        /// more of the geometry to follow; a side surface that is neither a plane nor a cylinder costs, since
        /// its structured grid is the least reliable; a sweep line that meets the target at a grazing angle
        /// costs as much, because the elements at its end are flattened - a sweep along a body onto a face
        /// that runs along the sweep is such a case; and a target that is an offset of the source - parallel
        /// planes, coaxial cylinders, a body of constant thickness - earns a bonus, since every sweep line
        /// then has the same length and the layers are the same shape as the source. Among the rest a larger
        /// source is preferred a little, so that a body is swept through its thin direction, where the layers
        /// are few and the source carries most of the mesh. A hole in the source earns a bonus of its own,
        /// since a face the bores run into is the one that carries the cross section, and a source made of
        /// planes earns a small one, its mesh being the one that projects onto the layers most faithfully.
        /// </summary>
        private double ScoreSweepBlocks(SweepBlocks blocks, Dictionary<int, GeomFaceType> faceTypes, SweepPartMaps maps)
        {
            int partId;
            double score = 0;
            GeomFaceType faceType;
            GeometryPart part;
            VisualizationData vis;
            HashSet<int> sourceEdgeIds;
            for (int i = 0; i < blocks.SourceSurfaceIds.Length; i++)
            {
                partId = FeMesh.GetPartIdFromGmshTopologyId(blocks.SourceSurfaceIds[i][0]);
                part = (GeometryPart)_geometry.GetPartFromId(partId);
                vis = part.Visualization;
                int[] sourceSurfaceIds = blocks.SourceSurfaceIds[i].Select(id => FeMesh.GetItemIdFromGmshTopologyId(id))
                                                                   .ToArray();
                int[] targetSurfaceIds = blocks.TargetSurfaceIds[i].Select(id => FeMesh.GetItemIdFromGmshTopologyId(id))
                                                                   .ToArray();
                //
                sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds);
                score += sourceEdgeIds.Count;
                score += 3 * Math.Max(0, vis.GetFreeEdgeLoops(sourceSurfaceIds, maps.GetVertexIdEdgeIds(part)).Length - 1);
                //
                bool planarSource = true;
                foreach (var surfaceId in blocks.SourceSurfaceIds[i])
                {
                    if (GetSweepFaceType(vis, surfaceId, faceTypes) != GeomFaceType.Plane) planarSource = false;
                }
                if (planarSource) score += 2;
                //
                foreach (var surfaceId in blocks.SideSurfaceIds[i])
                {
                    faceType = GetSweepFaceType(vis, surfaceId, faceTypes);
                    if (faceType != GeomFaceType.Plane && faceType != GeomFaceType.Cylinder) score -= 4;
                }
                //
                score -= 4 * CountGrazingSweepLines(part, sourceEdgeIds, targetSurfaceIds, maps);
                if (IsTargetAnOffsetOfSource(part, sourceSurfaceIds, targetSurfaceIds, faceTypes)) score += 6;
                if (vis.FaceAreas != null && vis.FaceAreas.Sum() > 0)
                    score += 2 * sourceSurfaceIds.Sum(id => vis.FaceAreas[id]) / vis.FaceAreas.Sum();
            }
            return score;
        }
        /// <summary>
        /// The number of edge chains running from the source that end without meeting a target surface head
        /// on: at a vertex that lies on no target surface, or at one where every target surface there makes
        /// an angle of more than 60 degrees with the chain.
        /// </summary>
        private int CountGrazingSweepLines(GeometryPart part, HashSet<int> sourceEdgeIds, int[] targetSurfaceIds,
                                           SweepPartMaps maps)
        {
            int count = 0;
            int lastEdgeId;
            int lastVertexId;
            VisualizationData vis = part.Visualization;
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = maps.GetVertexIdEdgeIds(part);
            HashSet<int> chainEdgeIds = new HashSet<int>();
            foreach (var vertexId in vis.GetVertexNodeIdsForEdgeIds(sourceEdgeIds.ToArray()).OrderBy(id => id))
            {
                foreach (var edgeId in vertexIdEdgeIds[vertexId].OrderBy(id => id))
                {
                    if (sourceEdgeIds.Contains(edgeId) || chainEdgeIds.Contains(edgeId)) continue;
                    FollowSweepChain(vis, edgeId, vertexId, sourceEdgeIds, vertexIdEdgeIds, _sweepChainCosine,
                                     chainEdgeIds, out lastEdgeId, out lastVertexId, out _);
                    Vec3D direction = -1 * GetSweepEdgeTangent(vis, lastEdgeId, lastVertexId);
                    if (direction.Len == 0) continue;
                    //
                    double bestCosine = 0;
                    foreach (var surfaceId in targetSurfaceIds)
                    {
                        Vec3D normal = GetFaceNormalAtVertex(vis, surfaceId, lastVertexId);
                        if (normal == null) continue;
                        double cosine = Math.Abs(Vec3D.DotProduct(direction, normal)) / (direction.Len * normal.Len);
                        bestCosine = Math.Max(bestCosine, cosine);
                    }
                    if (bestCosine < 0.5) count++;
                }
            }
            return count;
        }
        /// <summary>
        /// The normal of a surface at one of its vertices, from a cell of the surface at that vertex, or null
        /// when the vertex is not on the surface.
        /// </summary>
        private Vec3D GetFaceNormalAtVertex(VisualizationData vis, int surfaceId, int vertexNodeId)
        {
            foreach (var cellId in vis.CellIdsByFace[surfaceId])
            {
                int[] cell = vis.Cells[cellId];
                if (cell.Length < 3 || !cell.Contains(vertexNodeId)) continue;
                Vec3D a = new Vec3D(_geometry.Nodes[cell[0]].Coor);
                Vec3D b = new Vec3D(_geometry.Nodes[cell[1]].Coor);
                Vec3D c = new Vec3D(_geometry.Nodes[cell[2]].Coor);
                Vec3D normal = Vec3D.CrossProduct(b - a, c - a);
                if (normal.Len > 0) return normal;
            }
            return null;
        }
        /// <summary>
        /// Whether every target surface is an offset of the source: all of one type, planes parallel to the
        /// source planes or cylinders on the axis of the source cylinders, the axes read from the CAD data.
        /// </summary>
        private bool IsTargetAnOffsetOfSource(GeometryPart part, int[] sourceSurfaceIds, int[] targetSurfaceIds,
                                              Dictionary<int, GeomFaceType> faceTypes)
        {
            VisualizationData vis = part.Visualization;
            GeomFaceType type = GetSweepFaceType(vis, FeMesh.GmshTopologyId(sourceSurfaceIds[0], part.PartId), faceTypes);
            if (type != GeomFaceType.Plane && type != GeomFaceType.Cylinder) return false;
            foreach (var surfaceId in sourceSurfaceIds.Concat(targetSurfaceIds))
            {
                if (GetSweepFaceType(vis, FeMesh.GmshTopologyId(surfaceId, part.PartId), faceTypes) != type) return false;
            }
            //
            if (type == GeomFaceType.Plane)
            {
                Vec3D reference = null;
                foreach (var surfaceId in sourceSurfaceIds.Concat(targetSurfaceIds))
                {
                    if (vis.CellIdsByFace[surfaceId].Length == 0) return false;
                    int[] cell = vis.Cells[vis.CellIdsByFace[surfaceId][0]];
                    Vec3D origin = new Vec3D(_geometry.Nodes[cell[0]].Coor);
                    Vec3D normal = Vec3D.CrossProduct(new Vec3D(_geometry.Nodes[cell[1]].Coor) - origin,
                                                      new Vec3D(_geometry.Nodes[cell[2]].Coor) - origin);
                    if (normal.Len == 0) return false;
                    if (reference == null) reference = normal;
                    else if (Math.Abs(Vec3D.DotProduct(reference, normal)) < 0.999 * reference.Len * normal.Len) return false;
                }
                return true;
            }
            // Cylinders: one axis line for all of them
            if (part.GeometryMetadata == null) return false;
            double[] values;
            Vec3D axisPoint = null;
            Vec3D axisDirection = null;
            foreach (var surfaceId in sourceSurfaceIds.Concat(targetSurfaceIds))
            {
                GeometryMetadataEntity face = part.GeometryMetadata.GetEntity("face", surfaceId + 1);
                if (face == null || !face.TryGetNumbers("axis", out values) || values.Length != 6) return false;
                Vec3D point = new Vec3D(values[0], values[1], values[2]);
                Vec3D direction = new Vec3D(values[3], values[4], values[5]);
                if (direction.Len == 0) return false;
                if (axisDirection == null)
                {
                    axisPoint = point;
                    axisDirection = direction;
                    continue;
                }
                if (Math.Abs(Vec3D.DotProduct(axisDirection, direction)) < (1 - 1E-6) * axisDirection.Len * direction.Len)
                    return false;
                // The point of this axis must lie on the first one
                Vec3D offset = point - axisPoint;
                double along = Vec3D.DotProduct(offset, axisDirection) / axisDirection.Len;
                double across = Math.Sqrt(Math.Max(0, offset.Len * offset.Len - along * along));
                if (across > 1E-6 * Math.Max(1, offset.Len)) return false;
            }
            return true;
        }
        private GeomFaceType GetSweepFaceType(VisualizationData vis, int gmshTopologyId, Dictionary<int, GeomFaceType> faceTypes)
        {
            GeomFaceType faceType;
            if (faceTypes.TryGetValue(gmshTopologyId, out faceType)) return faceType;
            //
            int surfaceId = FeMesh.GetItemIdFromGmshTopologyId(gmshTopologyId);
            if (vis.FaceTypes != null && surfaceId < vis.FaceTypes.Length) faceType = vis.FaceTypes[surfaceId];
            else faceType = FaceTypeRecognition.GetFaceData(vis, _geometry.Nodes, surfaceId).Type;
            faceTypes.Add(gmshTopologyId, faceType);
            return faceType;
        }
        /// <summary>
        /// Splits the swept parts into one-to-one sweep blocks and stores them on the item in gmsh numbering.
        /// A single part is one block. The sub-parts of a compound part form a tree of blocks: a sub-part
        /// sharing a source surface is swept away from that same meshed surface, a sub-part sharing a
        /// target surface is swept next, from the mesh the blocks before it leave on the shared surfaces,
        /// and a sub-part sharing a side surface is swept alongside, from the surfaces that continue the
        /// source across the shared wall.
        ///
        /// A propagation stopped only because a sub-part was never reached still returns the blocks of the
        /// sub-parts it did sweep, with the error set, so that a selection that may leave sub-parts out still
        /// sweeps those. Every other refusal returns nothing.
        ///
        /// A neighbour swept alongside a block across their shared wall is only one way to sweep it: the wall
        /// is a wall to a neighbour swept along its other edges just as well. So when the sweep carried on
        /// from such a neighbour fails further on - the bar beside a plate swept through its thickness, whose
        /// cap is shared with a bar that leans along a chamfer and can only be swept across it - the neighbour
        /// is set aside and the propagation repeated without it. It is then left unreached, for a source of its own,
        /// the way a neighbour that cannot be swept alongside at all is left.
        ///
        /// A closed ring of sub-parts, each of which can only be swept from one of its neighbours to the next,
        /// fails that way: a source shared by two of them sends one sweep each way around the ring, and the two
        /// meet in the sub-part opposite, which would need a source at both ends. Such a propagation is repeated with
        /// the sweep sent one way round - the neighbours of a selected source are not swept away from it - and
        /// the ring is closed on it: the last block of the ring ends on the selected source, which is already
        /// meshed, and the mesher sweeps that block between two meshed caps. The repeated propagation is kept only
        /// when it did close a ring.
        /// </summary>
        private SweepBlocks CreateSweepBlocks(GeometryPart[] sweptParts,
                                              Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                              Dictionary<int, int> twinSurfaceIds, Dictionary<int, int> twinEdgeIds,
                                              SweepPartMaps maps, out string error, out string errorPartName,
                                              out HashSet<int> sweptPartIds, out bool onlyUnreached)
        {
            SweepBlocks blocks = CreateSweepBlocks(sweptParts, partIdSelectedSurfaceIds, twinSurfaceIds, twinEdgeIds, maps,
                                                   false, out error, out errorPartName, out sweptPartIds,
                                                   out onlyUnreached, out _);
            if (error == null || onlyUnreached) return blocks;
            //
            bool closedRing;
            bool ringOnlyUnreached;
            string ringError;
            string ringErrorPartName;
            HashSet<int> ringSweptPartIds;
            SweepBlocks ringBlocks = CreateSweepBlocks(sweptParts, partIdSelectedSurfaceIds, twinSurfaceIds, twinEdgeIds,
                                                       maps, true, out ringError, out ringErrorPartName,
                                                       out ringSweptPartIds, out ringOnlyUnreached, out closedRing);
            if (!closedRing || (ringError != null && !ringOnlyUnreached)) return blocks;
            //
            error = ringError;
            errorPartName = ringErrorPartName;
            sweptPartIds = ringSweptPartIds;
            onlyUnreached = ringOnlyUnreached;
            return ringBlocks;
        }
        private SweepBlocks CreateSweepBlocks(GeometryPart[] sweptParts,
                                              Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                              Dictionary<int, int> twinSurfaceIds, Dictionary<int, int> twinEdgeIds,
                                              SweepPartMaps maps, bool sweepRingsOneWay, out string error,
                                              out string errorPartName, out HashSet<int> sweptPartIds,
                                              out bool onlyUnreached, out bool closedRing)
        {
            int failedSideNeighbourId;
            HashSet<int> setAsidePartIds = new HashSet<int>();
            while (true)
            {
                SweepBlocks blocks = CreateSweepBlocks(sweptParts, partIdSelectedSurfaceIds, twinSurfaceIds, twinEdgeIds,
                                                       maps, setAsidePartIds, sweepRingsOneWay, out error, out errorPartName,
                                                       out sweptPartIds, out onlyUnreached, out failedSideNeighbourId,
                                                       out closedRing);
                if (error == null || onlyUnreached || failedSideNeighbourId < 0 ||
                    !setAsidePartIds.Add(failedSideNeighbourId)) return blocks;
            }
        }
        private SweepBlocks CreateSweepBlocks(GeometryPart[] sweptParts,
                                              Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                              Dictionary<int, int> twinSurfaceIds, Dictionary<int, int> twinEdgeIds,
                                              SweepPartMaps maps, HashSet<int> setAsidePartIds, bool sweepRingsOneWay,
                                              out string error, out string errorPartName, out HashSet<int> sweptPartIds,
                                              out bool onlyUnreached, out int failedSideNeighbourId, out bool closedRing)
        {
            failedSideNeighbourId = -1;
            closedRing = false;
            sweptPartIds = new HashSet<int>();
            onlyUnreached = false;
            error = null;
            errorPartName = null;
            //
            int twinId;
            int partId;
            int neighbourPartId;
            GeometryPart part;
            HashSet<int> sourceSurfaceIds;
            Dictionary<int, GeometryPart> partIdPart = new Dictionary<int, GeometryPart>();
            foreach (var sweptPart in sweptParts) partIdPart.Add(sweptPart.PartId, sweptPart);
            // The surfaces and the edges two sub-parts have in common, keyed and valued by the gmsh topology id
            if (twinSurfaceIds == null)
            {
                twinSurfaceIds = new Dictionary<int, int>();
                twinEdgeIds = new Dictionary<int, int>();
                if (sweptParts.Length > 1)
                {
                    twinSurfaceIds = _geometry.GetSharedSurfaceIds(sweptParts);
                    twinEdgeIds = _geometry.GetSharedEdgeIds(sweptParts);
                }
            }
            // The source surfaces of each part, the parts waiting to be swept, and for a part that reads the
            // caps of other blocks the blocks it has to wait for
            List<int> pendingPartIds = new List<int>();
            HashSet<int> sideDerivedPartIds = new HashSet<int>();
            HashSet<int> refusedPartIds = new HashSet<int>();
            // The neighbour swept alongside a block that every part reached from it goes back to
            int rootId;
            Dictionary<int, int> partIdSideNeighbourId = new Dictionary<int, int>();
            Dictionary<int, HashSet<int>> partIdSourceSurfaceIds = new Dictionary<int, HashSet<int>>();
            Dictionary<int, HashSet<int>> partIdFeederPartIds = new Dictionary<int, HashSet<int>>();
            foreach (var entry in partIdSelectedSurfaceIds)
            {
                partIdSourceSurfaceIds.Add(entry.Key, new HashSet<int>(entry.Value));
                pendingPartIds.Add(entry.Key);
            }
            //
            int deferrals = 0;
            int[] sideSurfaceIds;
            int[] targetSurfaceIds;
            int[][][] blockLayerGroupEdgeIds;
            List<int[]> layerSideSurfaceIds;
            HashSet<int> wallEdgeIds;
            HashSet<int> feederPartIds;
            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds;
            List<int[]> blockSourceSurfaceIds = new List<int[]>();
            List<int[]> blockSideSurfaceIds = new List<int[]>();
            List<int[]> blockTargetSurfaceIds = new List<int[]>();
            List<int[][]> layerGroupEdgeIds = new List<int[][]>();
            List<double[]> blockArrowStarts = new List<double[]>();
            List<double[]> blockArrowDirections = new List<double[]>();
            //
            while (pendingPartIds.Count > 0)
            {
                partId = pendingPartIds[0];
                pendingPartIds.RemoveAt(0);
                // A part refused as a neighbour swept alongside is not swept alongside again: it is given a
                // source of its own in a later round. Without this it is queued again by the next neighbour
                // that shares a wall with it, and two parts that refuse each other queue each other for ever.
                // A source or a target shared with a block still reaches it, and it is swept from that
                if (refusedPartIds.Contains(partId)) continue;
                // A block reads the caps of the blocks feeding it, so it is swept after all of them
                partIdFeederPartIds.TryGetValue(partId, out feederPartIds);
                if (feederPartIds != null && !feederPartIds.IsSubsetOf(sweptPartIds))
                {
                    if (++deferrals > pendingPartIds.Count + 1)
                    {
                        error = "The order in which the parts are swept could not be determined.";
                        if (partIdSideNeighbourId.TryGetValue(partId, out rootId)) failedSideNeighbourId = rootId;
                        return null;
                    }
                    pendingPartIds.Add(partId);
                    continue;
                }
                deferrals = 0;
                //
                part = partIdPart[partId];
                sourceSurfaceIds = partIdSourceSurfaceIds[partId];
                surfaceIdSurfaceNeighbourIds = maps.GetSurfaceIdSurfaceNeighbourIds(part);
                // The surfaces lying in the plane of the source are swept from as well. A source the sweep derived
                // for this part is the surface it is fed through and nothing more, and a chamfer or a round at that
                // end of the block is left out of it. Every propagation reaching the source grows it the same way
                bool derived = !partIdSelectedSurfaceIds.ContainsKey(partId);
                sourceSurfaceIds.UnionWith(maps.GetExtendedSourceSurfaceIds(part, sourceSurfaceIds, derived, () =>
                {
                    HashSet<int> extendedSourceSurfaceIds = new HashSet<int>(sourceSurfaceIds);
                    ExtendSourceOverCoplanarSurfaces(part, extendedSourceSurfaceIds, surfaceIdSurfaceNeighbourIds,
                                                     maps);
                    if (derived)
                    {
                        ExtendDerivedSourceOverLeaningSurfaces(part, extendedSourceSurfaceIds,
                                                               surfaceIdSurfaceNeighbourIds, maps);
                    }
                    return extendedSourceSurfaceIds;
                }));
                // A neighbour sharing a source surface is swept away from the same meshed surface - unless it
                // is one of the blocks feeding this one, whose target the shared surface is, or the sweep is
                // sent one way around a ring, which the neighbour then closes on the selected source
                foreach (var surfaceId in sourceSurfaceIds.ToArray())
                {
                    if (sweepRingsOneWay && partIdSelectedSurfaceIds.ContainsKey(partId)) break;
                    if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(surfaceId, partId), out twinId)) continue;
                    //
                    neighbourPartId = FeMesh.GetPartIdFromGmshTopologyId(twinId);
                    if (sweptPartIds.Contains(neighbourPartId)) continue;
                    if (feederPartIds != null && feederPartIds.Contains(neighbourPartId)) continue;
                    if (refusedPartIds.Remove(neighbourPartId)) sideDerivedPartIds.Remove(neighbourPartId);
                    AddSourceSurface(neighbourPartId, FeMesh.GetItemIdFromGmshTopologyId(twinId), partIdSourceSurfaceIds,
                                     pendingPartIds, false);
                    if (partIdSideNeighbourId.TryGetValue(partId, out rootId) &&
                        !partIdSideNeighbourId.ContainsKey(neighbourPartId)) partIdSideNeighbourId.Add(neighbourPartId, rootId);
                }
                //
                // A block is validated once per source and read again by every propagation reaching the source
                SweepBlockValidation validation = maps.GetSweepBlockValidation(part, sourceSurfaceIds, twinSurfaceIds != null,
                    () => ValidateSweepBlock(part, sourceSurfaceIds, surfaceIdSurfaceNeighbourIds, maps, twinSurfaceIds));
                error = validation.Error;
                validation.CopyTo(out sideSurfaceIds, out targetSurfaceIds, out layerSideSurfaceIds, out int[] directionEdgeIds);
                if (error != null)
                {
                    // A neighbour that cannot be swept alongside the block across their shared wall is left
                    // unreached, to be given a source of its own, rather than failing the whole sweep
                    if (sideDerivedPartIds.Contains(partId))
                    {
                        error = null;
                        partIdSourceSurfaceIds.Remove(partId);
                        refusedPartIds.Add(partId);
                        continue;
                    }
                    errorPartName = part.Name;
                    if (partIdSideNeighbourId.TryGetValue(partId, out rootId)) failedSideNeighbourId = rootId;
                    return null;
                }
                // A neighbour across a side surface is swept alongside this block, from the surfaces that
                // continue the source across the shared wall. It goes first, so that a block both of them
                // feed learns of both before it is swept. The shared wall ties the layer counts together.
                foreach (var sideSurfaceId in sideSurfaceIds)
                {
                    if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(sideSurfaceId, partId), out twinId)) continue;
                    //
                    neighbourPartId = FeMesh.GetPartIdFromGmshTopologyId(twinId);
                    if (partIdSourceSurfaceIds.ContainsKey(neighbourPartId)) continue;
                    if (refusedPartIds.Contains(neighbourPartId) || setAsidePartIds.Contains(neighbourPartId)) continue;
                    error = AddSideNeighbourSource(part, sourceSurfaceIds, sideSurfaceId, partIdPart[neighbourPartId],
                                                   FeMesh.GetItemIdFromGmshTopologyId(twinId), twinEdgeIds,
                                                   partIdSourceSurfaceIds, pendingPartIds);
                    if (error != null) return null;
                    if (partIdSourceSurfaceIds.ContainsKey(neighbourPartId))
                    {
                        sideDerivedPartIds.Add(neighbourPartId);
                        partIdSideNeighbourId[neighbourPartId] = neighbourPartId;
                    }
                }
                // A neighbour across a target surface is swept next, from the mesh this block leaves there.
                // That mesh follows the imprint of the shared surface where the imprint is a wall edge of this
                // block. A shared surface that ends inside the target has its boundary nodes meshed by gmsh,
                // and the cap follows them only when the block is a prism: the mesher then carries the split
                // to the source and pins the sweep lines between the two. Any other block is refused.
                wallEdgeIds = validation.GetWallEdgeIds(() =>
                    part.Visualization.GetEdgeIdsForSurfaceIds(sideSurfaceIds));
                bool prismatic = validation.IsPrismatic(() =>
                    IsPrismaticSweepBlock(part, sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds));
                foreach (var targetSurfaceId in targetSurfaceIds)
                {
                    if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(targetSurfaceId, partId), out twinId)) continue;
                    //
                    neighbourPartId = FeMesh.GetPartIdFromGmshTopologyId(twinId);
                    bool closesRing = false;
                    if (sweptPartIds.Contains(neighbourPartId) || partIdSelectedSurfaceIds.ContainsKey(neighbourPartId))
                    {
                        closesRing = sweepRingsOneWay &&
                                     ClosesSweepRing(partId, targetSurfaceIds, neighbourPartId, twinSurfaceIds,
                                                     partIdSelectedSurfaceIds, partIdSourceSurfaceIds, sweptPartIds);
                        if (!closesRing)
                        {
                            error = "The part " + partIdPart[neighbourPartId].Name + " is reached by the sweep from two " +
                                    "sides, the second time across " + GetSweepSurfaceName(part, targetSurfaceId) + ".";
                            if (partIdSideNeighbourId.TryGetValue(partId, out rootId)) failedSideNeighbourId = rootId;
                            return null;
                        }
                    }
                    foreach (var edgeId in part.Visualization.FaceEdgeIds[targetSurfaceId])
                    {
                        if (wallEdgeIds.Contains(edgeId) || prismatic) continue;
                        error = "The " + GetSweepSurfaceName(part, targetSurfaceId) + ", shared by the parts " + part.Name +
                                " and " + partIdPart[neighbourPartId].Name + ", ends inside the target of the part " +
                                part.Name + " at " + GetSweepEdgeName(part, edgeId) + ". The part " + part.Name +
                                " is not a prism, so the mesh swept through it cannot follow the split and continue " +
                                "into the part " + partIdPart[neighbourPartId].Name + ". Select the shared surface as " +
                                "the source instead, so that both parts are swept away from it.";
                        if (partIdSideNeighbourId.TryGetValue(partId, out rootId)) failedSideNeighbourId = rootId;
                        return null;
                    }
                    if (closesRing)
                    {
                        closedRing = true;
                        continue;
                    }
                    if (refusedPartIds.Remove(neighbourPartId)) sideDerivedPartIds.Remove(neighbourPartId);
                    AddSourceSurface(neighbourPartId, FeMesh.GetItemIdFromGmshTopologyId(twinId), partIdSourceSurfaceIds,
                                     pendingPartIds, false);
                    if (partIdSideNeighbourId.TryGetValue(partId, out rootId) &&
                        !partIdSideNeighbourId.ContainsKey(neighbourPartId)) partIdSideNeighbourId.Add(neighbourPartId, rootId);
                    if (!partIdFeederPartIds.TryGetValue(neighbourPartId, out feederPartIds))
                    {
                        feederPartIds = new HashSet<int>();
                        partIdFeederPartIds.Add(neighbourPartId, feederPartIds);
                    }
                    feederPartIds.Add(partId);
                }
                // Gmsh numbering. Walls found by following the edges from the source have no rings, and the
                // edges following the sweep direction are the chains themselves, in one group
                blockLayerGroupEdgeIds = validation.GetLayerGroupEdgeIds(() =>
                    directionEdgeIds != null ? new int[][][] { new int[][] { directionEdgeIds } }
                                             : GetDirectionEdges(part.Visualization, surfaceIdSurfaceNeighbourIds,
                                                                 sideSurfaceIds, layerSideSurfaceIds));
                // Every block gets an arrow of its own, from a source vertex along one of its layer edges
                double[] arrowStart;
                double[] arrowDirection;
                validation.GetArrow(() =>
                {
                    double[] start;
                    double[] direction;
                    GetBlockArrow(part, sourceSurfaceIds, blockLayerGroupEdgeIds, out start, out direction);
                    return new double[][] { start, direction };
                }, out arrowStart, out arrowDirection);
                blockArrowStarts.Add(arrowStart);
                blockArrowDirections.Add(arrowDirection);
                //
                blockSourceSurfaceIds.Add(ToGmshTopologyIds(sourceSurfaceIds, partId));
                blockSideSurfaceIds.Add(ToGmshTopologyIds(sideSurfaceIds, partId));
                blockTargetSurfaceIds.Add(ToGmshTopologyIds(targetSurfaceIds, partId));
                //
                for (int i = 0; i < blockLayerGroupEdgeIds.Length; i++)
                {
                    for (int j = 0; j < blockLayerGroupEdgeIds[i].Length; j++)
                        blockLayerGroupEdgeIds[i][j] = ToGmshTopologyIds(blockLayerGroupEdgeIds[i][j], partId);
                }
                layerGroupEdgeIds.AddRange(blockLayerGroupEdgeIds);
                //
                sweptPartIds.Add(partId);
            }
            // A compound part is meshed as a whole, so every sub-part must be swept
            foreach (var sweptPart in sweptParts)
            {
                if (sweptPartIds.Contains(sweptPart.PartId)) continue;
                //
                onlyUnreached = true;
                error = "The part " + sweptPart.Name + " is not reached by the sweep. Select a source surface on it " +
                        "as well.";
                break;
            }
            // The blocks of the parts that were reached are returned even when a part was not, so that a selection
            // that may leave parts out still sweeps them. The error is what says it is partial
            return new SweepBlocks(blockSourceSurfaceIds.ToArray(), blockSideSurfaceIds.ToArray(),
                                   blockTargetSurfaceIds.ToArray(), layerGroupEdgeIds.ToArray(),
                                   blockArrowStarts.ToArray(), blockArrowDirections.ToArray());
        }
        /// <summary>
        /// The arrow of one block: it starts at a source vertex and follows the first layer edge of the block
        /// towards the target.
        /// </summary>
        private void GetBlockArrow(GeometryPart part, HashSet<int> sourceSurfaceIds, int[][][] layerGroupEdgeIds,
                                   out double[] arrowStart, out double[] arrowDirection)
        {
            arrowStart = null;
            arrowDirection = null;
            VisualizationData vis = part.Visualization;
            HashSet<int> sourceVertexIds = vis.GetVertexNodeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            foreach (var layerGroups in layerGroupEdgeIds)
            {
                foreach (var edgeIds in layerGroups)
                {
                    foreach (var edgeId in edgeIds)
                    {
                        int startNodeId;
                        Vec3D direction = GetEdgeDirectionFromSource(vis, edgeId, sourceVertexIds, out startNodeId);
                        if (direction == null) continue;
                        // The exploded view moves the nodes of the part, and the arrow is drawn on the
                        // offset the part carries, so the start is kept without it
                        double[] offset = part.Offset != null ? part.Offset : new double[3];
                        double[] coor = _geometry.Nodes[startNodeId].Coor;
                        arrowStart = new double[] { coor[0] - offset[0], coor[1] - offset[1], coor[2] - offset[2] };
                        arrowDirection = direction.Coor;
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// The direction of a layer edge away from the source, and the source vertex it starts at, or null
        /// when the edge does not start on the source.
        /// </summary>
        private Vec3D GetEdgeDirectionFromSource(VisualizationData vis, int edgeId, HashSet<int> sourceVertexIds,
                                                 out int startNodeId)
        {
            startNodeId = -1;
            int[] edgeNodes = vis.GetNodeIdsForEdgeId(edgeId).Intersect(vis.VertexNodeIds).ToArray();
            if (edgeNodes.Length != 2) return null;
            // First node must be on the source
            if (!sourceVertexIds.Contains(edgeNodes[0])) (edgeNodes[0], edgeNodes[1]) = (edgeNodes[1], edgeNodes[0]);
            if (!sourceVertexIds.Contains(edgeNodes[0])) return null;
            startNodeId = edgeNodes[0];
            // Find the first edge cell for the direction
            int edgeCellId;
            int[] edgeCell;
            for (int i = 0; i < vis.EdgeCellIdsByEdge[edgeId].Length; i++)
            {
                edgeCellId = vis.EdgeCellIdsByEdge[edgeId][i];
                edgeCell = vis.EdgeCells[edgeCellId];
                if (edgeNodes[0] == edgeCell[0])
                {
                    edgeNodes[1] = edgeCell[1];
                    break;
                }
                else if (edgeNodes[0] == edgeCell[1])
                {
                    edgeNodes[1] = edgeCell[0];
                    break;
                }
            }
            return _geometry.ComputeDirectionFromEdgeCellIndices(edgeNodes, edgeNodes[0]);
        }
        private static void AddSourceSurface(int partId, int surfaceId, Dictionary<int, HashSet<int>> partIdSourceSurfaceIds,
                                             List<int> pendingPartIds, bool sweepFirst)
        {
            HashSet<int> sourceSurfaceIds;
            if (!partIdSourceSurfaceIds.TryGetValue(partId, out sourceSurfaceIds))
            {
                sourceSurfaceIds = new HashSet<int>();
                partIdSourceSurfaceIds.Add(partId, sourceSurfaceIds);
                if (sweepFirst) pendingPartIds.Insert(0, partId);
                else pendingPartIds.Add(partId);
            }
            sourceSurfaceIds.Add(surfaceId);
        }
        /// <summary>
        /// Whether a block closes a ring on a neighbour: the neighbour is a selected source block that is already
        /// swept, and the targets of this block are the whole of its source and nothing else, so that the cap this
        /// block would leave is the mesh already on the neighbour's source.
        /// </summary>
        private static bool ClosesSweepRing(int partId, int[] targetSurfaceIds, int neighbourPartId,
                                            Dictionary<int, int> twinSurfaceIds,
                                            Dictionary<int, HashSet<int>> partIdSelectedSurfaceIds,
                                            Dictionary<int, HashSet<int>> partIdSourceSurfaceIds, HashSet<int> sweptPartIds)
        {
            int twinId;
            HashSet<int> neighbourSourceIds;
            if (!sweptPartIds.Contains(neighbourPartId) || !partIdSelectedSurfaceIds.ContainsKey(neighbourPartId)) return false;
            if (!partIdSourceSurfaceIds.TryGetValue(neighbourPartId, out neighbourSourceIds)) return false;
            //
            HashSet<int> sharedIds = new HashSet<int>();
            foreach (var targetSurfaceId in targetSurfaceIds)
            {
                if (!twinSurfaceIds.TryGetValue(FeMesh.GmshTopologyId(targetSurfaceId, partId), out twinId)) return false;
                if (FeMesh.GetPartIdFromGmshTopologyId(twinId) != neighbourPartId) return false;
                sharedIds.Add(FeMesh.GetItemIdFromGmshTopologyId(twinId));
            }
            return sharedIds.SetEquals(neighbourSourceIds);
        }
        /// <summary>
        /// Gives a neighbour across a shared wall the source that continues this block's source over the
        /// wall: the neighbour's surfaces on the twins of the wall edges that this block's source touches.
        /// </summary>
        private string AddSideNeighbourSource(GeometryPart part, HashSet<int> sourceSurfaceIds, int sideSurfaceId,
                                              GeometryPart neighbour, int neighbourSideSurfaceId,
                                              Dictionary<int, int> twinEdgeIds,
                                              Dictionary<int, HashSet<int>> partIdSourceSurfaceIds, List<int> pendingPartIds)
        {
            int twinId;
            int neighbourEdgeId;
            VisualizationData vis = part.Visualization;
            VisualizationData neighbourVis = neighbour.Visualization;
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            sourceEdgeIds.IntersectWith(vis.FaceEdgeIds[sideSurfaceId]);
            //
            List<int> neighbourSourceIds = new List<int>();
            foreach (var edgeId in sourceEdgeIds)
            {
                if (!twinEdgeIds.TryGetValue(FeMesh.GmshTopologyId(edgeId, part.PartId), out twinId)) continue;
                //
                neighbourEdgeId = FeMesh.GetItemIdFromGmshTopologyId(twinId);
                for (int i = 0; i < neighbourVis.FaceEdgeIds.Length; i++)
                {
                    if (i == neighbourSideSurfaceId || !neighbourVis.FaceEdgeIds[i].Contains(neighbourEdgeId)) continue;
                    neighbourSourceIds.Add(i);
                }
            }
            // A neighbour with no surface continuing the source - a collar around the middle of a post - is not
            // swept alongside; it stays unreached and needs a source of its own, which the detection gives it
            if (neighbourSourceIds.Count == 0) return null;
            //
            foreach (var surfaceId in neighbourSourceIds)
                AddSourceSurface(neighbour.PartId, surfaceId, partIdSourceSurfaceIds, pendingPartIds, true);
            //
            return null;
        }
        /// <summary>
        /// Whether a block is a prism: planar source and target, and straight edges between them that are all
        /// one and the same vector, so that the target is the source moved by a single translation. Only such
        /// a block can carry a split of its target onto its source, which is what lets the sweep follow a
        /// shared surface that ends inside the target. The mesher measures the same thing before it carries
        /// the split, so a block accepted here and refused there would be meshed non-conformally: edges that
        /// are parallel but of different lengths, as they are on an obliquely cut end, are not enough.
        /// </summary>
        private bool IsPrismaticSweepBlock(GeometryPart part, HashSet<int> sourceSurfaceIds, int[] sideSurfaceIds,
                                           int[] targetSurfaceIds)
        {
            return IsPrismaticSweepBlock(part, sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds, out _);
        }
        /// <summary>
        /// The same, also giving the translation that moves the source onto the target.
        /// </summary>
        private bool IsPrismaticSweepBlock(GeometryPart part, HashSet<int> sourceSurfaceIds, int[] sideSurfaceIds,
                                           int[] targetSurfaceIds, out Vec3D translation)
        {
            translation = null;
            VisualizationData vis = part.Visualization;
            Dictionary<int, GeomFaceType> faceTypes = new Dictionary<int, GeomFaceType>();
            foreach (var surfaceId in sourceSurfaceIds.Concat(targetSurfaceIds))
            {
                if (GetSweepFaceType(vis, FeMesh.GmshTopologyId(surfaceId, part.PartId), faceTypes) != GeomFaceType.Plane)
                    return false;
            }
            // The vertices on the source side, to orient every edge between the source and the target alike
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            HashSet<int> sourceVertexIds = new HashSet<int>();
            foreach (var edgeId in sourceEdgeIds) sourceVertexIds.UnionWith(vis.GetVertexNodeIdsForEdgeId(edgeId));
            // The edges between the source and the target: straight, and all of one vector
            int[] vertexIds;
            int sourceVertexId;
            int targetVertexId;
            Vec3D vector;
            Vec3D reference = null;
            HashSet<int> crossEdgeIds = new HashSet<int>(sourceEdgeIds);
            crossEdgeIds.UnionWith(vis.GetEdgeIdsForSurfaceIds(targetSurfaceIds));
            foreach (var sideSurfaceId in sideSurfaceIds)
            {
                foreach (var edgeId in vis.FaceEdgeIds[sideSurfaceId])
                {
                    if (crossEdgeIds.Contains(edgeId)) continue;
                    //
                    vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                    if (vertexIds.Length != 2 || vertexIds[0] == vertexIds[1]) return false;
                    sourceVertexId = sourceVertexIds.Contains(vertexIds[0]) ? vertexIds[0] : vertexIds[1];
                    targetVertexId = sourceVertexId == vertexIds[0] ? vertexIds[1] : vertexIds[0];
                    if (!sourceVertexIds.Contains(sourceVertexId)) return false;
                    //
                    vector = new Vec3D(_geometry.Nodes[sourceVertexId].Coor) -
                             new Vec3D(_geometry.Nodes[targetVertexId].Coor);
                    if (!IsStraightEdge(vis, edgeId, targetVertexId, vector)) return false;
                    if (reference == null) reference = vector;
                    else if ((vector - reference).Len > 1E-6 * reference.Len) return false;
                }
            }
            if (reference != null) translation = -1 * reference;
            return reference != null;
        }
        private bool IsStraightEdge(VisualizationData vis, int edgeId, int startNodeId, Vec3D chord)
        {
            Vec3D start = new Vec3D(_geometry.Nodes[startNodeId].Coor);
            double tolerance = 1E-6 * chord.Len * chord.Len;
            foreach (var nodeId in vis.GetNodeIdsForEdgeId(edgeId))
            {
                Vec3D offset = new Vec3D(_geometry.Nodes[nodeId].Coor) - start;
                if (Vec3D.CrossProduct(chord, offset).Len > tolerance) return false;
            }
            return true;
        }
        /// <summary>
        /// Sorts the surfaces of a part into the side and the target surfaces of a sweep from the given source, or
        /// says why that cannot be done. The walls are found by walking the faces outwards from the source ring by
        /// ring; where the rings break down - a wall split along the sweep direction without a loop running around
        /// the body, so that its neighbours run the whole length - the edges are followed from the source instead,
        /// and the edges following the sweep direction come back explicitly, in one group.
        /// </summary>
        private string ValidateSweepBlock(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                          Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds,
                                          SweepPartMaps maps, Dictionary<int, int> twinSurfaceIds,
                                          out int[] sideSurfaceIds, out int[] targetSurfaceIds,
                                          out List<int[]> layerSideSurfaceIds, out int[] directionEdgeIds,
                                          out bool reachedTarget)
        {
            directionEdgeIds = null;
            reachedTarget = false;
            //
            VisualizationData vis = part.Visualization;
            int[] sourceSurfaceIdsArray = sourceSurfaceIds.ToArray();
            // Do the source surfaces form a connected surface
            if (!vis.AreSurfacesConnected(sourceSurfaceIdsArray, surfaceIdSurfaceNeighbourIds))
            {
                sideSurfaceIds = null;
                targetSurfaceIds = null;
                layerSideSurfaceIds = null;
                return "The source surfaces (" + GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") are not connected.";
            }
            // Are there any free edge loops with a single edge that no periodic wall with a single seam continues
            HashSet<int>[] freeEdgeLoops = vis.GetFreeEdgeLoops(sourceSurfaceIdsArray, maps.GetVertexIdEdgeIds(part));
            for (int i = 0; i < freeEdgeLoops.Length; i++)
            {
                if (freeEdgeLoops[i].Count == 1 &&
                    !vis.IsSingleEdgeLoopOnSeamWall(freeEdgeLoops[i].First(), sourceSurfaceIds,
                                                    surfaceIdSurfaceNeighbourIds))
                {
                    sideSurfaceIds = null;
                    targetSurfaceIds = null;
                    layerSideSurfaceIds = null;
                    string splitRimError = GetSplitBoreRimError(part, freeEdgeLoops[i].First(), sourceSurfaceIds, maps);
                    if (splitRimError != null) return splitRimError;
                    return "The source surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") contain a " +
                           "free edge loop with a single edge, " + GetSweepEdgeName(part, freeEdgeLoops[i].First()) +
                           " (a hole or an extrusion).";
                }
            }
            //
            string error = ValidateSweepRings(part, sourceSurfaceIds, maps, twinSurfaceIds, out sideSurfaceIds,
                                              out targetSurfaceIds, out layerSideSurfaceIds);
            reachedTarget = sideSurfaceIds != null && sideSurfaceIds.Length > 0 && targetSurfaceIds != null &&
                            targetSurfaceIds.Length > 0;
            if (error == null)
            {
                error = ValidateSweepBlockGeometry(part, sourceSurfaceIds, targetSurfaceIds, sideSurfaceIds, maps);
                if (error == null)
                {
                    HashSet<int> ringDirectionEdgeIds = new HashSet<int>();
                    foreach (var group in GetDirectionEdges(part.Visualization, surfaceIdSurfaceNeighbourIds, sideSurfaceIds,
                                                            layerSideSurfaceIds))
                    {
                        foreach (var edgeIds in group) ringDirectionEdgeIds.UnionWith(edgeIds);
                    }
                    error = GetWallChainsError(part, sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds,
                                               ringDirectionEdgeIds, maps);
                    if (error == null)
                        error = GetUnpairedTargetEdgesError(part, sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds,
                                                            ringDirectionEdgeIds);
                }
                if (error == null) return null;
                sideSurfaceIds = null;
                targetSurfaceIds = null;
                layerSideSurfaceIds = null;
                return error;
            }
            //
            // A body handed over because the walk swallowed a wall is followed with the wider angle, since
            // the wall it swallowed is the one that turns by more than a chain ordinarily may. Every other
            // body keeps the ordinary bound: a target inclined by more than it - the top of a block cut at
            // fifty degrees - lies within the wider one, and a chain would run out onto the target itself
            int[] chainSideSurfaceIds;
            int[] chainTargetSurfaceIds;
            int[] chainEdgeIds;
            double chainCosine = error == _swallowedWallError
                               ? Math.Cos(Globals.SweepSwallowedWallAngle * Math.PI / 180) : _sweepChainCosine;
            if (ValidateSweepChains(part, sourceSurfaceIds, maps, chainCosine, out chainSideSurfaceIds,
                                    out chainTargetSurfaceIds, out chainEdgeIds) != null) return error;
            reachedTarget = true;
            error = ValidateSweepBlockGeometry(part, sourceSurfaceIds, chainTargetSurfaceIds,
                                               chainSideSurfaceIds, maps);
            if (error == null)
                error = GetUnpairedTargetEdgesError(part, sourceSurfaceIds, chainSideSurfaceIds, chainTargetSurfaceIds,
                                                    chainEdgeIds);
            if (error != null) return error;
            sideSurfaceIds = chainSideSurfaceIds;
            targetSurfaceIds = chainTargetSurfaceIds;
            layerSideSurfaceIds = new List<int[]>() { chainSideSurfaceIds };
            directionEdgeIds = chainEdgeIds;
            return null;
        }
        private SweepBlockValidation ValidateSweepBlock(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                                        Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds,
                                                        SweepPartMaps maps, Dictionary<int, int> twinSurfaceIds)
        {
            int[] sideSurfaceIds;
            int[] targetSurfaceIds;
            int[] directionEdgeIds;
            List<int[]> layerSideSurfaceIds;
            bool reachedTarget;
            string error = ValidateSweepBlock(part, sourceSurfaceIds, surfaceIdSurfaceNeighbourIds, maps, twinSurfaceIds,
                                              out sideSurfaceIds, out targetSurfaceIds, out layerSideSurfaceIds,
                                              out directionEdgeIds, out reachedTarget);
            return new SweepBlockValidation(error, sideSurfaceIds, targetSurfaceIds, layerSideSurfaceIds,
                                            directionEdgeIds, reachedTarget);
        }
        /// <summary>
        /// Refuses a block whose target is divided into several surfaces along an edge the source does not carry.
        /// The source is meshed on its own surfaces and the target is only projected onto, so an edge between two
        /// target surfaces stays in the mesh only where a source edge is swept onto it: both its vertices are
        /// reached from the source along the edges following the sweep direction, from the two vertices of one
        /// source edge. The mesher also pins the sweep lines between an edge splitting the target and the edge
        /// splitting the source it is a copy of, moved: on a prism edge by edge, by the translation of the prism,
        /// and on any other block only when every edge splitting the target has such a copy of its own on the
        /// source, one for one - the two tangency lines of a bent plate on its front and on its back. Such edges
        /// need no edges at their ends. Any other edge between the target surfaces - one ending inside a side of
        /// a wall, where no edge follows the sweep direction, or one no source edge runs beneath - is lost, and
        /// the geometry with it.
        /// </summary>
        private string GetUnpairedTargetEdgesError(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                                   int[] sideSurfaceIds, int[] targetSurfaceIds,
                                                   IEnumerable<int> directionEdgeIds)
        {
            if (targetSurfaceIds.Length < 2) return null;
            //
            VisualizationData vis = part.Visualization;
            Dictionary<int, int> edgeIdCount = new Dictionary<int, int>();
            foreach (var surfaceId in targetSurfaceIds)
            {
                foreach (var edgeId in vis.FaceEdgeIds[surfaceId].Distinct())
                {
                    if (edgeIdCount.ContainsKey(edgeId)) edgeIdCount[edgeId]++;
                    else edgeIdCount[edgeId] = 1;
                }
            }
            int[] innerTargetEdgeIds = edgeIdCount.Where(entry => entry.Value > 1).Select(entry => entry.Key)
                                                  .OrderBy(id => id).ToArray();
            if (innerTargetEdgeIds.Length == 0) return null;
            // The vertices joined by the edges following the sweep direction, each chain one group
            int[] vertexIds;
            Dictionary<int, int> vertexIdParentId = new Dictionary<int, int>();
            Func<int, int> find = id =>
            {
                int parentId;
                while (vertexIdParentId.TryGetValue(id, out parentId) && parentId != id) id = parentId;
                return id;
            };
            foreach (var edgeId in directionEdgeIds)
            {
                vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                int rootId1 = find(vertexIds[0]);
                int rootId2 = find(vertexIds[vertexIds.Length - 1]);
                if (rootId1 != rootId2) vertexIdParentId[rootId2] = rootId1;
            }
            //
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            HashSet<Tuple<int, int>> sourceEdgeVertexPairs = new HashSet<Tuple<int, int>>();
            Dictionary<int, List<int>> rootIdSourceVertexIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in sourceEdgeIds)
            {
                vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                int vertexId1 = vertexIds[0];
                int vertexId2 = vertexIds[vertexIds.Length - 1];
                sourceEdgeVertexPairs.Add(new Tuple<int, int>(Math.Min(vertexId1, vertexId2), Math.Max(vertexId1, vertexId2)));
                foreach (var vertexId in vertexIds.Distinct())
                {
                    int rootId = find(vertexId);
                    if (!rootIdSourceVertexIds.ContainsKey(rootId)) rootIdSourceVertexIds.Add(rootId, new List<int>());
                    if (!rootIdSourceVertexIds[rootId].Contains(vertexId)) rootIdSourceVertexIds[rootId].Add(vertexId);
                }
            }
            //
            List<int> unpairedEdgeIds = new List<int>();
            List<int> sourceVertexIds1;
            List<int> sourceVertexIds2;
            foreach (var edgeId in innerTargetEdgeIds)
            {
                vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                if (!rootIdSourceVertexIds.TryGetValue(find(vertexIds[0]), out sourceVertexIds1) ||
                    !rootIdSourceVertexIds.TryGetValue(find(vertexIds[vertexIds.Length - 1]), out sourceVertexIds2) ||
                    !sourceVertexIds1.Any(id1 => sourceVertexIds2.Any(id2 =>
                        sourceEdgeVertexPairs.Contains(new Tuple<int, int>(Math.Min(id1, id2), Math.Max(id1, id2))))))
                    unpairedEdgeIds.Add(edgeId);
            }
            if (unpairedEdgeIds.Count == 0) return null;
            //
            Vec3D translation;
            Dictionary<int, int> sourceEdgeIdCount = new Dictionary<int, int>();
            foreach (var surfaceId in sourceSurfaceIds)
            {
                foreach (var edgeId in vis.FaceEdgeIds[surfaceId].Distinct())
                {
                    if (sourceEdgeIdCount.ContainsKey(edgeId)) sourceEdgeIdCount[edgeId]++;
                    else sourceEdgeIdCount[edgeId] = 1;
                }
            }
            int[] innerSourceEdgeIds = sourceEdgeIdCount.Where(entry => entry.Value > 1).Select(entry => entry.Key)
                                                        .OrderBy(id => id).ToArray();
            if (IsPrismaticSweepBlock(part, sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds, out translation))
            {
                unpairedEdgeIds.RemoveAll(edgeId => innerSourceEdgeIds.Any(sourceEdgeId =>
                {
                    Vec3D edgeTranslation = GetEdgeTranslation(vis, edgeId, sourceEdgeId);
                    return edgeTranslation != null &&
                           (edgeTranslation - translation).Len <= 1E-5 * (translation.Len + vis.EdgeLengths[edgeId]);
                }));
            }
            else if (innerSourceEdgeIds.Length == innerTargetEdgeIds.Length)
            {
                HashSet<int> usedSourceEdgeIds = new HashSet<int>();
                foreach (var edgeId in innerTargetEdgeIds)
                {
                    foreach (var sourceEdgeId in innerSourceEdgeIds)
                    {
                        if (usedSourceEdgeIds.Contains(sourceEdgeId)) continue;
                        if (GetEdgeTranslation(vis, edgeId, sourceEdgeId) == null) continue;
                        usedSourceEdgeIds.Add(sourceEdgeId);
                        break;
                    }
                }
                if (usedSourceEdgeIds.Count == innerTargetEdgeIds.Length) unpairedEdgeIds.Clear();
            }
            if (unpairedEdgeIds.Count == 0) return null;
            //
            return "The sweep target surface/s (" + GetSweepSurfaceNames(part, targetSurfaceIds) + ") are divided by " +
                   "the edge/s (" + GetSweepEdgeNames(part, unpairedEdgeIds) + ") that no edge of the sweep source " +
                   "surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIds) + ") is swept onto, so the sweep would " +
                   "lose them. Select the source surfaces by hand, or split the part so that each piece is swept on " +
                   "its own.";
        }
        /// <summary>
        /// The translation that moves a source edge onto a target edge, or null when the target edge is not the
        /// source edge moved: its vertices are those of the source edge moved by one vector, and it is as long.
        /// </summary>
        private Vec3D GetEdgeTranslation(VisualizationData vis, int targetEdgeId, int sourceEdgeId)
        {
            if (vis.EdgeLengths == null) return null;
            double targetLength = vis.EdgeLengths[targetEdgeId];
            double sourceLength = vis.EdgeLengths[sourceEdgeId];
            if (Math.Abs(targetLength - sourceLength) > 1E-3 * Math.Max(targetLength, sourceLength)) return null;
            //
            int[] targetVertexIds = vis.GetVertexNodeIdsForEdgeId(targetEdgeId);
            int[] sourceVertexIds = vis.GetVertexNodeIdsForEdgeId(sourceEdgeId);
            Vec3D targetStart = new Vec3D(_geometry.Nodes[targetVertexIds[0]].Coor);
            Vec3D targetEnd = new Vec3D(_geometry.Nodes[targetVertexIds[targetVertexIds.Length - 1]].Coor);
            Vec3D sourceStart = new Vec3D(_geometry.Nodes[sourceVertexIds[0]].Coor);
            Vec3D sourceEnd = new Vec3D(_geometry.Nodes[sourceVertexIds[sourceVertexIds.Length - 1]].Coor);
            //
            Vec3D translation = targetStart - sourceStart;
            double tolerance = 1E-5 * (translation.Len + targetLength);
            if ((targetEnd - sourceEnd - translation).Len <= tolerance) return translation;
            translation = targetStart - sourceEnd;
            tolerance = 1E-5 * (translation.Len + targetLength);
            if ((targetEnd - sourceStart - translation).Len <= tolerance) return translation;
            return null;
        }
        /// <summary>
        /// Measures the sweep a block would make and refuses the ones the topology allows but the geometry
        /// does not. The topological checks only ask that the walls close a loop and reduce to four sides,
        /// which a body satisfies just as readily when the walk stops after one ring and leaves the whole
        /// rest of the body standing as the target. Every measurement below is a property of the geometry
        /// alone, taken on the visualization the part already carries, so it costs the detection nothing.
        /// </summary>
        private string ValidateSweepBlockGeometry(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                                  int[] targetSurfaceIds, int[] sideSurfaceIds,
                                                  SweepPartMaps maps)
        {
            // A block the transfinite mesher accepts carries the same structured grid whichever of its faces
            // the sweep starts from, so none of the measurements below has anything to refuse on it. They
            // would refuse a curved block swept from its inner face to its outer one, where the target is the
            // source stretched in one direction alone and not scaled the way a taper scales it
            if (maps.IsTransfiniteBlock(part, p => IsPartTransfinite((GeometryPart)p, false) == null)) return null;
            //
            VisualizationData vis = part.Visualization;
            int[] sourceSurfaceIdsArray = sourceSurfaceIds.ToArray();
            // A wall that continues the source or the target smoothly is a rounding of that end of the
            // block rather than a side of it, and the sweep lines along it leave the source in the plane of
            // the source instead of across it. Abaqus asks the same of a swept region, that the angle
            // between the source side and each connecting side stand close to 90 degrees
            foreach (var group in maps.GetSmoothSurfaceIdGroups(_geometry, part, Globals.EdgeAngle))
            {
                if (!group.Overlaps(sideSurfaceIds)) continue;
                if (group.Overlaps(sourceSurfaceIdsArray) || group.Overlaps(targetSurfaceIds))
                {
                    int[] endSurfaceIds = group.Where(id => sourceSurfaceIdsArray.Contains(id) ||
                                                            targetSurfaceIds.Contains(id)).ToArray();
                    return "The sweep side surface/s (" +
                           GetSweepSurfaceNames(part, group.Where(id => sideSurfaceIds.Contains(id))) + ") continue " +
                           "the sweep source or target surface/s (" + GetSweepSurfaceNames(part, endSurfaceIds) +
                           ") smoothly, so they round the end of the sweep instead of following it. Select the " +
                           "source surfaces by hand, or split the part so that each piece is swept on its own.";
                }
            }
            // The walls carry the source to the target, so the two stand apart. A block whose source and
            // target meet along an edge has no sweep length there at all and its layers collapse onto that
            // edge, which is where the inverted elements of such a mesh come from
            int[] sharedEndEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIdsArray)
                                        .Intersect(vis.GetEdgeIdsForSurfaceIds(targetSurfaceIds)).ToArray();
            if (sharedEndEdgeIds.Length > 0)
            {
                return "The sweep source surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") touch " +
                       "the sweep target surface/s, so the sweep has no length along the edge/s they share (" +
                       GetSweepEdgeNames(part, sharedEndEdgeIds) + "). Select the source surfaces by hand, or " +
                       "split the part so that each piece is swept on its own.";
            }
            // The target of a sweep is the source carried along the sweep lines, so it is a similar shape
            // however strongly the sweep tapers: the square root of its area ratio and its perimeter ratio
            // then report the same scale. A target that is the rest of the body reports two different ones
            if (vis.FaceAreas != null && vis.EdgeLengths != null && targetSurfaceIds.Length > 0)
            {
                double sourceArea = sourceSurfaceIdsArray.Sum(id => vis.FaceAreas[id]);
                double targetArea = targetSurfaceIds.Sum(id => vis.FaceAreas[id]);
                double sourcePerimeter = GetSweepPerimeter(vis, sourceSurfaceIdsArray, maps.GetVertexIdEdgeIds(part));
                double targetPerimeter = GetSweepPerimeter(vis, targetSurfaceIds, maps.GetVertexIdEdgeIds(part));
                if (sourceArea > 0 && sourcePerimeter > 0 && targetPerimeter > 0)
                {
                    double scale = Math.Sqrt(targetArea / sourceArea) * sourcePerimeter / targetPerimeter;
                    if (scale < 0.8 || scale > 1.25)
                    {
                        return "The sweep target surface/s (" + GetSweepSurfaceNames(part, targetSurfaceIds) + ") are " +
                               "not the shape of the sweep source surface/s (" +
                               GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") carried along the sweep " +
                               "direction. Select the source surfaces by hand, or split the part so that each piece " +
                               "is swept on its own.";
                    }
                }
            }
            //
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIdsArray);
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = maps.GetVertexIdEdgeIds(part);
            HashSet<int> chainEdgeIds = new HashSet<int>();
            Dictionary<int, double> vertexIdChainLength = new Dictionary<int, double>();
            foreach (var vertexId in vis.GetVertexNodeIdsForEdgeIds(sourceEdgeIds.ToArray()).OrderBy(id => id))
            {
                foreach (var edgeId in vertexIdEdgeIds[vertexId].OrderBy(id => id))
                {
                    if (sourceEdgeIds.Contains(edgeId) || chainEdgeIds.Contains(edgeId)) continue;
                    //
                    int lastEdgeId;
                    int lastVertexId;
                    double chainLength;
                    FollowSweepChain(vis, edgeId, vertexId, sourceEdgeIds, vertexIdEdgeIds, _sweepChainCosine,
                                     chainEdgeIds, out lastEdgeId, out lastVertexId, out chainLength);
                    Vec3D chord = new Vec3D(_geometry.Nodes[lastVertexId].Coor) -
                                  new Vec3D(_geometry.Nodes[vertexId].Coor);
                    // A chain that ends near where it began has run around the body rather than across it
                    if (chainLength > 0 && chord.Len < 0.3 * chainLength)
                    {
                        return "The edge chain following the sweep direction from " + GetSweepEdgeName(part, edgeId) +
                               " turns back on itself instead of running from the source surface/s (" +
                               GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") to the target surface/s.";
                    }
                    // The sweep leaves the source into the material and arrives at the target out of it.
                    // The visualization is wound outward, so the cosine against the source normal is
                    // negative and the one against the target normal positive; a block reporting them the
                    // other way round has its source and its target the wrong way about, and sweeps out of
                    // the part rather than through it
                    Vec3D entryDirection = GetSweepEdgeTangent(vis, edgeId, vertexId);
                    Vec3D exitDirection = -1 * GetSweepEdgeTangent(vis, lastEdgeId, lastVertexId);
                    double entryCosine = entryDirection.Len > 0
                                       ? GetSweepFaceCosine(vis, sourceSurfaceIdsArray, vertexId, entryDirection)
                                       : double.NaN;
                    double exitCosine = exitDirection.Len > 0
                                      ? GetSweepFaceCosine(vis, targetSurfaceIds, lastVertexId, exitDirection)
                                      : double.NaN;
                    if (entryCosine > 0 || exitCosine < 0)
                    {
                        return "The sweep direction leaves the part " + part.Name + " through the sweep source " +
                               "surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") instead of " +
                               "entering it, so the source and the target surface/s are the wrong way round.";
                    }
                    //
                    if (chainLength > 0)
                    {
                        double length;
                        vertexIdChainLength.TryGetValue(vertexId, out length);
                        vertexIdChainLength[vertexId] = Math.Max(length, chainLength);
                    }
                }
            }
            // Every sweep line of a block is divided into the same number of elements, so the layers are thinner
            // on a short line than on a long one. A block that bends lengthens its lines steadily from the inside of
            // the bend to the outside - by the angle it turns through for every unit of distance across it - and its
            // elements change their thickness as steadily; a source taken across a slot or beside a step meets lines
            // of very different lengths side by side, and the elements between them fold. So the lengths of the
            // lines leaving the two ends of every source edge may differ by no more than a full turn makes them
            // differ: by two pi times the distance between the ends. The length of a sweep line is the length of
            // the chain it follows, which on a block that bends is longer than the chord between its ends. A source
            // across a slot keeps its lines straight, and is caught by the lines measured across it below
            foreach (var edgeId in sourceEdgeIds.OrderBy(id => id))
            {
                int[] edgeVertexIds = vis.GetVertexNodeIdsForEdgeIds(new int[] { edgeId }).ToArray();
                if (edgeVertexIds.Length != 2) continue;
                double length1;
                double length2;
                if (!vertexIdChainLength.TryGetValue(edgeVertexIds[0], out length1) ||
                    !vertexIdChainLength.TryGetValue(edgeVertexIds[1], out length2)) continue;
                double distance = (new Vec3D(_geometry.Nodes[edgeVertexIds[1]].Coor) -
                                   new Vec3D(_geometry.Nodes[edgeVertexIds[0]].Coor)).Len;
                if (distance <= 0 || Math.Abs(length2 - length1) <= _maxSweepLengthGradient * distance) continue;
                //
                return "The sweep lines leaving the two ends of " + GetSweepEdgeName(part, edgeId) + " of the sweep " +
                       "source surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") are " +
                       Math.Round(length1, 3) + " and " + Math.Round(length2, 3) + " long, which a block bending " +
                       "through a full turn could not make them over the " + Math.Round(distance, 3) + " between " +
                       "them, so one element layer count cannot suit them. Select the source surfaces by hand, or " +
                       "split the part so that each piece is swept on its own.";
            }
            // The chains measure the sweep only where an edge leaves the source. A source whose faces continue
            // one another smoothly - the two walls of a slot and the rounded bottom between them - has vertices
            // no edge leaves, and the length across the body behind them is never seen: the chains report the
            // width of the prongs and nothing of the way from the bottom of the slot to the end of the part.
            // So the sweep lines are measured across the source as well, from its cells into the material
            double minSampledLength;
            double maxSampledLength;
            if (SampleSweepLengths(vis, sourceSurfaceIdsArray, targetSurfaceIds, out minSampledLength,
                                   out maxSampledLength) && minSampledLength < 0.5 * maxSampledLength)
            {
                return "The sweep lines measured across the sweep source surface/s (" +
                       GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") differ in length by more than half, so " +
                       "one element layer count cannot suit them all. Select the source surfaces by hand, or split " +
                       "the part so that each piece is swept on its own.";
            }
            //
            return null;
        }
        /// <summary>
        /// The shortest and the longest sweep line measured across the source: from the centroid of a source
        /// cell along the inward normal of the cell to the first cell of the part the line meets, for up to a
        /// hundred cells spread over the source. Only a line ending on a target surface is measured - a line
        /// ending on a wall belongs to a body that bends, and the straight line says nothing of its length.
        /// False when no line reached a target.
        /// </summary>
        private bool SampleSweepLengths(VisualizationData vis, int[] sourceSurfaceIds, int[] targetSurfaceIds,
                                        out double minLength, out double maxLength)
        {
            minLength = double.MaxValue;
            maxLength = 0;
            if (vis.Cells == null || vis.CellIdsByFace == null) return false;
            //
            HashSet<int> sourceSurfaceIdSet = new HashSet<int>(sourceSurfaceIds);
            HashSet<int> targetSurfaceIdSet = new HashSet<int>(targetSurfaceIds);
            // The box of every surface a line may end on, so that a line is tested against the cells of the
            // surfaces it can reach alone
            int numSurfaces = vis.CellIdsByFace.Length;
            double[][] surfaceMin = new double[numSurfaces][];
            double[][] surfaceMax = new double[numSurfaces][];
            for (int surfaceId = 0; surfaceId < numSurfaces; surfaceId++)
            {
                if (sourceSurfaceIdSet.Contains(surfaceId)) continue;
                double[] min = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
                double[] max = new double[] { double.MinValue, double.MinValue, double.MinValue };
                foreach (var cellId in vis.CellIdsByFace[surfaceId])
                {
                    foreach (var nodeId in vis.Cells[cellId])
                    {
                        double[] coor = _geometry.Nodes[nodeId].Coor;
                        for (int k = 0; k < 3; k++)
                        {
                            if (coor[k] < min[k]) min[k] = coor[k];
                            if (coor[k] > max[k]) max[k] = coor[k];
                        }
                    }
                }
                surfaceMin[surfaceId] = min;
                surfaceMax[surfaceId] = max;
            }
            //
            const int maxSamples = 100;
            List<int> sourceCellIds = new List<int>();
            foreach (var surfaceId in sourceSurfaceIds) sourceCellIds.AddRange(vis.CellIdsByFace[surfaceId]);
            int step = Math.Max(1, sourceCellIds.Count / maxSamples);
            //
            for (int i = 0; i < sourceCellIds.Count; i += step)
            {
                int[] cell = vis.Cells[sourceCellIds[i]];
                if (cell.Length < 3) continue;
                int numCorners = cell.Length == 3 || cell.Length == 6 ? 3 : 4;
                Vec3D a = new Vec3D(_geometry.Nodes[cell[0]].Coor);
                Vec3D b = new Vec3D(_geometry.Nodes[cell[1]].Coor);
                Vec3D c = new Vec3D(_geometry.Nodes[cell[2]].Coor);
                Vec3D normal = Vec3D.CrossProduct(b - a, c - a);
                if (normal.Len == 0) continue;
                // Into the material: the visualization is wound outward
                Vec3D direction = (-1 / normal.Len) * normal;
                Vec3D origin = new Vec3D();
                for (int j = 0; j < numCorners; j++) origin = origin + new Vec3D(_geometry.Nodes[cell[j]].Coor);
                origin = (1.0 / numCorners) * origin;
                //
                double nearest = double.MaxValue;
                int nearestSurfaceId = -1;
                for (int surfaceId = 0; surfaceId < numSurfaces; surfaceId++)
                {
                    if (surfaceMin[surfaceId] == null) continue;
                    if (!DoesRayHitBox(origin, direction, surfaceMin[surfaceId], surfaceMax[surfaceId], nearest)) continue;
                    foreach (var cellId in vis.CellIdsByFace[surfaceId])
                    {
                        double distance;
                        if (!DoesRayHitCell(vis.Cells[cellId], origin, direction, out distance)) continue;
                        if (distance < nearest)
                        {
                            nearest = distance;
                            nearestSurfaceId = surfaceId;
                        }
                    }
                }
                if (nearestSurfaceId < 0 || !targetSurfaceIdSet.Contains(nearestSurfaceId)) continue;
                minLength = Math.Min(minLength, nearest);
                maxLength = Math.Max(maxLength, nearest);
            }
            return maxLength > 0;
        }
        private static bool DoesRayHitBox(Vec3D origin, Vec3D direction, double[] min, double[] max, double maxDistance)
        {
            double near = 0;
            double far = maxDistance;
            double[] o = origin.Coor;
            double[] d = direction.Coor;
            for (int k = 0; k < 3; k++)
            {
                if (Math.Abs(d[k]) < 1E-12)
                {
                    if (o[k] < min[k] || o[k] > max[k]) return false;
                    continue;
                }
                double t1 = (min[k] - o[k]) / d[k];
                double t2 = (max[k] - o[k]) / d[k];
                if (t1 > t2) { double t = t1; t1 = t2; t2 = t; }
                if (t1 > near) near = t1;
                if (t2 < far) far = t2;
                if (near > far) return false;
            }
            return true;
        }
        /// <summary>
        /// Whether a line starting at the origin meets the cell, and how far along the line it does so. A
        /// quadrangle is tested as its two triangles.
        /// </summary>
        private bool DoesRayHitCell(int[] cell, Vec3D origin, Vec3D direction, out double distance)
        {
            distance = double.MaxValue;
            if (cell.Length < 3) return false;
            Vec3D a = new Vec3D(_geometry.Nodes[cell[0]].Coor);
            Vec3D b = new Vec3D(_geometry.Nodes[cell[1]].Coor);
            Vec3D c = new Vec3D(_geometry.Nodes[cell[2]].Coor);
            bool hit = DoesRayHitTriangle(a, b, c, origin, direction, out distance);
            if (cell.Length == 4 || cell.Length == 8)
            {
                double distance2;
                Vec3D d = new Vec3D(_geometry.Nodes[cell[3]].Coor);
                if (DoesRayHitTriangle(a, c, d, origin, direction, out distance2) && distance2 < distance)
                {
                    distance = distance2;
                    hit = true;
                }
            }
            return hit;
        }
        private static bool DoesRayHitTriangle(Vec3D a, Vec3D b, Vec3D c, Vec3D origin, Vec3D direction,
                                               out double distance)
        {
            distance = double.MaxValue;
            Vec3D ab = b - a;
            Vec3D ac = c - a;
            Vec3D p = Vec3D.CrossProduct(direction, ac);
            double det = Vec3D.DotProduct(ab, p);
            double scale = ab.Len * ac.Len;
            if (Math.Abs(det) < 1E-12 * scale) return false;
            double invDet = 1 / det;
            Vec3D s = origin - a;
            double u = Vec3D.DotProduct(s, p) * invDet;
            if (u < -1E-9 || u > 1 + 1E-9) return false;
            Vec3D q = Vec3D.CrossProduct(s, ab);
            double v = Vec3D.DotProduct(direction, q) * invDet;
            if (v < -1E-9 || u + v > 1 + 1E-9) return false;
            double t = Vec3D.DotProduct(ac, q) * invDet;
            if (t <= 1E-9 * Math.Sqrt(scale)) return false;
            distance = t;
            return true;
        }
        /// <summary>
        /// The cosine between a direction and the normal of the given surfaces at a vertex, taken from the
        /// surface whose normal is most nearly parallel to the direction, and NaN when none of them reaches
        /// the vertex. It is the sign that is read: the visualization is wound outward, so a direction
        /// leading into the material reports a negative cosine.
        /// </summary>
        private double GetSweepFaceCosine(VisualizationData vis, int[] surfaceIds, int vertexId, Vec3D direction)
        {
            double best = -1;
            double cosine = double.NaN;
            foreach (var surfaceId in surfaceIds)
            {
                Vec3D normal = GetFaceNormalAtVertex(vis, surfaceId, vertexId);
                if (normal == null || normal.Len == 0) continue;
                //
                double value = Vec3D.DotProduct(direction, normal) / (direction.Len * normal.Len);
                if (Math.Abs(value) <= best) continue;
                best = Math.Abs(value);
                cosine = value;
            }
            return cosine;
        }
        /// <summary>
        /// The length of the free edge loops of a group of surfaces - its outer boundary and the boundaries
        /// of the holes in it.
        /// </summary>
        private static double GetSweepPerimeter(VisualizationData vis, int[] surfaceIds,
                                                Dictionary<int, HashSet<int>> vertexIdEdgeIds)
        {
            double perimeter = 0;
            foreach (var loop in vis.GetFreeEdgeLoops(surfaceIds, vertexIdEdgeIds))
            {
                foreach (var edgeId in loop) perimeter += vis.EdgeLengths[edgeId];
            }
            return perimeter;
        }
        private static string GetSweepSideHoleError(BasePart part, int surfaceId)
        {
            return "The sweep side " + GetSweepSurfaceName(part, surfaceId) + " has a hole. A bore running across the " +
                   "sweep direction has to be swept along its own axis, or the part has to be split.";
        }
        private static string GetSweepSideFourSidesError(BasePart part, int surfaceId)
        {
            return "The sweep side " + GetSweepSurfaceName(part, surfaceId) + " could not be reduced to 4 sides. Each " +
                   "side surface must have exactly 2 edges following the sweep direction; the source and target " +
                   "sides may be split into any number of edges.";
        }
        /// <summary>
        /// A surface or an edge the way the interface names it, so that a message about the sweep says which one
        /// it means and the geometry it names can be found and shown.
        /// </summary>
        /// <summary>
        /// Why a bore or a boss whose rim on the source is a single closed edge cannot be swept, when the reason is a
        /// defect of the geometry: its wall is a periodic face closed along one seam, but the rim at its other end is
        /// split into several edges by vertices that no other edge meets. Such vertices carry nothing; a repair of
        /// the geometry merges the edges, and the wall is then one the sweep meshes. Null for any other wall.
        /// </summary>
        private string GetSplitBoreRimError(GeometryPart part, int rimEdgeId, HashSet<int> sourceSurfaceIds,
                                            SweepPartMaps maps)
        {
            VisualizationData vis = part.Visualization;
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = maps.GetVertexIdEdgeIds(part);
            HashSet<int> rimVertexIds = vis.GetVertexNodeIdsForEdgeIds(new int[] { rimEdgeId });
            for (int surfaceId = 0; surfaceId < vis.FaceCount; surfaceId++)
            {
                if (sourceSurfaceIds.Contains(surfaceId) || !vis.FaceEdgeIds[surfaceId].Contains(rimEdgeId)) continue;
                // The seam runs from the vertex of the rim to the other end; every other edge is a piece of that end
                int[] otherEdgeIds = vis.FaceEdgeIds[surfaceId].Where(id => id != rimEdgeId).Distinct().ToArray();
                int[] seamEdgeIds = otherEdgeIds.Where(id => vis.GetVertexNodeIdsForEdgeIds(new int[] { id })
                                                                .Overlaps(rimVertexIds)).ToArray();
                int[] pieceEdgeIds = otherEdgeIds.Except(seamEdgeIds).ToArray();
                if (seamEdgeIds.Length != 1 || pieceEdgeIds.Length < 2) continue;
                // The pieces close a loop, and only the seam meets it besides them
                HashSet<int> seamVertexIds = vis.GetVertexNodeIdsForEdgeIds(seamEdgeIds);
                HashSet<int> pieceVertexIds = vis.GetVertexNodeIdsForEdgeIds(pieceEdgeIds);
                if (pieceVertexIds.Count != pieceEdgeIds.Length) continue;
                HashSet<int> pieces = new HashSet<int>(pieceEdgeIds);
                bool superfluous = true;
                foreach (var vertexId in pieceVertexIds)
                {
                    int others = vertexIdEdgeIds[vertexId].Count(id => !pieces.Contains(id) && !seamEdgeIds.Contains(id));
                    if (others > 0 || (!seamVertexIds.Contains(vertexId) && vertexIdEdgeIds[vertexId].Count != 2))
                        superfluous = false;
                }
                if (!superfluous) continue;
                //
                return _splitBoreRimErrorStart + GetSweepSurfaceName(part, surfaceId) + " at its other end is " +
                       "split into " + pieceEdgeIds.Length + " edges (" + GetSweepEdgeNames(part, pieceEdgeIds) + ") by " +
                       "vertices no other edge meets, so the wall is not one closed along a single seam, which the " +
                       "sweep meshes. Repair the geometry of the part " + part.Name + " to merge them.";
            }
            return null;
        }
        private static string GetSweepSurfaceName(BasePart part, int surfaceId)
        {
            return "surface " + (surfaceId + 1) + " of " + part.Name;
        }
        private static string GetSweepSurfaceNames(BasePart part, IEnumerable<int> surfaceIds)
        {
            return string.Join(", ", surfaceIds.OrderBy(id => id).Select(id => GetSweepSurfaceName(part, id)));
        }
        private static string GetSweepEdgeName(BasePart part, int edgeId)
        {
            return "edge " + (edgeId + 1) + " of " + part.Name;
        }
        private static string GetSweepEdgeNames(BasePart part, IEnumerable<int> edgeIds)
        {
            return string.Join(", ", edgeIds.OrderBy(id => id).Select(id => GetSweepEdgeName(part, id)));
        }
        private const double _sweepChainCosine = 0.9;
        private const int _maxCompoundSweepSources = 20000;
        private const int _maxCompoundSweepSolves = 100;
        private const string _splitBoreRimErrorStart = "The rim of the bore or boss ";
        // How fast the length of the sweep lines may change across the source: a block bending through a full turn
        // lengthens them by two pi for every unit of distance from the inside of the bend to the outside
        private const double _maxSweepLengthGradient = 2 * Math.PI;
        private static readonly double _wallChainsCosine = Math.Cos(45 * Math.PI / 180);
        private const double _wallChainsLengthTolerance = 0.05;
        private const string _swallowedWallError = "A sweep side surface was taken for a target surface.";
        private string ValidateSweepRings(GeometryPart part, HashSet<int> sourceSurfaceIds, SweepPartMaps maps,
                                          Dictionary<int, int> twinSurfaceIds,
                                          out int[] sideSurfaceIds, out int[] targetSurfaceIds,
                                          out List<int[]> layerSideSurfaceIds)
        {
            VisualizationData vis = part.Visualization;
            int[] sourceSurfaceIdsArray = sourceSurfaceIds.ToArray();
            // Is it possible to determine the side and target surfaces
            if (!vis.IsBodySweepable(sourceSurfaceIds, out sideSurfaceIds, out targetSurfaceIds, out layerSideSurfaceIds,
                                     maps.GetSurfaceIdSurfaceNeighbourIds(part), maps.GetVertexIdEdgeIds(part)))
                return "It is not possible to determine the side and target surfaces of a sweep from the source " +
                       "surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ").";
            if (sideSurfaceIds.Length == 0)
                return "The sweep side surfaces around the source surface/s (" +
                       GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ") do not form a closed loop.";
            // The mesh is built by projecting onto every target surface and keeping the nearest hit, so a
            // target that is split into several surfaces is allowed
            if (targetSurfaceIds.Length == 0)
                return "No sweep target surface could be determined for the source surface/s (" +
                       GetSweepSurfaceNames(part, sourceSurfaceIdsArray) + ").";
            // Can all side surfaces be reduced to 4 sides? A side surface may carry more than 4 edges when
            // the source or the target face has been split, since the split puts an extra vertex partway
            // along one of its sides. What matters is that it still has exactly two edges following the
            // sweep direction, so its four corners can be nominated to the transfinite algorithm.
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIdsArray);
            HashSet<int> targetEdgeIdSet = vis.GetEdgeIdsForSurfaceIds(targetSurfaceIds);
            HashSet<int> crossEdgeIds = new HashSet<int>(sourceEdgeIds);
            crossEdgeIds.UnionWith(targetEdgeIdSet);
            // A body split along the sweep direction has its walls in rings, and a wall in the middle
            // touches neither the source nor the target: the edges between two rings are cross edges too
            HashSet<int> ringEdgeIds;
            for (int i = 0; i + 1 < layerSideSurfaceIds.Count; i++)
            {
                ringEdgeIds = vis.GetEdgeIdsForSurfaceIds(layerSideSurfaceIds[i]);
                ringEdgeIds.IntersectWith(vis.GetEdgeIdsForSurfaceIds(layerSideSurfaceIds[i + 1]));
                crossEdgeIds.UnionWith(ringEdgeIds);
            }
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = maps.GetVertexIdEdgeIds(part);
            foreach (var surfaceId in sideSurfaceIds)
            {
                // A wall with a hole - the opening of a bore that runs across the sweep - has no structured
                // grid; gmsh refuses such a transfinite surface, so it is refused here first. A wall running around
                // the body as a ring, one of its two loops on the source and the other on the target - the end of a
                // tube swept through its wall - is no such wall: the mesher splits it into walls of four sides
                int numOfEdgeLoops = CountEdgeLoops(vis.FaceEdgeIds[surfaceId], vertexIdEdgeIds);
                if (numOfEdgeLoops == 2 && IsRingWall(vis.FaceEdgeIds[surfaceId], sourceEdgeIds, targetEdgeIdSet))
                    continue;
                if (numOfEdgeLoops != 1) return GetSweepSideHoleError(part, surfaceId);
                if (vis.IsSurfaceACylinderLike(surfaceId, out _)) continue;
                //
                int numDirectionEdges = 0;
                int numCrossEdges = 0;
                foreach (var edgeId in vis.FaceEdgeIds[surfaceId])
                {
                    if (crossEdgeIds.Contains(edgeId)) numCrossEdges++;
                    else numDirectionEdges++;
                }
                if (numDirectionEdges != 2 || numCrossEdges < 2) return GetSweepSideFourSidesError(part, surfaceId);
            }
            // A wall split along the sweep direction without a loop around the body makes the walk take the far
            // piece for a target, and the near piece's edge then runs straight on into an edge of that target.
            // The rings describe such a body only as a sweep onto its own side, so it is left to the chains
            HashSet<int> targetEdgeIds = vis.GetEdgeIdsForSurfaceIds(targetSurfaceIds);
            foreach (var surfaceId in sideSurfaceIds)
            {
                foreach (var edgeId in vis.FaceEdgeIds[surfaceId])
                {
                    if (crossEdgeIds.Contains(edgeId)) continue;
                    foreach (var vertexId in vis.GetVertexNodeIdsForEdgeId(edgeId))
                    {
                        Vec3D incoming = -1 * GetSweepEdgeTangent(vis, edgeId, vertexId);
                        if (incoming.Len == 0) continue;
                        foreach (var targetEdgeId in vertexIdEdgeIds[vertexId])
                        {
                            if (!targetEdgeIds.Contains(targetEdgeId)) continue;
                            Vec3D outgoing = GetSweepEdgeTangent(vis, targetEdgeId, vertexId);
                            if (outgoing.Len == 0) continue;
                            if (Vec3D.DotProduct(incoming, outgoing) > 0.95 * incoming.Len * outgoing.Len)
                                return "The sweep side " + GetSweepSurfaceName(part, surfaceId) + " runs straight " +
                                       "on into a target surface at " + GetSweepEdgeName(part, targetEdgeId) + ".";
                        }
                    }
                }
            }
            // The same body read the other way round: a wall split partway along the sweep leaves its far
            // half in the last wavefront, and the walk takes that for a target beside the real one. Such a
            // surface faces across the sweep instead of along it, and the chains reach it as the wall it is
            if (IsWallTakenForTarget(part, sourceSurfaceIds, targetSurfaceIds, twinSurfaceIds))
                return _swallowedWallError;
            //
            return null;
        }
        /// <summary>
        /// Whether the walk took a wall for a target. Everything the ring walk has not reached when it stops
        /// becomes the target, so a wall split partway along the sweep with no ring around the body - the tip
        /// wall standing above the chamfer of a plate, which reaches the one end of the block and not the
        /// other - lands in the target beside the face that really is the end.
        ///
        /// It is told apart by where it faces: the end of a block faces along the sweep and a side of it faces
        /// across, so a surface that turns its back on the source everywhere, by more than the chain angle, is
        /// a side. A source too curved to have a direction of its own - a bore swept around its axis - gives
        /// no reading and is left alone.
        ///
        /// Asked only where the reading decides anything: of a surface this block hands on to another part,
        /// which ends against a second surface of the same target. That is the one configuration the sweep
        /// cannot carry - the shared surface cannot be swept through, and the whole run of parts stops at it -
        /// and it is the surface on the other side of that edge that has to be a wall for the sweep to go on.
        /// A part swept on its own projects onto every surface of its target and is left to the reading it has
        /// always had, and so is a target whose surfaces stand apart from the one that is handed on. Only a
        /// target of exactly two surfaces is read again - the end of the block and the one wall the walk
        /// swallowed - since a target cut into more than that is not this at all.
        /// </summary>
        private bool IsWallTakenForTarget(GeometryPart part, HashSet<int> sourceSurfaceIds, int[] targetSurfaceIds,
                                          Dictionary<int, int> twinSurfaceIds)
        {
            if (targetSurfaceIds.Length != 2 || twinSurfaceIds == null) return false;
            //
            double flatness = 0;
            Vec3D sourceNormal = null;
            VisualizationData vis = part.Visualization;
            foreach (var surfaceId in targetSurfaceIds)
            {
                if (!twinSurfaceIds.ContainsKey(FeMesh.GmshTopologyId(surfaceId, part.PartId))) continue;
                //
                foreach (var neighbourId in targetSurfaceIds)
                {
                    if (neighbourId == surfaceId) continue;
                    if (!vis.FaceEdgeIds[surfaceId].Intersect(vis.FaceEdgeIds[neighbourId]).Any()) continue;
                    //
                    if (sourceNormal == null)
                    {
                        sourceNormal = GetSurfaceGroupNormal(vis, sourceSurfaceIds, out flatness);
                        if (sourceNormal == null || flatness < 0.9) return false;
                    }
                    if (!DoesSurfaceFaceAlong(vis, neighbourId, sourceNormal)) return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Whether any part of a surface faces the way the source does, within the chain angle. The end of a
        /// block faces along the sweep and a side of it faces across, and a surface curved enough to do both -
        /// the wall of a bore the sweep runs into - counts as an end.
        /// </summary>
        private bool DoesSurfaceFaceAlong(VisualizationData vis, int surfaceId, Vec3D sourceNormal)
        {
            double cosine = Math.Cos(Globals.SweepSwallowedWallAngle * Math.PI / 180);
            foreach (var cellId in vis.CellIdsByFace[surfaceId])
            {
                Vec3D normal = GetCellNormal(vis.Cells[cellId]);
                if (normal == null) continue;
                if (Math.Abs(Vec3D.DotProduct(normal, sourceNormal)) > cosine * normal.Len * sourceNormal.Len)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// The normals of the cells of a group of surfaces added up, and how flat the group is - the length of
        /// the sum against the sum of the lengths, one for a plane and nothing for a surface that closes on
        /// itself.
        /// </summary>
        private Vec3D GetSurfaceGroupNormal(VisualizationData vis, IEnumerable<int> surfaceIds, out double flatness)
        {
            flatness = 0;
            double lengthSum = 0;
            Vec3D groupNormal = new Vec3D();
            foreach (var surfaceId in surfaceIds)
            {
                foreach (var cellId in vis.CellIdsByFace[surfaceId])
                {
                    Vec3D normal = GetCellNormal(vis.Cells[cellId]);
                    if (normal == null) continue;
                    groupNormal += normal;
                    lengthSum += normal.Len;
                }
            }
            if (lengthSum == 0) return null;
            //
            flatness = groupNormal.Len / lengthSum;
            return groupNormal;
        }
        private Vec3D GetCellNormal(int[] cell)
        {
            if (cell.Length < 3) return null;
            Vec3D a = new Vec3D(_geometry.Nodes[cell[0]].Coor);
            Vec3D b = new Vec3D(_geometry.Nodes[cell[1]].Coor);
            Vec3D c = new Vec3D(_geometry.Nodes[cell[2]].Coor);
            Vec3D normal = Vec3D.CrossProduct(b - a, c - a);
            return normal.Len > 0 ? normal : null;
        }
        /// <summary>
        /// Sorts the surfaces by following the edges from the source: from every vertex of the source, along
        /// every edge leaving it, and on through the edge that continues most smoothly at every vertex, until
        /// nothing continues within the chain angle. The edges so reached follow the sweep direction; a surface
        /// carrying one of them is a wall, every other surface is a target. A wall must have exactly two such
        /// chains on it, whatever number of edges each is made of.
        /// </summary>
        private string ValidateSweepChains(GeometryPart part, HashSet<int> sourceSurfaceIds, SweepPartMaps maps,
                                           double chainCosine,
                                           out int[] sideSurfaceIds, out int[] targetSurfaceIds, out int[] chainEdgeIds)
        {
            sideSurfaceIds = null;
            targetSurfaceIds = null;
            chainEdgeIds = null;
            //
            VisualizationData vis = part.Visualization;
            HashSet<int> sourceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = maps.GetVertexIdEdgeIds(part);
            HashSet<int> chainEdgeIdSet = new HashSet<int>();
            foreach (var vertexId in vis.GetVertexNodeIdsForEdgeIds(sourceEdgeIds.ToArray()).OrderBy(id => id))
            {
                foreach (var edgeId in vertexIdEdgeIds[vertexId].OrderBy(id => id))
                {
                    if (sourceEdgeIds.Contains(edgeId) || chainEdgeIdSet.Contains(edgeId)) continue;
                    FollowSweepChain(vis, edgeId, vertexId, sourceEdgeIds, vertexIdEdgeIds, chainCosine,
                                     chainEdgeIdSet, out _, out _, out _);
                }
            }
            if (chainEdgeIdSet.Count == 0)
                return "No edges follow the sweep direction from the source surfaces (" +
                       GetSweepSurfaceNames(part, sourceSurfaceIds) + ").";
            //
            List<int> wallSurfaceIds = new List<int>();
            List<int> otherSurfaceIds = new List<int>();
            for (int surfaceId = 0; surfaceId < vis.FaceEdgeIds.Length; surfaceId++)
            {
                if (sourceSurfaceIds.Contains(surfaceId)) continue;
                if (vis.FaceEdgeIds[surfaceId].Any(id => chainEdgeIdSet.Contains(id))) wallSurfaceIds.Add(surfaceId);
                else otherSurfaceIds.Add(surfaceId);
            }
            if (otherSurfaceIds.Count == 0)
                return "No sweep target surface could be determined for the source surface/s (" +
                       GetSweepSurfaceNames(part, sourceSurfaceIds) + ").";
            //
            foreach (var surfaceId in wallSurfaceIds)
            {
                if (CountEdgeLoops(vis.FaceEdgeIds[surfaceId], vertexIdEdgeIds) != 1)
                    return GetSweepSideHoleError(part, surfaceId);
                if (vis.IsSurfaceACylinderLike(surfaceId, out _)) continue;
                //
                int[] directionEdgeIds = vis.FaceEdgeIds[surfaceId].Where(id => chainEdgeIdSet.Contains(id)).ToArray();
                int numCrossEdges = vis.FaceEdgeIds[surfaceId].Length - directionEdgeIds.Length;
                if (CountChainEnds(vis, directionEdgeIds) != 4 || numCrossEdges < 2)
                    return GetSweepSideFourSidesError(part, surfaceId);
            }
            string wallChainsError = GetWallChainsError(part, sourceSurfaceIds, wallSurfaceIds.ToArray(),
                                                        otherSurfaceIds.ToArray(), chainEdgeIdSet, maps);
            if (wallChainsError != null) return wallChainsError;
            //
            sideSurfaceIds = wallSurfaceIds.ToArray();
            targetSurfaceIds = otherSurfaceIds.ToArray();
            chainEdgeIds = chainEdgeIdSet.OrderBy(id => id).ToArray();
            return null;
        }
        private void FollowSweepChain(VisualizationData vis, int edgeId, int vertexId, HashSet<int> sourceEdgeIds,
                                      Dictionary<int, HashSet<int>> vertexIdEdgeIds, double chainCosine,
                                      HashSet<int> chainEdgeIds,
                                      out int lastEdgeId, out int lastVertexId, out double chainLength)
        {
            chainLength = 0;
            while (true)
            {
                chainEdgeIds.Add(edgeId);
                if (vis.EdgeLengths != null) chainLength += vis.EdgeLengths[edgeId];
                int[] endVertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                int nextVertexId = endVertexIds[0] == vertexId ? endVertexIds[1] : endVertexIds[0];
                lastEdgeId = edgeId;
                lastVertexId = nextVertexId;
                if (nextVertexId == vertexId) return;
                // The direction of travel into the vertex, against the direction the edge leaves the vertex in
                Vec3D incoming = -1 * GetSweepEdgeTangent(vis, edgeId, nextVertexId);
                if (incoming.Len == 0) return;
                int bestEdgeId = -1;
                double bestCosine = chainCosine;
                foreach (var candidateId in vertexIdEdgeIds[nextVertexId].OrderBy(id => id))
                {
                    if (candidateId == edgeId || sourceEdgeIds.Contains(candidateId) || chainEdgeIds.Contains(candidateId))
                        continue;
                    Vec3D outgoing = GetSweepEdgeTangent(vis, candidateId, nextVertexId);
                    if (outgoing.Len == 0) continue;
                    double cosine = Vec3D.DotProduct(incoming, outgoing) / (incoming.Len * outgoing.Len);
                    if (cosine > bestCosine)
                    {
                        bestCosine = cosine;
                        bestEdgeId = candidateId;
                    }
                }
                if (bestEdgeId < 0) return;
                edgeId = bestEdgeId;
                vertexId = nextVertexId;
            }
        }
        /// <summary>
        /// The direction an edge leaves one of its vertices in, read off the edge cell at that vertex.
        /// </summary>
        private Vec3D GetSweepEdgeTangent(VisualizationData vis, int edgeId, int vertexNodeId)
        {
            Vec3D vertex = new Vec3D(_geometry.Nodes[vertexNodeId].Coor);
            foreach (var cellId in vis.EdgeCellIdsByEdge[edgeId])
            {
                int[] cell = vis.EdgeCells[cellId];
                if (cell[0] == vertexNodeId) return new Vec3D(_geometry.Nodes[cell[cell.Length - 1]].Coor) - vertex;
                if (cell[cell.Length - 1] == vertexNodeId) return new Vec3D(_geometry.Nodes[cell[0]].Coor) - vertex;
            }
            return new Vec3D();
        }
        /// <summary>
        /// The number of vertices at which an odd number of the given edges end: two for every chain the
        /// edges form, none for a closed loop.
        /// </summary>
        /// <summary>
        /// Refuses a block with a flat wall whose two chains along the sweep run different ways and are not as long
        /// as each other. A source that reaches round to the far end of the part - the round at the end of a flat
        /// plate, running on to the plane the other end lies in - starts a chain along that end, square to the
        /// chain the rest of the wall follows, and the sweep folds the plate across its own outline. A plate bent
        /// round a corner and swept through its thickness has the chains of its end walls square to each other
        /// just as well, but each of them is the thickness of the plate: the sweep lines only turn with the bend.
        /// A block the transfinite mesher accepts is left alone, as it is by the other measurements of the geometry:
        /// it carries one structured grid whichever face the sweep starts from, and a tapered three sided prism,
        /// whose side edges converge, was refused as a sweep although it meshes as a transfinite block.
        /// </summary>
        private string GetWallChainsError(GeometryPart part, HashSet<int> sourceSurfaceIds, int[] wallSurfaceIds,
                                          int[] targetSurfaceIds, HashSet<int> directionEdgeIds, SweepPartMaps maps)
        {
            if (maps.IsTransfiniteBlock(part, p => IsPartTransfinite((GeometryPart)p, false) == null)) return null;
            //
            VisualizationData vis = part.Visualization;
            if (vis.FaceTypes == null) return null;
            //
            foreach (var surfaceId in wallSurfaceIds)
            {
                if (surfaceId >= vis.FaceTypes.Length || vis.FaceTypes[surfaceId] != GeomFaceType.Plane) continue;
                int[] wallDirectionEdgeIds = vis.FaceEdgeIds[surfaceId].Where(directionEdgeIds.Contains).ToArray();
                if (CountChainEnds(vis, wallDirectionEdgeIds) != 4) continue;
                if (AreWallChainsParallel(vis, wallDirectionEdgeIds)) continue;
                //
                return "The two sides of the sweep side " + GetSweepSurfaceName(part, surfaceId) + " that follow the " +
                       "sweep direction from the source surface/s (" + GetSweepSurfaceNames(part, sourceSurfaceIds) +
                       ") do not run the same way.";
            }
            return null;
        }
        /// <summary>
        /// Whether the edges of a wall that follow the sweep form two chains whose ends lie along directions no
        /// more than 45 degrees apart, or two chains of the same length.
        /// </summary>
        private bool AreWallChainsParallel(VisualizationData vis, int[] directionEdgeIds)
        {
            int[] endVertexIds;
            int[] parent = Enumerable.Range(0, directionEdgeIds.Length).ToArray();
            Func<int, int> find = i =>
            {
                while (parent[i] != i) i = parent[i];
                return i;
            };
            Dictionary<int, List<int>> vertexIdEdgeIndices = new Dictionary<int, List<int>>();
            for (int i = 0; i < directionEdgeIds.Length; i++)
            {
                foreach (var vertexId in vis.GetVertexNodeIdsForEdgeId(directionEdgeIds[i]))
                {
                    if (!vertexIdEdgeIndices.ContainsKey(vertexId)) vertexIdEdgeIndices.Add(vertexId, new List<int>());
                    vertexIdEdgeIndices[vertexId].Add(i);
                }
            }
            foreach (var indices in vertexIdEdgeIndices.Values)
            {
                for (int k = 1; k < indices.Count; k++)
                {
                    int a = find(indices[0]);
                    int b = find(indices[k]);
                    if (a != b) parent[b] = a;
                }
            }
            List<Vec3D> chords = new List<Vec3D>();
            List<double> lengths = new List<double>();
            foreach (var chain in Enumerable.Range(0, directionEdgeIds.Length).GroupBy(find))
            {
                lengths.Add(chain.Sum(i => vis.EdgeLengths != null && directionEdgeIds[i] < vis.EdgeLengths.Length
                                           ? vis.EdgeLengths[directionEdgeIds[i]] : 0));
                Dictionary<int, int> vertexIdDegree = new Dictionary<int, int>();
                foreach (var i in chain)
                {
                    endVertexIds = vis.GetVertexNodeIdsForEdgeId(directionEdgeIds[i]);
                    foreach (var vertexId in endVertexIds)
                    {
                        if (vertexIdDegree.ContainsKey(vertexId)) vertexIdDegree[vertexId]++;
                        else vertexIdDegree[vertexId] = 1;
                    }
                }
                int[] ends = vertexIdDegree.Where(entry => entry.Value % 2 == 1).Select(entry => entry.Key).ToArray();
                if (ends.Length != 2) return true;
                chords.Add(new Vec3D(_geometry.Nodes[ends[1]].Coor) - new Vec3D(_geometry.Nodes[ends[0]].Coor));
            }
            if (chords.Count != 2 || chords[0].Len == 0 || chords[1].Len == 0) return true;
            if (lengths[0] > 0 && lengths[1] > 0 &&
                Math.Abs(lengths[0] - lengths[1]) <= _wallChainsLengthTolerance * Math.Max(lengths[0], lengths[1])) return true;
            return Math.Abs(Vec3D.DotProduct(chords[0], chords[1])) >= _wallChainsCosine * chords[0].Len * chords[1].Len;
        }
        private static int CountChainEnds(VisualizationData vis, IEnumerable<int> edgeIds)
        {
            int[] endVertexIds;
            Dictionary<int, int> vertexIdDegree = new Dictionary<int, int>();
            foreach (var edgeId in edgeIds)
            {
                endVertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                if (endVertexIds[0] == endVertexIds[1]) continue;
                foreach (var vertexId in endVertexIds)
                {
                    if (vertexIdDegree.ContainsKey(vertexId)) vertexIdDegree[vertexId]++;
                    else vertexIdDegree[vertexId] = 1;
                }
            }
            return vertexIdDegree.Count(entry => entry.Value % 2 == 1);
        }
        /// <summary>
        /// Whether the edges of a wall of two loops lie on the source and on the target alone, with edges on both.
        /// </summary>
        private static bool IsRingWall(int[] wallEdgeIds, HashSet<int> sourceEdgeIds, HashSet<int> targetEdgeIds)
        {
            return wallEdgeIds.All(id => sourceEdgeIds.Contains(id) != targetEdgeIds.Contains(id)) &&
                   wallEdgeIds.Any(sourceEdgeIds.Contains) && wallEdgeIds.Any(targetEdgeIds.Contains);
        }
        /// <summary>
        /// The number of closed edge loops the given edges form, edges being joined at their vertices.
        /// </summary>
        private static int CountEdgeLoops(int[] edgeIds, Dictionary<int, HashSet<int>> vertexIdEdgeIds)
        {
            HashSet<int> edgeIdSet = new HashSet<int>(edgeIds);
            HashSet<int> visited = new HashSet<int>();
            Queue<int> pending = new Queue<int>();
            int numLoops = 0;
            foreach (var edgeId in edgeIds)
            {
                if (!visited.Add(edgeId)) continue;
                //
                numLoops++;
                pending.Enqueue(edgeId);
                while (pending.Count > 0)
                {
                    int currentEdgeId = pending.Dequeue();
                    foreach (var entry in vertexIdEdgeIds)
                    {
                        if (!entry.Value.Contains(currentEdgeId)) continue;
                        foreach (var otherEdgeId in entry.Value)
                        {
                            if (edgeIdSet.Contains(otherEdgeId) && visited.Add(otherEdgeId)) pending.Enqueue(otherEdgeId);
                        }
                    }
                }
            }
            return numLoops;
        }
        /// <summary>
        /// Adds to a source the neighbouring surfaces that lie in the same plane. A sub-part wider than the
        /// one it is swept from has its exposed rim there, and the rim is meshed together with the shared
        /// surface so that both conform along the imprint; a shared surface in the same plane joins as
        /// well, and the neighbour behind it is then swept away from it.
        /// </summary>
        /// <summary>
        /// Adds to a source the sweep derived for a part the surfaces leaning on it, where that leaves the
        /// block one target surface instead of several.
        ///
        /// A derived source is the twin of the surface the part is fed through, while the seeds the automatic
        /// search tries take a face together with everything leaning on it within the source angle. The
        /// difference tells on a chamfer or a round at that end of the block: left out of the source it is a
        /// wall, the face it would have rounded stands as a second target surface, and a surface shared with
        /// the next part that ends against that second face cannot be swept through - the sweep stops there.
        /// The chamfered end of one slab of the stack of sweep_21 refuses the whole stack that way, although
        /// the automatic search sweeps it from the very seed this adds.
        ///
        /// Only a source the sweep derived is extended; the one the user selected is the user's. And only
        /// where the block validates both ways and the extension leaves fewer target surfaces, so that a wall
        /// leaning on the source - the side of a tapered block - is not swallowed by it.
        /// </summary>
        private void ExtendDerivedSourceOverLeaningSurfaces(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                                            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds,
                                                            SweepPartMaps maps)
        {
            SweepBlockValidation validation = maps.GetSweepBlockValidation(part, sourceSurfaceIds, false,
                () => ValidateSweepBlock(part, sourceSurfaceIds, surfaceIdSurfaceNeighbourIds, maps, null));
            if (validation.Error != null) return;
            int[] targetSurfaceIds = validation.TargetSurfaceIds;
            if (targetSurfaceIds == null || targetSurfaceIds.Length < 2) return;
            //
            HashSet<int> leaningIds;
            HashSet<int> extendedSourceSurfaceIds = new HashSet<int>(sourceSurfaceIds);
            Dictionary<int, HashSet<int>> leaningSurfaceIds = new Dictionary<int, HashSet<int>>();
            foreach (var entry in maps.GetSurfaceIdPairAngles(_geometry, part))
            {
                if (entry.Value[0] > Globals.SweepSourceAngle) continue;
                AddLeaningSurfaceId(leaningSurfaceIds, entry.Key[0], entry.Key[1]);
                AddLeaningSurfaceId(leaningSurfaceIds, entry.Key[1], entry.Key[0]);
            }
            foreach (var surfaceId in sourceSurfaceIds)
            {
                if (leaningSurfaceIds.TryGetValue(surfaceId, out leaningIds))
                    extendedSourceSurfaceIds.UnionWith(leaningIds);
            }
            if (extendedSourceSurfaceIds.Count == sourceSurfaceIds.Count) return;
            //
            SweepBlockValidation extendedValidation = maps.GetSweepBlockValidation(part, extendedSourceSurfaceIds, false,
                () => ValidateSweepBlock(part, extendedSourceSurfaceIds, surfaceIdSurfaceNeighbourIds, maps, null));
            if (extendedValidation.Error != null) return;
            int[] extendedTargetSurfaceIds = extendedValidation.TargetSurfaceIds;
            if (extendedTargetSurfaceIds == null ||
                extendedTargetSurfaceIds.Length >= targetSurfaceIds.Length) return;
            //
            sourceSurfaceIds.UnionWith(extendedSourceSurfaceIds);
        }
        private void ExtendSourceOverCoplanarSurfaces(GeometryPart part, HashSet<int> sourceSurfaceIds,
                                                      Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds,
                                                      SweepPartMaps maps)
        {
            int surfaceId;
            GeomFaceData faceData;
            GeomFaceData neighbourFaceData;
            VisualizationData vis = part.Visualization;
            double tolerance = 1E-4 * part.BoundingBox.GetDiagonal();
            Dictionary<int, GeomFaceData> surfaceIdFaceData = maps.GetSurfaceIdFaceData(part);
            Queue<int> pendingSurfaceIds = new Queue<int>(sourceSurfaceIds);
            //
            while (pendingSurfaceIds.Count > 0)
            {
                surfaceId = pendingSurfaceIds.Dequeue();
                faceData = GetSurfaceFaceData(vis, surfaceId, surfaceIdFaceData);
                if (faceData.Type != GeomFaceType.Plane) continue;
                //
                foreach (var neighbourId in surfaceIdSurfaceNeighbourIds[surfaceId])
                {
                    if (sourceSurfaceIds.Contains(neighbourId)) continue;
                    //
                    neighbourFaceData = GetSurfaceFaceData(vis, neighbourId, surfaceIdFaceData);
                    if (!AreCoplanar(faceData, neighbourFaceData, tolerance)) continue;
                    //
                    sourceSurfaceIds.Add(neighbourId);
                    pendingSurfaceIds.Enqueue(neighbourId);
                }
            }
        }
        private GeomFaceData GetSurfaceFaceData(VisualizationData vis, int surfaceId,
                                                Dictionary<int, GeomFaceData> surfaceIdFaceData)
        {
            GeomFaceData faceData;
            if (!surfaceIdFaceData.TryGetValue(surfaceId, out faceData))
            {
                faceData = FaceTypeRecognition.GetFaceData(vis, _geometry.Nodes, surfaceId);
                surfaceIdFaceData.Add(surfaceId, faceData);
            }
            return faceData;
        }
        private static bool AreCoplanar(GeomFaceData face1, GeomFaceData face2, double tolerance)
        {
            if (face1.Type != GeomFaceType.Plane || face2.Type != GeomFaceType.Plane) return false;
            if (face1.Axis == null || face2.Axis == null || face1.Point == null || face2.Point == null) return false;
            // The normals are oriented outwards, so the surfaces of one plane on the same solid point alike
            Vec3D normal1 = new Vec3D(face1.Axis);
            Vec3D normal2 = new Vec3D(face2.Axis);
            normal1.Normalize();
            normal2.Normalize();
            if (Vec3D.DotProduct(normal1, normal2) < 0.9999) return false;
            //
            Vec3D offset = new Vec3D(face2.Point) - new Vec3D(face1.Point);
            return Math.Abs(Vec3D.DotProduct(offset, normal1)) <= tolerance;
        }
        private static int[] ToGmshTopologyIds(IEnumerable<int> itemIds, int partId)
        {
            List<int> gmshTopologyIds = new List<int>();
            foreach (var itemId in itemIds) gmshTopologyIds.Add(FeMesh.GmshTopologyId(itemId, partId));
            return gmshTopologyIds.ToArray();
        }
        /// <summary>
        /// The axis, the angle and the middle radius of the revolution of the given source surfaces, read from the
        /// edges that leave their vertices. The axis center is in the coordinates of the geometry nodes.
        /// </summary>
        private string GetRevolveAxisData(GeometryPart part, HashSet<int> surfaceIds, out double r, out double arcAngleDeg,
                                          out double[] axisCenter, out double[] axisDirection)
        {
            r = -1;
            arcAngleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            //
            VisualizationData vis = part.Visualization;
            // Do selected surfaces form a connected surface
            if (!vis.AreSurfacesConnected(surfaceIds.ToArray())) return "The selected surfaces are not connected.";
            //
            Dictionary<int, HashSet<int>> vertexIdEdgeId = vis.GetVertexIdEdgeIds();
            HashSet<int> surfaceVertices = vis.GetVertexNodeIdsForSurfaceIds(surfaceIds.ToArray());
            HashSet<int> surfaceEdgeIds = vis.GetEdgeIdsForSurfaceIds(surfaceIds.ToArray());
            //
            Dictionary<int, HashSet<int>> vertexIdConnectedEdgeIds = new Dictionary<int, HashSet<int>>();
            HashSet<int> allConnectedEdgeIds = new HashSet<int>();
            foreach (var vertexId in surfaceVertices)
            {
                HashSet<int> vertexDirectionEdgeIds = new HashSet<int>(vertexIdEdgeId[vertexId]);
                vertexDirectionEdgeIds.ExceptWith(surfaceEdgeIds);
                vertexIdConnectedEdgeIds.Add(vertexId, vertexDirectionEdgeIds);
                allConnectedEdgeIds.UnionWith(vertexDirectionEdgeIds);
            }
            // Find an axis from a circular edge, then ignore all edges attached to vertices on that axis.
            HashSet<int> directionEdgeIds = null;
            HashSet<int> axisNodeIds = null;
            double axisTolerance = 1E-6 * part.BoundingBox.GetDiagonal();
            int maxNumDirectionEdges = -1;
            foreach (var edgeId in allConnectedEdgeIds)
            {
                if (!TryGetRevolveMeshArcData(vis, new HashSet<int>() { edgeId }, surfaceVertices,
                                              out _, out _, out double[] candidateAxisCenter,
                                              out double[] candidateAxisDirection))
                    continue;
                //
                HashSet<int> candidateAxisNodeIds = new HashSet<int>();
                foreach (var vertexId in surfaceVertices)
                {
                    Vec3D radialDirection = new Vec3D(_geometry.Nodes[vertexId].Coor) -
                                            new Vec3D(candidateAxisCenter);
                    if (Vec3D.CrossProduct(radialDirection, new Vec3D(candidateAxisDirection)).Len <= axisTolerance)
                        candidateAxisNodeIds.Add(vertexId);
                }
                if (candidateAxisNodeIds.Count == 0) continue;
                //
                HashSet<int> candidateNonAxisVertexIds = new HashSet<int>(surfaceVertices);
                candidateNonAxisVertexIds.ExceptWith(candidateAxisNodeIds);
                // Follow each revolved (direction) edge through the vertices where a revolved/side surface
                // is split, so that the full swept angle is measured instead of only the first segment.
                if (TryGetRevolveMeshArcDataAlongChains(vis, candidateNonAxisVertexIds,
                                              vertexIdConnectedEdgeIds, surfaceEdgeIds, surfaceVertices,
                                              vertexIdEdgeId, axisTolerance,
                                              out double candidateR, out double candidateArcAngleDeg,
                                              out double[] candidateAxisCenter2,
                                              out double[] candidateAxisDirection2,
                                              out HashSet<int> candidateChainEdgeIds) &&
                    candidateChainEdgeIds.Count > maxNumDirectionEdges)
                {
                    directionEdgeIds = candidateChainEdgeIds;
                    axisNodeIds = candidateAxisNodeIds;
                    r = candidateR;
                    arcAngleDeg = candidateArcAngleDeg;
                    axisCenter = candidateAxisCenter2;
                    axisDirection = candidateAxisDirection2;
                    maxNumDirectionEdges = candidateChainEdgeIds.Count;
                }
            }
            // Preserve the previous validation for surfaces that do not touch the revolution axis.
            bool arcEdges;
            if (directionEdgeIds == null)
            {
                directionEdgeIds = allConnectedEdgeIds;
                axisNodeIds = new HashSet<int>();
                arcEdges = TryGetRevolveMeshArcData(vis, directionEdgeIds, surfaceVertices,
                                                    out r, out arcAngleDeg, out axisCenter, out axisDirection);
            }
            else arcEdges = true;
            //
            if (!arcEdges) return "The selected surfaces have direction edges of different arc radii.";
            if (r <= 0 || arcAngleDeg <= 0)
                return "The selected surfaces have direction edges with different arc angles or axes.";
            if (!AreNodesOnRevolveAxis(axisNodeIds, axisCenter, axisDirection, axisTolerance))
                return "The selected surfaces contain a vertex without a direction edge that is not on the revolution axis.";
            return null;
        }
        /// <summary>
        /// Finds the surfaces a solid part is revolved from: a connected group of coplanar surfaces whose edges
        /// turn about one axis through one angle, and whose revolution the part is. The part is taken to be the
        /// revolution of the group only when every vertex of it lies on a circle a vertex of the group turns on
        /// and every other surface is either the group turned through the angle or a surface turning about the
        /// axis itself. A hole, a pocket or a boss the revolution does not make therefore refuses the part rather
        /// than being meshed away. The two ends of a revolved part are both such groups, and the first one is
        /// taken. Only a group with a vertex on the axis is taken, unless clearOfAxis is set, and every vertex of the part has to lie on one of
        /// its two ends, since a wall split along the revolution is not one a revolved sweep block can mesh. The targets are the surfaces the group is turned onto, and reversed tells that the group
        /// is turned onto them against the axis direction.
        /// </summary>
        private string DetectRevolveSourceSurfaces(GeometryPart part, out HashSet<int> sourceSurfaceIds,
                                                   out HashSet<int> targetSurfaceIds, out double r, out double angleDeg,
                                                   out double[] axisCenter, out double[] axisDirection,
                                                   out bool reversed, bool clearOfAxis = false)
        {
            sourceSurfaceIds = null;
            targetSurfaceIds = null;
            reversed = false;
            r = -1;
            angleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            //
            VisualizationData vis = part.Visualization;
            if (part.GeometryMetadata == null || vis.FaceTypes == null || vis.FaceTypes.Length != vis.FaceCount)
                return "The part " + part.Name + " carries no CAD surface data, so its source surfaces cannot be " +
                       "found automatically.";
            //
            string error;
            string revolutionError = null;
            foreach (var surfaceIds in GetCoplanarSurfaceGroups(part))
            {
                if (GetRevolveAxisData(part, surfaceIds, out r, out angleDeg, out axisCenter, out axisDirection) != null)
                    continue;
                //
                error = IsPartRevolvedFromSurfaces(part, surfaceIds, angleDeg, axisCenter, axisDirection,
                                                   out targetSurfaceIds, out reversed, clearOfAxis);
                if (error == null)
                {
                    sourceSurfaceIds = surfaceIds;
                    return null;
                }
                if (revolutionError == null) revolutionError = error;
            }
            r = -1;
            angleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            targetSurfaceIds = null;
            reversed = false;
            //
            if (revolutionError != null) return revolutionError;
            return "No planar surface of the part " + part.Name + " has edges that all turn about one axis through " +
                   "one angle, so the part is not revolved from any of its surfaces.";
        }
        /// <summary>
        /// The planar surfaces of the part gathered into groups of connected surfaces lying in one plane, ordered by
        /// their lowest surface id. The planes are read from the CAD geometry data.
        /// </summary>
        private static List<HashSet<int>> GetCoplanarSurfaceGroups(GeometryPart part)
        {
            VisualizationData vis = part.Visualization;
            double tolerance = 1E-4 * part.BoundingBox.GetDiagonal();
            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds = vis.GetSurfaceIdSurfaceNeighbourIds();
            Dictionary<int, double[]> surfaceIdPlane = new Dictionary<int, double[]>();
            double[] plane;
            for (int surfaceId = 0; surfaceId < vis.FaceCount; surfaceId++)
            {
                if (vis.FaceTypes[surfaceId] != GeomFaceType.Plane) continue;
                //
                GeometryMetadataEntity face = part.GeometryMetadata.GetEntity("face", surfaceId + 1);
                if (face == null || !face.TryGetNumbers("axis", out plane) || plane.Length != 6) continue;
                if (new Vec3D(plane[3], plane[4], plane[5]).Len == 0) continue;
                surfaceIdPlane.Add(surfaceId, plane);
            }
            //
            int currentId;
            HashSet<int> group;
            HashSet<int> neighbourIds;
            Queue<int> pendingSurfaceIds = new Queue<int>();
            HashSet<int> groupedSurfaceIds = new HashSet<int>();
            List<HashSet<int>> groups = new List<HashSet<int>>();
            foreach (var surfaceId in surfaceIdPlane.Keys.OrderBy(id => id))
            {
                if (!groupedSurfaceIds.Add(surfaceId)) continue;
                //
                group = new HashSet<int>() { surfaceId };
                pendingSurfaceIds.Enqueue(surfaceId);
                while (pendingSurfaceIds.Count > 0)
                {
                    currentId = pendingSurfaceIds.Dequeue();
                    if (!surfaceIdSurfaceNeighbourIds.TryGetValue(currentId, out neighbourIds)) continue;
                    //
                    foreach (var neighbourId in neighbourIds)
                    {
                        if (groupedSurfaceIds.Contains(neighbourId) || !surfaceIdPlane.ContainsKey(neighbourId)) continue;
                        if (!ArePlanesEqual(surfaceIdPlane[surfaceId], surfaceIdPlane[neighbourId], tolerance)) continue;
                        //
                        groupedSurfaceIds.Add(neighbourId);
                        group.Add(neighbourId);
                        pendingSurfaceIds.Enqueue(neighbourId);
                    }
                }
                groups.Add(group);
            }
            return groups;
        }
        private static bool ArePlanesEqual(double[] plane1, double[] plane2, double tolerance)
        {
            Vec3D normal1 = new Vec3D(plane1[3], plane1[4], plane1[5]);
            Vec3D normal2 = new Vec3D(plane2[3], plane2[4], plane2[5]);
            normal1.Normalize();
            normal2.Normalize();
            if (Math.Abs(Vec3D.DotProduct(normal1, normal2)) < 1 - 1E-6) return false;
            //
            Vec3D offset = new Vec3D(plane2[0], plane2[1], plane2[2]) - new Vec3D(plane1[0], plane1[1], plane1[2]);
            return Math.Abs(Vec3D.DotProduct(offset, normal1)) <= tolerance;
        }
        /// <summary>
        /// Why the part is not the revolution of the source surfaces about the axis through the angle, or null when
        /// it is. The axis center is in the coordinates of the geometry nodes.
        /// </summary>
        private string IsPartRevolvedFromSurfaces(GeometryPart part, HashSet<int> sourceSurfaceIds, double angleDeg,
                                                  double[] axisCenter, double[] axisDirection,
                                                  out HashSet<int> targetSurfaceIds, out bool reversed,
                                                  bool clearOfAxis = false)
        {
            targetSurfaceIds = new HashSet<int>();
            reversed = false;
            VisualizationData vis = part.Visualization;
            double tolerance = 1E-4 * part.BoundingBox.GetDiagonal();
            string sourceNames = GetSweepSurfaceNames(part, sourceSurfaceIds);
            Vec3D center = new Vec3D(axisCenter);
            Vec3D direction = new Vec3D(axisDirection);
            direction.Normalize();
            // The boundary of the source turned through the angle, in whichever sense its edges run, is the boundary
            // of the target. A source split inside its boundary may be turned onto a target that is not, so the
            // vertices inside the source need no vertex on the target, and a vertex that only splits an edge of the
            // source boundary may be turned onto a point along an edge of the target
            HashSet<int> sourceVertexIds = vis.GetVertexNodeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
            Dictionary<int, int> edgeUseCount = new Dictionary<int, int>();
            foreach (var surfaceId in sourceSurfaceIds)
            {
                foreach (var edgeId in vis.FaceEdgeIds[surfaceId].Distinct())
                {
                    edgeUseCount.TryGetValue(edgeId, out int count);
                    edgeUseCount[edgeId] = count + 1;
                }
            }
            HashSet<int> boundaryVertexIds =
                vis.GetVertexNodeIdsForEdgeIds(edgeUseCount.Where(entry => entry.Value == 1).Select(entry => entry.Key)
                                                           .ToArray());
            List<int> unmatchedVertexIds;
            List<int> reversedUnmatchedVertexIds;
            HashSet<int> targetVertexIds = GetRevolvedVertexIds(vis, boundaryVertexIds, center, direction, angleDeg,
                                                                tolerance, out unmatchedVertexIds);
            HashSet<int> reversedVertexIds = GetRevolvedVertexIds(vis, boundaryVertexIds, center, direction, -angleDeg,
                                                                  tolerance, out reversedUnmatchedVertexIds);
            if (reversedUnmatchedVertexIds.Count < unmatchedVertexIds.Count)
            {
                targetVertexIds = reversedVertexIds;
                unmatchedVertexIds = reversedUnmatchedVertexIds;
                reversed = true;
            }
            if (!targetVertexIds.Except(boundaryVertexIds).Any())
                return "The revolution of " + sourceNames + " about its axis ends on no vertices of the part " +
                       part.Name + ", so the part is not revolved from it.";
            // Every vertex lies on a circle a vertex of the source turns on
            double radius;
            double height;
            bool onAxis = false;
            List<double[]> sourceCircles = new List<double[]>();
            foreach (var vertexId in sourceVertexIds)
            {
                GetAxialCoordinates(_geometry.Nodes[vertexId].Coor, center, direction, out radius, out height);
                sourceCircles.Add(new double[] { radius, height });
                if (radius <= tolerance) onAxis = true;
            }
            if (!onAxis && !clearOfAxis)
                return "The " + sourceNames + " does not touch the axis it is revolved about, so the part " + part.Name +
                       " is swept rather than revolved.";
            Dictionary<int, HashSet<int>> surfaceIdVertexIds = new Dictionary<int, HashSet<int>>();
            for (int surfaceId = 0; surfaceId < vis.FaceCount; surfaceId++)
                surfaceIdVertexIds.Add(surfaceId, new HashSet<int>(vis.GetVertexNodeIdsForSurfaceId(surfaceId)));
            foreach (var vertexId in vis.VertexNodeIds)
            {
                GetAxialCoordinates(_geometry.Nodes[vertexId].Coor, center, direction, out radius, out height);
                if (sourceCircles.Any(circle => Math.Abs(circle[0] - radius) <= tolerance &&
                                                Math.Abs(circle[1] - height) <= tolerance)) continue;
                //
                int[] vertexSurfaceIds = surfaceIdVertexIds.Where(entry => entry.Value.Contains(vertexId))
                                                           .Select(entry => entry.Key).ToArray();
                return "A vertex of " + GetSweepSurfaceNames(part, vertexSurfaceIds) + " lies on no circle a " +
                       "vertex of " + sourceNames + " turns on, so the part is not revolved from it.";
            }
            // Every other surface is the target or a surface turning about the axis
            Vec3D axisPoint = center - new Vec3D(part.Offset);
            HashSet<int> surfaceVertexIds;
            for (int surfaceId = 0; surfaceId < vis.FaceCount; surfaceId++)
            {
                if (sourceSurfaceIds.Contains(surfaceId)) continue;
                //
                surfaceVertexIds = surfaceIdVertexIds[surfaceId];
                if (vis.FaceTypes[surfaceId] == GeomFaceType.Plane && surfaceVertexIds.IsSubsetOf(targetVertexIds) &&
                    !surfaceVertexIds.IsSubsetOf(sourceVertexIds))
                {
                    targetSurfaceIds.Add(surfaceId);
                    continue;
                }
                //
                if (!DoesSurfaceTurnAboutAxis(part, surfaceId, axisPoint, direction, tolerance))
                    return "The " + GetSweepSurfaceName(part, surfaceId) + " does not turn about the axis of " +
                           sourceNames + ", so the part is not revolved from it.";
            }
            if (targetSurfaceIds.Count == 0)
                return "The revolution of " + sourceNames + " about its axis ends on no surface of the part " + part.Name +
                       ", so the part is not revolved from it.";
            // A vertex splitting an edge of the source boundary is turned onto an edge of the target boundary
            HashSet<int> targetEdgeIds = vis.GetEdgeIdsForSurfaceIds(targetSurfaceIds.ToArray());
            double angleRad = (reversed ? -angleDeg : angleDeg) * Math.PI / 180;
            foreach (var vertexId in unmatchedVertexIds)
            {
                Vec3D turned = RotateAboutAxis(new Vec3D(_geometry.Nodes[vertexId].Coor), center, direction, angleRad);
                if (targetEdgeIds.Any(edgeId => GetDistanceToEdge(vis, edgeId, turned) <= tolerance)) continue;
                //
                return "The revolution of " + sourceNames + " about its axis does not end on the boundary of " +
                       GetSweepSurfaceNames(part, targetSurfaceIds) + ", so the part is not revolved from it.";
            }
            return null;
        }
        private double GetDistanceToEdge(VisualizationData vis, int edgeId, Vec3D point)
        {
            int[] nodeIds = vis.GetOrderedNodeIdsForEdgeId(edgeId);
            double minDistance = double.MaxValue;
            for (int i = 0; i < nodeIds.Length - 1; i++)
            {
                Vec3D start = new Vec3D(_geometry.Nodes[nodeIds[i]].Coor);
                Vec3D segment = new Vec3D(_geometry.Nodes[nodeIds[i + 1]].Coor) - start;
                double length2 = segment.Len2;
                double t = length2 > 0 ? Vec3D.DotProduct(point - start, segment) / length2 : 0;
                t = Math.Max(0, Math.Min(1, t));
                minDistance = Math.Min(minDistance, (start + t * segment - point).Len);
            }
            return minDistance;
        }
        private static Vec3D RotateAboutAxis(Vec3D point, Vec3D axisCenter, Vec3D axisDirection, double angleRad)
        {
            // Rodrigues' rotation of the arm from the axis center
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            Vec3D arm = point - axisCenter;
            return axisCenter + cos * arm + sin * Vec3D.CrossProduct(axisDirection, arm) +
                   ((1 - cos) * Vec3D.DotProduct(axisDirection, arm)) * axisDirection;
        }
        /// <summary>
        /// The vertices the given vertices are turned onto, and the vertices that are turned onto none.
        /// </summary>
        private HashSet<int> GetRevolvedVertexIds(VisualizationData vis, HashSet<int> vertexIds, Vec3D axisCenter,
                                                  Vec3D axisDirection, double angleDeg, double tolerance,
                                                  out List<int> unmatchedVertexIds)
        {
            unmatchedVertexIds = new List<int>();
            double angle = angleDeg * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            Vec3D arm;
            Vec3D rotated;
            bool found;
            HashSet<int> revolvedVertexIds = new HashSet<int>();
            foreach (var vertexId in vertexIds)
            {
                arm = new Vec3D(_geometry.Nodes[vertexId].Coor) - axisCenter;
                rotated = axisCenter + cos * arm + sin * Vec3D.CrossProduct(axisDirection, arm) +
                          ((1 - cos) * Vec3D.DotProduct(axisDirection, arm)) * axisDirection;
                //
                found = false;
                foreach (var candidateId in vis.VertexNodeIds)
                {
                    if ((new Vec3D(_geometry.Nodes[candidateId].Coor) - rotated).Len > tolerance) continue;
                    //
                    revolvedVertexIds.Add(candidateId);
                    found = true;
                    break;
                }
                if (!found) unmatchedVertexIds.Add(vertexId);
            }
            return revolvedVertexIds;
        }
        private static void GetAxialCoordinates(double[] coor, Vec3D axisCenter, Vec3D axisDirection, out double radius,
                                                out double height)
        {
            Vec3D arm = new Vec3D(coor) - axisCenter;
            height = Vec3D.DotProduct(arm, axisDirection);
            radius = Vec3D.CrossProduct(arm, axisDirection).Len;
        }
        /// <summary>
        /// Whether the CAD geometry of the surface turns about the axis: a plane square to it, or a cylinder, a cone,
        /// a sphere, a torus or a surface of revolution about it. The axis point is in the CAD coordinates.
        /// </summary>
        private static bool DoesSurfaceTurnAboutAxis(GeometryPart part, int surfaceId, Vec3D axisPoint,
                                                     Vec3D axisDirection, double tolerance)
        {
            GeometryMetadataEntity face = part.GeometryMetadata.GetEntity("face", surfaceId + 1);
            if (face == null) return false;
            //
            double[] values;
            double radius;
            double height;
            GeomFaceType faceType = part.Visualization.FaceTypes[surfaceId];
            if (faceType == GeomFaceType.Sphere)
            {
                if (!face.TryGetNumbers("center", out values) || values.Length != 3) return false;
                GetAxialCoordinates(values, axisPoint, axisDirection, out radius, out height);
                return radius <= tolerance;
            }
            if (faceType != GeomFaceType.Plane && faceType != GeomFaceType.Cylinder && faceType != GeomFaceType.Cone &&
                faceType != GeomFaceType.Torus && faceType != GeomFaceType.SurfaceOfRevolution) return false;
            if (!face.TryGetNumbers("axis", out values) || values.Length != 6) return false;
            //
            Vec3D direction = new Vec3D(values[3], values[4], values[5]);
            if (direction.Len == 0) return false;
            direction.Normalize();
            if (Math.Abs(Vec3D.DotProduct(direction, axisDirection)) < 1 - 1E-6) return false;
            // A plane square to the axis may stand anywhere along it
            if (faceType == GeomFaceType.Plane) return true;
            //
            GetAxialCoordinates(new double[] { values[0], values[1], values[2] }, axisPoint, axisDirection,
                                out radius, out height);
            return radius <= tolerance;
        }
        private bool TryGetRevolveMeshArcData(VisualizationData vis, HashSet<int> edgeIds,
                                               HashSet<int> surfaceVertices, out double r, out double arcAngleDeg,
                                               out double[] axisCenter, out double[] axisDirection)
        {
            r = -1;
            arcAngleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            //
            bool arcEdges = false;
            if (vis.EdgeTypes != null)
            {
                HashSet<GeomCurveType> edgeTypes = new HashSet<GeomCurveType>();
                foreach (var edgeId in edgeIds) edgeTypes.Add(vis.EdgeTypes[edgeId]);
                //
                if (edgeTypes.Count() == 1 && edgeTypes.First() == GeomCurveType.Circle) arcEdges = true;
            }
            if (!arcEdges) arcEdges = vis.IsEdgeRadiusConstant(edgeIds, _geometry.Nodes);
            if (!arcEdges) return false;
            //
            vis.GetArcEdgeDataForEdgeIds(edgeIds, surfaceVertices, _geometry.Nodes,
                                         out r, out arcAngleDeg, out axisCenter, out axisDirection);
            return r > 0 && arcAngleDeg > 0;
        }
        private bool TryGetRevolveMeshArcDataAlongChains(VisualizationData vis,
                                                         HashSet<int> nonAxisSourceVertexIds,
                                                         Dictionary<int, HashSet<int>> vertexIdConnectedEdgeIds,
                                                         HashSet<int> surfaceEdgeIds, HashSet<int> surfaceVertices,
                                                         Dictionary<int, HashSet<int>> vertexIdEdgeIds,
                                                         double tolerance, out double r, out double arcAngleDeg,
                                                         out double[] axisCenter, out double[] axisDirection,
                                                         out HashSet<int> chainEdgeIds)
        {
            r = -1;
            arcAngleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            chainEdgeIds = new HashSet<int>();
            //
            double minAngle = double.MaxValue;
            double maxAngle = -double.MaxValue;
            double minR = double.MaxValue;
            double maxR = -double.MaxValue;
            BoundingBox bbCenter = new BoundingBox();
            BoundingBox bbDirection = new BoundingBox();
            bool any = false;
            // Each non-axis source vertex starts one revolved edge. Walk each along its full chain of split
            // segments so that all revolved edges are required to share the same axis, radius and swept angle.
            foreach (int vertexId in nonAxisSourceVertexIds)
            {
                foreach (int startEdgeId in vertexIdConnectedEdgeIds[vertexId])
                {
                    if (chainEdgeIds.Contains(startEdgeId)) continue;
                    if (!TryGetRevolvedEdgeChain(vis, startEdgeId, vertexId, surfaceEdgeIds, surfaceVertices,
                                                 vertexIdEdgeIds, tolerance, out HashSet<int> oneChainEdgeIds,
                                                 out double chainAngleDeg, out double chainR,
                                                 out double[] chainCenter, out double[] chainDirection))
                        return false;
                    //
                    chainEdgeIds.UnionWith(oneChainEdgeIds);
                    if (chainAngleDeg < minAngle) minAngle = chainAngleDeg;
                    if (chainAngleDeg > maxAngle) maxAngle = chainAngleDeg;
                    if (chainR < minR) minR = chainR;
                    if (chainR > maxR) maxR = chainR;
                    bbCenter.IncludeCoor(chainCenter);
                    bbDirection.IncludeCoor(chainDirection);
                    any = true;
                }
            }
            if (!any) return false;
            // The revolved edges must span the same angle about the same axis. Their radii differ (each source
            // vertex is a different distance from the axis), so only a representative middle radius is returned -
            // exactly as GetArcEdgeDataForEdgeIds does for the unsplit case.
            if (minAngle <= 0 || maxAngle - minAngle > 1E-3 * maxAngle) return false;
            if (minR <= 0) return false;
            if (bbDirection.GetDiagonal() >= 0.0017) return false;   // approximately 0.1 degree difference
            //
            r = (minR + maxR) / 2;
            arcAngleDeg = (minAngle + maxAngle) / 2;
            axisCenter = bbCenter.GetCenter();
            axisDirection = bbDirection.GetCenter();
            return true;
        }
        private bool TryGetRevolvedEdgeChain(VisualizationData vis, int startEdgeId, int startVertexId,
                                             HashSet<int> surfaceEdgeIds, HashSet<int> surfaceVertices,
                                             Dictionary<int, HashSet<int>> vertexIdEdgeIds, double tolerance,
                                             out HashSet<int> chainEdgeIds, out double totalArcAngleDeg,
                                             out double r, out double[] axisCenter, out double[] axisDirection)
        {
            chainEdgeIds = new HashSet<int>();
            totalArcAngleDeg = 0;
            r = -1;
            axisCenter = null;
            axisDirection = null;
            //
            int currentEdgeId = startEdgeId;
            int fromVertexId = startVertexId;
            while (true)
            {
                vis.GetArcEdgeDataForEdgeId(currentEdgeId, new HashSet<int>() { fromVertexId }, _geometry.Nodes,
                                            out double edgeR, out double edgeAngleDeg,
                                            out double[] edgeCenter, out double[] edgeDirection);
                if (edgeCenter == null || edgeR <= 0 || double.IsInfinity(edgeR) || edgeAngleDeg <= 0)
                {
                    // The direction edge is a line or could not be evaluated as an arc.
                    if (chainEdgeIds.Count == 0) return false;
                    break;
                }
                if (chainEdgeIds.Count == 0)
                {
                    r = edgeR;
                    axisCenter = edgeCenter;
                    axisDirection = edgeDirection;
                }
                else if (!RevolveArcsMatch(edgeR, edgeCenter, edgeDirection, r, axisCenter, axisDirection, tolerance))
                {
                    break;   // the next segment is a different circle - the chain has ended
                }
                chainEdgeIds.Add(currentEdgeId);
                totalArcAngleDeg += edgeAngleDeg;
                // Move to the far vertex of the current edge
                int[] edgeVertexIds = vis.GetVertexNodeIdsForEdgeId(currentEdgeId);
                if (edgeVertexIds.Length != 2) break;
                int farVertexId = edgeVertexIds[0] == fromVertexId ? edgeVertexIds[1] : edgeVertexIds[0];
                if (farVertexId == fromVertexId || surfaceVertices.Contains(farVertexId)) break;
                // Find a single arc continuation at the far vertex that shares the axis and radius
                int nextEdgeId = -1;
                if (vertexIdEdgeIds.TryGetValue(farVertexId, out HashSet<int> farVertexEdgeIds))
                {
                    foreach (int candidateEdgeId in farVertexEdgeIds)
                    {
                        if (candidateEdgeId == currentEdgeId || surfaceEdgeIds.Contains(candidateEdgeId) ||
                            chainEdgeIds.Contains(candidateEdgeId)) continue;
                        vis.GetArcEdgeDataForEdgeId(candidateEdgeId, new HashSet<int>() { farVertexId },
                                                    _geometry.Nodes, out double candR, out double candAngleDeg,
                                                    out double[] candCenter, out double[] candDirection);
                        if (candCenter == null || candR <= 0 || double.IsInfinity(candR) || candAngleDeg <= 0)
                            continue;
                        if (RevolveArcsMatch(candR, candCenter, candDirection, r, axisCenter, axisDirection, tolerance))
                        {
                            if (nextEdgeId >= 0) { nextEdgeId = -1; break; }   // ambiguous branch - stop here
                            nextEdgeId = candidateEdgeId;
                        }
                    }
                }
                if (nextEdgeId < 0) break;
                currentEdgeId = nextEdgeId;
                fromVertexId = farVertexId;
            }
            return chainEdgeIds.Count > 0 && totalArcAngleDeg > 0 && r > 0;
        }
        private static bool RevolveArcsMatch(double r1, double[] center1, double[] direction1,
                                             double r2, double[] center2, double[] direction2, double tolerance)
        {
            double radiusTolerance = Math.Max(tolerance, 1E-3 * r2);
            if (Math.Abs(r1 - r2) > radiusTolerance) return false;
            if ((new Vec3D(center1) - new Vec3D(center2)).Len > Math.Max(tolerance, 1E-3 * r2)) return false;
            //
            Vec3D u1 = new Vec3D(direction1);
            Vec3D u2 = new Vec3D(direction2);
            double denominator = u1.Len * u2.Len;
            if (denominator <= 0) return false;
            // The continuation must wind the same way (same axis direction), not just lie on a parallel axis.
            if (Vec3D.DotProduct(u1, u2) <= 0) return false;
            return Vec3D.CrossProduct(u1, u2).Len / denominator < 1.7E-3;   // approximately 0.1 degree
        }
        private bool AreNodesOnRevolveAxis(HashSet<int> nodeIds, double[] axisCenter,
                                            double[] axisDirection, double tolerance)
        {
            Vec3D center = new Vec3D(axisCenter);
            Vec3D direction = new Vec3D(axisDirection);
            direction.Normalize();
            //
            foreach (var nodeId in nodeIds)
            {
                Vec3D radialDirection = new Vec3D(_geometry.Nodes[nodeId].Coor) - center;
                if (Vec3D.CrossProduct(radialDirection, direction).Len > tolerance) return false;
            }
            return true;
        }
        public bool IsPartCompoundSubPart(string partName)
        {
            return GetCompoundPartOfSubPart(partName) != null;
        }
        public CompoundGeometryPart GetCompoundPartOfSubPart(string partName)
        {
            if (_geometry == null) return null;
            //
            foreach (var entry in _geometry.Parts)
            {
                if (entry.Value is CompoundGeometryPart cgp && cgp.SubPartNames.Contains(partName)) return cgp;
            }
            return null;
        }
        private int[][][] GetDirectionEdges(VisualizationData vis, Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds,
                                            int[] surfaceIds, List<int[]> layerSurfaceIds)
        {
            // Split into groups
            Node<int> node;
            Graph<int> connections = new Graph<int>();
            Dictionary<int, Node<int>> surfaceIdNode = new Dictionary<int, Node<int>>();
            //
            foreach (var surfaceId in surfaceIds)
            {
                node = new Node<int>(surfaceId);
                connections.AddNode(node);
                surfaceIdNode.Add(surfaceId, node);
            }
            HashSet<int> surfaceIdsHash = surfaceIds.ToHashSet();
            foreach (var surfaceId in surfaceIds)
            {
                foreach (var neighbourId in surfaceIdSurfaceNeighbourIds[surfaceId])
                {
                    if (surfaceIdsHash.Contains(neighbourId))
                        connections.AddUndirectedEdge(surfaceIdNode[surfaceId], surfaceIdNode[neighbourId]);
                }
            }
            List<Graph<int>> subGraphs = connections.GetConnectedSubgraphs();
            //
            int count = 0;
            HashSet<int>[] groupSurfaceId = new HashSet<int>[subGraphs.Count];
            foreach (var subGraph in subGraphs) groupSurfaceId[count++] = subGraph.GetValues().ToHashSet();
            // Create group mask
            Dictionary<int, int> surfaceIdGroupId = new Dictionary<int, int>();
            for (int i = 0; i < groupSurfaceId.Length; i++)
            {
                foreach (var surfaceId in groupSurfaceId[i])
                {
                    surfaceIdGroupId.Add(surfaceId, i);
                }
            }
            // Create layer mask
            Dictionary<int, int> surfaceIdLayerId = new Dictionary<int, int>();
            count = 0;
            foreach (var layer in layerSurfaceIds)
            {
                for (int i = 0; i < layer.Length; i++)
                {
                    surfaceIdLayerId.Add(layer[i], count);
                }
                count++;
            }
            //
            HashSet<int>[][] layerGroupSurfaceIds = new HashSet<int>[layerSurfaceIds.Count][];
            for (int i = 0; i < layerGroupSurfaceIds.Length; i++)
            {
                layerGroupSurfaceIds[i] = new HashSet<int>[groupSurfaceId.Length];
                for (int j = 0; j < layerGroupSurfaceIds[i].Length; j++) layerGroupSurfaceIds[i][j] = new HashSet<int>();
            }
            //
            int groupId;
            int layerId;
            for (int i = 0; i < surfaceIds.Length; i++)
            {
                groupId = surfaceIdGroupId[surfaceIds[i]];
                layerId = surfaceIdLayerId[surfaceIds[i]];
                layerGroupSurfaceIds[layerId][groupId].Add(surfaceIds[i]);
            }
            //
            int[][][] layerGroupEdgeIds = new int[layerSurfaceIds.Count][][];
            for (int i = 0; i < layerGroupEdgeIds.Length; i++)
            {
                layerGroupEdgeIds[i] = new int[groupSurfaceId.Length][];
                for (int j = 0; j < layerGroupEdgeIds[i].Length; j++)
                    layerGroupEdgeIds[i][j] = GetSharedEdgeIds(vis, layerGroupSurfaceIds[i][j]);
            }
            //
            return layerGroupEdgeIds;
        }
        private int[] GetSharedEdgeIds(VisualizationData vis, HashSet<int> surfaceIds)
        {
            int directionEdgeId;
            Dictionary<int, int> edgeIdCount = new Dictionary<int, int>();
            foreach (var surfaceId in surfaceIds)
            {
                if (vis.IsSurfaceACylinderLike(surfaceId, out directionEdgeId))
                {
                    if (edgeIdCount.ContainsKey(directionEdgeId)) edgeIdCount[directionEdgeId] += 2;
                    else edgeIdCount[directionEdgeId] = 2;
                }
                else
                {
                    foreach (var edgeId in vis.FaceEdgeIds[surfaceId])
                    {
                        if (edgeIdCount.ContainsKey(edgeId)) edgeIdCount[edgeId]++;
                        else edgeIdCount[edgeId] = 1;
                    }
                }
            }
            List<int> edgeIds = new List<int>();
            foreach (var entry in edgeIdCount)
            {
                if (entry.Value == 2) edgeIds.Add(entry.Key);
            }
            return edgeIds.ToArray();
        }
        // Springs                                                                                  
        public PointSpringData[] GetPointSpringsFromSurfaceSpring(SurfaceSpring spring)
        {
            List<PointSpringData> springs = new List<PointSpringData>();
            //
            if (spring.GetSpringDirections().Length != 0)
            {
                double area;
                Dictionary<int, double> nodalStiffnesses;
                GetDistributedNodalValuesFromSurface(spring.RegionName, out nodalStiffnesses, out area);
                //
                if (spring.StiffnessPerArea) area = 1;      // account for the stiffness type
                //
                double k1ByArea = spring.K1.Value / area;
                double k2ByArea = spring.K2.Value / area;
                double k3ByArea = spring.K3.Value / area;
                //
                foreach (var entry in nodalStiffnesses)
                {
                    if (entry.Value != 0)
                    {
                        springs.Add(new PointSpringData(spring.Name + "_" + entry.Key.ToString(), entry.Key,
                                    k1ByArea  * entry.Value,
                                    k2ByArea  * entry.Value,
                                    k3ByArea  * entry.Value));
                    }
                }
            }
            //
            return springs.ToArray();
        }
        // Initial conditions                                                                       
        public InitialTemperature[] GetNodalTemperaturesFromVariableInitialTemperature(InitialTemperature initialTemperature)
        {
            if (initialTemperature.DistributionName == Distribution.DefaultDistributionName) return null;
            int[] nodeIds;
            double[][] coor;
            _mesh.GetRegionNodeIdsAndCoordinates(initialTemperature.RegionName, initialTemperature.RegionType,
                                                 out nodeIds, out coor);
            // Temperature
            double[] temperatures;
            initialTemperature.GetTemperaturesAndDistancesForPoints(this, coor, out _, out temperatures);
            //
            InitialTemperature[] initialTemperatures = new InitialTemperature[nodeIds.Length];
            Parallel.For(0, nodeIds.Length, i =>
            {
                int nodeId = nodeIds[i];
                // Temperature conditions
                InitialTemperature it = new InitialTemperature("NodeID_" + nodeId.ToString(), nodeId, temperatures[i], true);
                initialTemperatures[i] = it;
            });
            //
            return initialTemperatures;
        }
        // Boundary conditions                                                                      
        public TemperatureBC[] GetNodalTemperaturesFromVariableTemperatureBC(TemperatureBC temperatureBC)
        {
            if (temperatureBC.DistributionName == Distribution.DefaultDistributionName) return null;
            int[] nodeIds;
            double[][] coor;
            _mesh.GetRegionNodeIdsAndCoordinates(temperatureBC.RegionName, temperatureBC.RegionType,
                                                 out nodeIds, out coor);
            // Temperature
            double[] temperatures;
            temperatureBC.GetTemperaturesAndDistancesForPoints(this, coor, out _, out temperatures);
            //
            TemperatureBC[] temperatureBCs = new TemperatureBC[nodeIds.Length];
            Parallel.For(0, nodeIds.Length, i =>
            {
                int nodeId = nodeIds[i];
                // Temperature bcs
                TemperatureBC tbc = new TemperatureBC("NodeID_" + nodeId.ToString(), nodeId, temperatures[i], true);
                tbc.AmplitudeName = temperatureBC.AmplitudeName;
                temperatureBCs[i] = tbc;
            });
            //
            return temperatureBCs;
        }
        public double[][] GetNodalValuesFromVariableDisplacementRotation(DisplacementRotation displacementRotation,
                                                                         out int[] nodeIds)
        {
            nodeIds = null;
            if (displacementRotation.DistributionName == Distribution.DefaultDistributionName) return null;
            if (displacementRotation.RegionType == RegionTypeEnum.ReferencePointName)
                throw new CaeException("A distribution cannot be applied to a reference point.");
            //
            double[][] coor;
            _mesh.GetRegionNodeIdsAndCoordinates(displacementRotation.RegionName, displacementRotation.RegionType,
                                                 out nodeIds, out coor);
            //
            return displacementRotation.GetConstrainValuesForPoints(this, coor);
        }
        // Loads                                                                                    
        public double GetAreaForSTLoad(STLoad load)
        {
            int sectionId = 0;
            double A;
            double thickness;
            double area = 0;
            FeElement element;
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            Dictionary<int, double> sectionIdThickness = new Dictionary<int, double>();
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            foreach (var section in assignedSections)
            {
                thickness = section.Thickness.Value;
                sectionIdThickness.Add(sectionId++, thickness);
            }
            //
            FeSurface surface = _mesh.Surfaces[load.SurfaceName];
            if (surface.ElementFaces == null) return 0;
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    A = element.GetArea(entry.Key, _mesh.Nodes);
                    // Is shell edge face
                    if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                    {
                        sectionId = elementIdSectionId[elementId];
                        if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                    " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                        thickness = sectionIdThickness[sectionId];
                        A *= thickness;
                    }
                    area += A;
                }
            }
            return area;
        }
        public CLoad[] GetNodalCLoadsFromSurfaceTraction(STLoad load)
        {
            if (load.DistributionName == Distribution.DefaultDistributionName)
                return GetNodalCLoadsFromConstantSurfaceTraction(load);
            else
                return GetNodalCLoadsFromSurfaceTractionByDistribution(load);
        }
        public CLoad[] GetNodalCLoadsFromConstantSurfaceTraction(STLoad load)
        {
            List<CLoad> loads = new List<CLoad>();
            //
            if (load.FMagnitude.Value != 0)
            {
                double area;
                Dictionary<int, double> nodalForces;
                GetDistributedNodalValuesFromSurface(load.SurfaceName, out nodalForces, out area);
                //
                double f1ByArea = load.F1.Value / area;
                double f2ByArea = load.F2.Value / area;
                double f3ByArea = load.F3.Value / area;
                //
                CLoad cLoad;
                double phaseDeg = load.PhaseDeg.Value;
                foreach (var entry in nodalForces)
                {
                    if (entry.Value != 0)
                    {
                        cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                          f1ByArea * entry.Value,
                                          f2ByArea * entry.Value,
                                          f3ByArea * entry.Value, load.TwoD, load.Complex, phaseDeg, true);
                        cLoad.AmplitudeName = load.AmplitudeName;
                        loads.Add(cLoad);
                    }
                }
            }
            //
            return loads.ToArray();
        }
        public CLoad[] GetNodalCLoadsFromSurfaceTractionByDistribution(STLoad load)
        {
            Distribution distribution = _distributions[load.DistributionName];
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            //
            int sectionId = 0;
            double thickness;
            string surfaceName = load.SurfaceName;
            double[] sectionIdThickness = new double[assignedSections.Count];
            foreach (var section in assignedSections)
            {
                thickness = section.Thickness.Value;
                sectionIdThickness[sectionId++] = thickness;
            }
            // Surface
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return null;
            //
            int nodeId;
            int[] nodeIds;
            double A;
            double area = 0;
            double[] magnitude;
            double[] force;
            double[] nodalForce;
            double[] faceNormal;
            double[][] magnitudeByComponent;
            double[][] nodalForceMagnitudes;
            FeElement element;
            Dictionary<int, double[]> nodeIdForce = new Dictionary<int, double[]>();
            // Collect all node ids - speed up
            HashSet<int> allNodeIdsHash = new HashSet<int>();
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    allNodeIdsHash.UnionWith(element.GetNodeIdsFromFaceName(entry.Key));
                }
            }
            // Collect all points - speed up
            int[] allNodeIds = allNodeIdsHash.ToArray();
            double[][] points = new double[allNodeIds.Length][];
            for (int i = 0; i < allNodeIds.Length; i++)
            {
                nodeId = allNodeIds[i];
                points[i] = _mesh.Nodes[nodeId].Coor;
            }
            // Get all nodal values
            double[][] values;
            distribution.GetMagnitudesAndDistancesForPoints(this, points, out values, out _);
            // Get all nodal values as dictionary - speed up
            Dictionary<int, double[]> nodeIdValues = new Dictionary<int, double[]>();
            for (int i = 0; i < allNodeIds.Length; i++) nodeIdValues.Add(allNodeIds[i], values[i]);
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    // Node ids
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    //
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out double[] faceCenter, out faceNormal,
                                                        out bool shellElement);
                    //
                    A = element.GetArea(entry.Key, _mesh.Nodes);
                    // Account for 2D area when an edge is selected
                    if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                    {
                        sectionId = elementIdSectionId[elementId];
                        if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                    " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                        thickness = sectionIdThickness[sectionId];
                        A *= thickness;
                    }
                    // Force magnitude
                    magnitudeByComponent = new double[distribution.NumOfComponents][];
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        nodeId = nodeIds[i];
                        magnitude = nodeIdValues[nodeId];
                        //
                        for (int j = 0; j < magnitudeByComponent.Length; j++)
                        {
                            if (magnitudeByComponent[j] == null) magnitudeByComponent[j] = new double[nodeIds.Length];
                            magnitudeByComponent[j][i] = magnitude[j];
                        }
                    }
                    // Force magnitudes without area
                    nodalForceMagnitudes = new double[distribution.NumOfComponents][];
                    for (int i = 0; i < nodalForceMagnitudes.Length; i++)
                    {
                        nodalForceMagnitudes[i] = element.GetEquivalentForcesFromFaceName(entry.Key, magnitudeByComponent[i]);
                    }
                    // Force vectors
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        if (distribution.NumOfComponents == 1)
                        {
                            force = new double[] { A * nodalForceMagnitudes[0][i],
                                                   A * nodalForceMagnitudes[0][i],
                                                   A * nodalForceMagnitudes[0][i] };
                        }
                        else if (distribution.NumOfComponents == 3)
                        {
                            force = new double[] { A * nodalForceMagnitudes[0][i],
                                                   A * nodalForceMagnitudes[1][i],
                                                   A * nodalForceMagnitudes[2][i] };
                        }
                        else throw new NotSupportedException();
                        //
                        if (!nodeIdForce.TryGetValue(nodeIds[i], out nodalForce))
                        {
                            nodalForce = new double[3];
                            nodeIdForce.Add(nodeIds[i], nodalForce);
                        }
                        nodalForce[0] += force[0];
                        nodalForce[1] += force[1];
                        nodalForce[2] += force[2];
                    }
                    //
                    area += A;
                }
            }
            // Concentrated loads
            CLoad cLoad;
            List<CLoad> loads = new List<CLoad>();
            double phaseDeg = load.PhaseDeg.Value;
            double p1ByArea = load.P1.Value;
            double p2ByArea = load.P2.Value;
            double p3ByArea = load.P3.Value;
            foreach (var entry in nodeIdForce)
            {
                if (entry.Value[0] != 0 || entry.Value[1] != 0 || entry.Value[2] != 0)
                {
                    cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                      p1ByArea * entry.Value[0],
                                      p2ByArea * entry.Value[1],
                                      p3ByArea * entry.Value[2],
                                      load.TwoD, load.Complex, phaseDeg, true);
                    //
                    cLoad.AmplitudeName = load.AmplitudeName;
                    //
                    loads.Add(cLoad);
                }
            }
            //
            return loads.ToArray();
        }
        public void GetDistributedNodalValuesFromSurface(string surfaceName,
                                                         out Dictionary<int, double> nodalValues,
                                                         out double area)
        {
            nodalValues = new Dictionary<int, double>();
            area = 0;
            //
            int nodeId;
            int sectionId = 0;
            int[] nodeIds;
            double A;
            double thickness;
            double[] equValue;
            FeElement element;
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            Dictionary<int, double> sectionIdThickness = new Dictionary<int, double>();
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            foreach (var section in assignedSections)
            {
                thickness = section.Thickness.Value;
                sectionIdThickness.Add(sectionId++, thickness);
            }
            //
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return;
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    A = element.GetArea(entry.Key, _mesh.Nodes);
                    // Is shell edge face
                    if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                    {
                        sectionId = elementIdSectionId[elementId];
                        if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                    " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                        thickness = sectionIdThickness[sectionId];
                        A *= thickness;
                    }
                    area += A;
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    equValue = element.GetEquivalentForcesFromFaceName(entry.Key);
                    //
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        nodeId = nodeIds[i];
                        if (nodalValues.ContainsKey(nodeId)) nodalValues[nodeId] += A * equValue[i];
                        else nodalValues.Add(nodeId, A * equValue[i]);
                    }
                }
            }
        }
        public CLoad[] GetNodalCLoadsFromImportedSurfaceTraction(ImportedSTLoad load)
        {
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            //
            int sectionId = 0;
            double thickness;
            string surfaceName = load.SurfaceName;
            double[] sectionIdThickness = new double[assignedSections.Count];
            foreach (var section in assignedSections)
            {
                thickness = section.Thickness.Value;
                sectionIdThickness[sectionId++] = thickness;
            }
            // Surface
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return null;
            //
            int nodeId;
            int[] nodeIds;
            double A;
            double[] forcePerArea;
            double[] force;
            double[] nodalForce;
            double[] faceNormal;
            double[][] forcePerAreaByValueId;
            double[][] nodalForceMagnitudes;
            FeElement element;
            Dictionary<int, double[]> nodeIdForcePerArea = new Dictionary<int, double[]>();
            Dictionary<int, double[]> nodeIdForce = new Dictionary<int, double[]>();
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    // Node ids
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    //
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out double[] faceCenter, out faceNormal,
                                                        out bool shellElement);
                    //
                    A = element.GetArea(entry.Key, _mesh.Nodes);
                    // Account for 2D area when an edge is selected
                    if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                    {
                        sectionId = elementIdSectionId[elementId];
                        if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                    " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                        thickness = sectionIdThickness[sectionId];
                        A *= thickness;
                    }
                    // Force per area
                    forcePerAreaByValueId = new double[load.Interpolator.NumOfValues][];
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        nodeId = nodeIds[i];
                        if (!nodeIdForcePerArea.TryGetValue(nodeId, out forcePerArea))
                        {
                            forcePerArea = load.GetForcePerAreaForPoint(_mesh.Nodes[nodeId].Coor);
                            nodeIdForcePerArea.Add(nodeId, forcePerArea);
                        }
                        for (int j = 0; j < forcePerAreaByValueId.Length; j++)
                        {
                            if (forcePerAreaByValueId[j] == null) forcePerAreaByValueId[j] = new double[nodeIds.Length];
                            forcePerAreaByValueId[j][i] = forcePerArea[j];
                        }
                    }
                    // Force magnitudes without area
                    nodalForceMagnitudes = new double[load.Interpolator.NumOfValues][];
                    for (int i = 0; i < nodalForceMagnitudes.Length; i++)
                    {
                        nodalForceMagnitudes[i] = element.GetEquivalentForcesFromFaceName(entry.Key, forcePerAreaByValueId[i]);
                    }
                    // Force vectors
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        force = new double[] { A * nodalForceMagnitudes[0][i],
                                               A * nodalForceMagnitudes[1][i],
                                               A * nodalForceMagnitudes[2][i] };
                        //
                        if (!nodeIdForce.TryGetValue(nodeIds[i], out nodalForce))
                        {
                            nodalForce = new double[3];
                            nodeIdForce.Add(nodeIds[i], nodalForce);
                        }
                        nodalForce[0] += force[0];
                        nodalForce[1] += force[1];
                        nodalForce[2] += force[2];
                    }
                    
                }
            }
            // Concentrated loads
            CLoad cLoad;
            List<CLoad> loads = new List<CLoad>();
            double phaseDeg = load.PhaseDeg.Value;
            foreach (var entry in nodeIdForce)
            {
                if (entry.Value[0] != 0 || entry.Value[1] != 0 || entry.Value[2] != 0)
                {
                    cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                      entry.Value[0],
                                      entry.Value[1],
                                      entry.Value[2],
                                      load.TwoD, load.Complex, phaseDeg, true);
                    //
                    cLoad.AmplitudeName = load.AmplitudeName;
                    //
                    loads.Add(cLoad);
                }
            }
            //
            return loads.ToArray();
        }
        public CLoad[] GetNodalCLoadsFromVariablePressureLoad(VariablePressure load)
        {
            List<Section> assignedSections;
            Dictionary<int, int> elementIdSectionId;
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId, out assignedSections);
            //
            int sectionId = 0;
            double thickness;
            string surfaceName = load.SurfaceName;
            double[] sectionIdThickness = new double[assignedSections.Count];
            foreach (var section in assignedSections)
            {
                thickness = section.Thickness.Value;
                sectionIdThickness[sectionId++] = thickness;
            }
            // Surface
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return null;
            //
            int nodeId = -1;
            int[] nodeIds;
            double A;
            double sign;
            double pressure;
            double[] force;
            double[] nodalForce;
            double[] faceNormal;
            double[] nodalPressures;
            double[] nodalForceMagnitudes;
            FeElement element;
            Dictionary<int, double> nodeIdPressure = new Dictionary<int, double>();
            Dictionary<int, double[]> nodeIdForce = new Dictionary<int, double[]>();
            //
            FeFaceName faceName;
            FeElement expandedElement;
            int[] expandedNodeIds;
            Dictionary<int, FeNode> expandedNodes;
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    // Node ids
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    //
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out double[] faceCenter, out faceNormal,
                                                        out bool shellElement);
                    //
                    if (Properties.ModelSpace == ModelSpaceEnum.Axisymmetric)
                    {
                        GetExpandedAxisymmetricElementFromNodeIds(nodeIds, out expandedElement, out expandedNodes);
                        expandedNodeIds = expandedElement.NodeIds;
                        faceName = FeFaceName.S1;
                        //
                        A = expandedElement.GetArea(faceName, expandedNodes) * 180 / 0.01;
                        // Pressure
                        nodalPressures = new double[expandedNodeIds.Length];
                        for (int i = 0; i < nodalPressures.Length; i++)
                        {
                            nodeId = expandedNodeIds[i];
                            pressure = load.GetPressureForPoint(this, expandedNodes[nodeId].Coor);
                            nodalPressures[i] = pressure;
                        }
                        // Force magnitudes without area
                        nodalForceMagnitudes = expandedElement.GetEquivalentForcesFromFaceName(faceName, nodalPressures);
                        // Invert face normal in case of S1 or S2 shell face
                        sign = 1;
                        // Force vectors
                        for (int i = 0; i < expandedNodeIds.Length; i++)
                        {
                            force = new double[] {
                                sign * A * nodalForceMagnitudes[i] * faceNormal[0],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[1],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[2]};
                            //
                            if (expandedElement.Id == 31)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                            }
                            else if (expandedElement.Id == 32)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[0];
                            }
                            else if (expandedElement.Id == 41)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[0];
                            }
                            else if (expandedElement.Id == 61)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 5) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 6) nodeId = nodeIds[2];
                            }
                            else if (expandedElement.Id == 62)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 5) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 6) nodeId = nodeIds[2];
                            }
                            else if (expandedElement.Id == 81)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 5) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 6) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 7) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 8) nodeId = nodeIds[0];
                            }
                            //
                            if (!nodeIdForce.TryGetValue(nodeId, out nodalForce))
                            {
                                nodalForce = new double[3];
                                nodeIdForce.Add(nodeId, nodalForce);
                            }
                            nodalForce[0] += force[0];
                            nodalForce[1] += force[1];
                            nodalForce[2] += force[2];
                        }
                    }
                    else
                    {
                        A = element.GetArea(entry.Key, _mesh.Nodes);
                        // Account for 2D area when an edge is selected
                        if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                        {
                            sectionId = elementIdSectionId[elementId];
                            if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                        " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                            thickness = sectionIdThickness[sectionId];
                            A *= thickness;
                        }
                        // Pressure
                        nodalPressures = new double[nodeIds.Length];
                        for (int i = 0; i < nodalPressures.Length; i++)
                        {
                            nodeId = nodeIds[i];
                            if (!nodeIdPressure.TryGetValue(nodeId, out pressure))
                            {
                                pressure = load.GetPressureForPoint(this, _mesh.Nodes[nodeId].Coor);
                                nodeIdPressure.Add(nodeId, pressure);
                            }
                            nodalPressures[i] = pressure;
                        }
                        // Force magnitudes without area
                        nodalForceMagnitudes = element.GetEquivalentForcesFromFaceName(entry.Key, nodalPressures);
                        // Invert face normal in case of S1 or S2 shell face
                        if (shellElement && (entry.Key == FeFaceName.S1 || entry.Key == FeFaceName.S2)) sign = -1;
                        else sign = 1;
                        // Force vectors
                        for (int i = 0; i < nodeIds.Length; i++)
                        {
                            force = new double[] {
                                sign * A * nodalForceMagnitudes[i] * faceNormal[0],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[1],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[2]};
                            //
                            if (!nodeIdForce.TryGetValue(nodeIds[i], out nodalForce))
                            {
                                nodalForce = new double[3];
                                nodeIdForce.Add(nodeIds[i], nodalForce);
                            }
                            nodalForce[0] += force[0];
                            nodalForce[1] += force[1];
                            nodalForce[2] += force[2];
                        }
                    }
                }
            }
            // Concentrated loads
            CLoad cLoad;
            List<CLoad> loads = new List<CLoad>();
            double phaseDeg = load.PhaseDeg.Value;
            foreach (var entry in nodeIdForce)
            {
                if (entry.Value[0] != 0 || entry.Value[1] != 0 || entry.Value[2] != 0)
                {
                    cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                      entry.Value[0],
                                      entry.Value[1],
                                      entry.Value[2],
                                      load.TwoD, load.Complex, phaseDeg, true);
                    //
                    cLoad.AmplitudeName = load.AmplitudeName;
                    //
                    loads.Add(cLoad);
                }
            }
            //
            return loads.ToArray();
        }
        public void GetExpandedAxisymmetricElementFromNodeIds(int[] nodeIds, out FeElement expandedElement,
                                                             out Dictionary<int, FeNode> expandedNodes)
        {
            expandedElement = null;
            expandedNodes = new Dictionary<int, FeNode>();
            // Node coor
            double[][] coor = new double[nodeIds.Length][];
            for (int i = 0; i < nodeIds.Length; i++) coor[i] = _mesh.Nodes[nodeIds[i]].Coor;
            //
            double cos = Math.Cos(Math.PI / 180 * 0.01);
            double sin = Math.Sin(Math.PI / 180 * 0.01);
            //
            if (nodeIds.Length == 2)
            {
                // Triangle
                if (coor[0][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0]));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    expandedElement = new LinearTriangleElement(31, new int[] { 1, 2, 3 });
                }
                // Triangle
                else if (coor[1][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[1]));
                    expandedNodes.Add(2, new FeNode(2, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    expandedElement = new LinearTriangleElement(32, new int[] { 1, 2, 3 });
                }
                // Quad
                else
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    expandedNodes.Add(4, new FeNode(4, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    expandedElement = new LinearQuadrilateralElement(41, new int[] { 1, 2, 3, 4 });
                }
            }
            else if (nodeIds.Length == 3)
            {
                // Triangle
                if (coor[0][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0]));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    //
                    expandedNodes.Add(4, new FeNode(4, coor[2][0] * cos, coor[2][1], coor[2][0] * sin));
                    expandedNodes.Add(5, new FeNode(5, coor[1]));
                    expandedNodes.Add(6, new FeNode(6, coor[2][0] * cos, coor[2][1], -coor[2][0] * sin));
                    //
                    expandedElement = new ParabolicTriangleElement(61, new int[] { 1, 2, 3, 4, 5, 6 });
                }
                // Triangle
                else if (coor[1][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[1]));
                    expandedNodes.Add(2, new FeNode(2, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    //
                    expandedNodes.Add(4, new FeNode(4, coor[2][0] * cos, coor[2][1], coor[2][0] * sin));
                    expandedNodes.Add(5, new FeNode(5, coor[0]));
                    expandedNodes.Add(6, new FeNode(6, coor[2][0] * cos, coor[2][1], -coor[2][0] * sin));
                    //
                    expandedElement = new ParabolicTriangleElement(62, new int[] { 1, 2, 3, 4, 5, 6 });
                }
                // Quad
                else
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    expandedNodes.Add(4, new FeNode(4, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    //
                    expandedNodes.Add(5, new FeNode(5, coor[2][0] * cos, coor[2][1], coor[2][0] * sin));
                    expandedNodes.Add(6, new FeNode(6, coor[1]));
                    expandedNodes.Add(7, new FeNode(7, coor[2][0] * cos, coor[2][1], -coor[2][0] * sin));
                    expandedNodes.Add(8, new FeNode(8, coor[0]));
                    //
                    expandedElement = new ParabolicQuadrilateralElement(81, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                }
            }
            else throw new NotSupportedException();
        }
        public DLoad[] GetElementDLoadsFromVariablePressureLoad(VariablePressure load)
        {
            // Surface
            FeSurface surface = _mesh.Surfaces[load.SurfaceName];
            if (surface.ElementFaces == null) return null;
            //
            double[][] faceCenters;
            ConcurrentBag<DLoad> loads = new ConcurrentBag<DLoad>();
            //
            foreach (var entry in surface.ElementFaces) // this are faces S1, S2, ...
            {
                // Parallel
                faceCenters = new double[_mesh.ElementSets[entry.Value].Labels.Length][];
                Parallel.For(0, _mesh.ElementSets[entry.Value].Labels.Length, i =>
                //for (int i = 0; i < _mesh.ElementSets[entry.Value].Labels.Length; i++)
                {
                    int elementId = _mesh.ElementSets[entry.Value].Labels[i];
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out faceCenters[i], out _, out _);
                }
                );
                // Pressure
                double[] pressure = load.GetPressuresForPoints(this, faceCenters);
                // Parallel
                Parallel.For(0, _mesh.ElementSets[entry.Value].Labels.Length, i =>
                //for (int i = 0; i < _mesh.ElementSets[entry.Value].Labels.Length; i++)
                {
                    int elementId = _mesh.ElementSets[entry.Value].Labels[i];
                    // Pressure loads
                    if (pressure[i] != 0)
                    {
                        DLoad dLoad = new DLoad("ElementID_" + elementId.ToString(), elementId, entry.Key, pressure[i], load.TwoD,
                                                load.Complex, load.PhaseDeg.Value, true);
                        dLoad.AmplitudeName = load.AmplitudeName;
                        loads.Add(dLoad);
                    }
                }
                );
            }
            //
            return loads.ToArray();
        }
        public InitialTranslationalVelocity[] GetTranslationalVelocities(InitialAngularVelocity initialAngularVelocity,
                                                       Dictionary<string, Calculix.CalReferencePoint> referencePoints)
        {
            Dictionary<int, double[]> nodeIdCoor = new Dictionary<int, double[]>();
            if (initialAngularVelocity.RegionType == RegionTypeEnum.ReferencePointName)
            {
                FeReferencePoint rp = _mesh.ReferencePoints[initialAngularVelocity.RegionName];
                nodeIdCoor[referencePoints[rp.Name].ReferenceNodeId] = rp.Coor();
            }
            else
            {
                int[] nodeIds;
                double[][] coor;
                _mesh.GetRegionNodeIdsAndCoordinates(initialAngularVelocity.RegionName,
                                                     initialAngularVelocity.RegionType, out nodeIds, out coor);
                for (int i = 0; i < nodeIds.Length; i++) nodeIdCoor[nodeIds[i]] = coor[i];
            }
            //
            Dictionary<int, double[]> nodeIdVelocity;
            initialAngularVelocity.GetTranslationalVelocities(nodeIdCoor, out nodeIdVelocity);
            //
            int count = 0;
            int nodeId;
            InitialTranslationalVelocity[] translationalVelocities = new InitialTranslationalVelocity[nodeIdVelocity.Count];
            foreach (var entry in nodeIdVelocity)
            {
                nodeId = entry.Key;
                // Is the node is on the axis
                if (nodeId < 0)
                {
                    // If the node is a translational node of the reference point, get a rotational node instead
                    foreach (var rpEntry in referencePoints)
                    {
                        if (rpEntry.Value.ReferenceNodeId == -nodeId)
                        {
                            nodeId = rpEntry.Value.HasRotationNode
                                   ? rpEntry.Value.RotationNodeId.Value
                                   : rpEntry.Value.ReferenceNodeId;
                            break;
                        }
                    }
                }
                //
                translationalVelocities[count++] =
                    new InitialTranslationalVelocity("_iniTransVel_" + nodeId.ToString(), nodeId,
                                                     entry.Value[0], entry.Value[1], entry.Value[2],
                                                     initialAngularVelocity.TwoD, true);
            }
            //
            return translationalVelocities;
        }
        // Defined fields                                                                           
        public DefinedTemperature[] GetNodalTemperaturesFromVariableDefinedTemperature(DefinedTemperature definedTemperature)
        {
            if (definedTemperature.Type == DefinedTemperatureTypeEnum.FromFile) return null;
            if (definedTemperature.DistributionName == Distribution.DefaultDistributionName) return null;
            int[] nodeIds;
            double[][] coor;
            _mesh.GetRegionNodeIdsAndCoordinates(definedTemperature.RegionName, definedTemperature.RegionType,
                                                 out nodeIds, out coor);
            // Temperature
            double[] temperatures;
            definedTemperature.GetTemperaturesAndDistancesForPoints(this, coor, out _, out temperatures);
            //
            DefinedTemperature[] definedTemperatures = new DefinedTemperature[nodeIds.Length];
            Parallel.For(0, nodeIds.Length, i =>
            {
                int nodeId = nodeIds[i];
                // Temperature definitions
                DefinedTemperature dt = new DefinedTemperature("NodeID_" + nodeId.ToString(), nodeId, temperatures[i], true);
                dt.AmplitudeName = definedTemperature.AmplitudeName;
                definedTemperatures[i] = dt;
            });
            //
            return definedTemperatures;
        }
        // Parameters                                                                               
        public void UpdateNCalcParameters()
        {
            List<KeyValuePair<string, string>> nameEquations = new List<KeyValuePair<string, string>>();
            foreach (var entry in _parameters)
                nameEquations.Add(new KeyValuePair<string, string>(entry.Key, entry.Value.Equation.String));
            //
            Dictionary<string, string> errors = EquationParameterResolver.Evaluate(nameEquations, GetPropertyParameters(),
                                                                                   out _);
            if (errors.Count > 0)
            {
                MessageBoxes.ShowError("The following parameters could not be evaluated: " +
                                       string.Join(", ", errors.Keys) + "." + Environment.NewLine +
                                       "Check the parameter equations.");
            }
        }
        
        public Dictionary<string, object> GetPropertyParameters()
        {
            double volume;
            double area;
            string prefix = "Geometry";
            Dictionary<string, object> propParameters = new Dictionary<string, object>();
            // Geometry
            string volumePrefix = prefix + ".Volume";
            propParameters.Add(volumePrefix, 0);
            string areaPrefix = prefix + ".Area";
            propParameters.Add(areaPrefix, 0);
            // 
            volume = 0;
            area = 0;
            HashSet<string> compoundSubpartNames = new HashSet<string>();
            foreach (var partEntry in _geometry.Parts)
            {
                if (partEntry.Value is CompoundGeometryPart cgp) compoundSubpartNames.UnionWith(cgp.SubPartNames);
            }
            foreach (var partEntry in _geometry.Parts)
            {
                if (partEntry.Value is CompoundGeometryPart cgp)
                {
                    AddCompoundPartProperties(prefix, cgp, ref propParameters);
                    volume += (double)propParameters[prefix + "." + partEntry.Value.Name + ".Volume"];
                    area += (double)propParameters[prefix + "." + partEntry.Value.Name + ".Area"];
                }
                else if (!compoundSubpartNames.Contains(partEntry.Value.Name))
                {
                    AddNonCompoundPartProperties(prefix, partEntry.Value, ref propParameters);
                    volume += (double)propParameters[prefix + "." + partEntry.Value.Name + ".Volume"];
                    area += (double)propParameters[prefix + "." + partEntry.Value.Name + ".Area"];
                }
            }
            propParameters[volumePrefix] = volume;
            propParameters[areaPrefix] = area;
            // Mesh
            prefix = "Mesh";
            volumePrefix = prefix + ".Volume";
            propParameters.Add(volumePrefix, 0);
            areaPrefix = prefix + ".Area";
            propParameters.Add(areaPrefix, 0);
            // 
            volume = 0;
            area = 0;
            foreach (var partEntry in _mesh.Parts)
            {
                AddNonCompoundPartProperties(prefix, partEntry.Value, ref propParameters);
                volume += (double)propParameters[prefix + "." + partEntry.Value.Name + ".Volume"];
                area += (double)propParameters[prefix + "." + partEntry.Value.Name + ".Area"];
            }
            propParameters[volumePrefix] = volume;
            propParameters[areaPrefix] = area;
            //
            return propParameters;
        }
        public string[] GetAllParameterNames()
        {
            List<string> parameterNames = new List<string>();
            parameterNames.AddRange(_parameters.Keys);
            parameterNames.AddRange(GetPropertyParameters().Keys);
            return parameterNames.ToArray();
        }
        private void AddCompoundPartProperties(string prefix, CompoundGeometryPart part,
                                               ref Dictionary<string, object> propParameters)
        {
            // Geometry
            prefix += "." + part.Name;
            string volumePrefix = prefix + ".Volume";
            propParameters.Add(volumePrefix, 0);
            string areaPrefix = prefix + ".Area";
            propParameters.Add(areaPrefix, 0);
            double volume = 0;
            double area = 0;
            foreach (var subPartName in part.SubPartNames)
            {
                AddNonCompoundPartProperties(prefix, _geometry.Parts[subPartName], ref propParameters);
                volume += (double)propParameters[prefix + "." + subPartName + ".Volume"];
                area += (double)propParameters[prefix + "." + subPartName + ".Area"];
            }
            propParameters[volumePrefix] = volume;
            propParameters[areaPrefix] = area;
        }
        private void AddNonCompoundPartProperties(string prefix, BasePart part, ref Dictionary<string, object> propParameters)
        {
            bool solid = part.PartType == PartType.Solid || part.PartType == PartType.SolidAsShell;
            if (part is CompoundGeometryPart)
                throw new NotSupportedException();
            else
            {
                VisualizationData vis = part.Visualization;
                prefix += "." + part.Name;
                // Volume
                double volume = 0;
                if (solid) volume = part.MassProperties.Volume;
                propParameters.Add(prefix + ".Volume", volume);
                // Area
                string areaPrefix = prefix + ".Area";
                propParameters.Add(areaPrefix, -1);
                // Surfaces
                double area = 0;
                double perimeter;
                string surfacePrefix;
                for (int i = 0; i < vis.FaceCount; i++)
                {
                    surfacePrefix = prefix + ".Surface-" + (i + 1);
                    propParameters.Add(surfacePrefix + ".Area", vis.FaceAreas[i]);
                    area += vis.FaceAreas[i];
                    perimeter = 0;
                    for (int j = 0; j < vis.FaceEdgeIds[i].Length; j++) perimeter += vis.EdgeLengths[vis.FaceEdgeIds[i][j]];
                    //propParameters.Add(surfacePrefix + ".Perimeter", perimeter);
                }
                //
                propParameters[areaPrefix] = area;
                // Edges
                string edgePrefix;
                for (int i = 0; i < vis.EdgeCount; i++)
                {
                    edgePrefix = prefix + ".Edge-" + (i + 1);
                    //propParameters.Add(edgePrefix + ".Length", vis.EdgeLengths[i]);
                }
            }
        }



        // 3D - 2D                                                                                  
        public void UpdateMeshPartsElementTypes(bool allowMixedModel)
        {
            if (_properties != null)
            {
                Dictionary<Type, HashSet<Enum>> elementTypeEnums = _properties.ModelSpace.GetAvailableElementTypes(allowMixedModel);
                if (_mesh != null) _mesh.UpdatePartsElementTypes(elementTypeEnums);
            }
        }
        // Boundary displacement method                                                             
        public FeModel PrepareBdmModel(Dictionary<int, double[]> deformations)
        {
            // Mesh
            FeModel bdmModel = new FeModel("BDMmodel", _unitSystem, false);
            bdmModel.Properties = _properties;
            bdmModel.SetMesh(_mesh.DeepCopy());
            // Materials
            Material materialElastic = new Material("Elastic");
            materialElastic.AddProperty(new Elastic(new double[][] { new double[] { 1000, 0, 0 } }));
            bdmModel._materials.Add(materialElastic.Name, materialElastic);
            // Sections
            Section section;
            foreach (var entry in _sections)
            {
                section = entry.Value.DeepClone();
                section.MaterialName = materialElastic.Name;
                bdmModel.Sections.Add(section.Name, section);
            }
            // Constraints
            HashSet<int> constrainedNodeIds = new HashSet<int>();
            Constraint constraint;
            foreach (var entry in _constraints)
            {
                constraint = entry.Value.DeepClone();
                bdmModel.Constraints.Add(constraint.Name, constraint);
                //
                if (constraint is RigidBody rb)
                {
                    if (rb.SlaveRegionType == RegionTypeEnum.Selection)
                        constrainedNodeIds.UnionWith(rb.SlaveCreationIds);
                    else if (rb.SlaveRegionType == RegionTypeEnum.NodeSetName)
                        constrainedNodeIds.UnionWith(_mesh.NodeSets[rb.SlaveRegionName].Labels);
                    else if (rb.SlaveRegionType == RegionTypeEnum.SurfaceName)
                        constrainedNodeIds.UnionWith(_mesh.NodeSets[_mesh.Surfaces[rb.SlaveRegionName].NodeSetName].Labels);
                }
            }
            // Distributions
            Distribution distribution;
            foreach (var entry in _distributions)
            {
                distribution = entry.Value.DeepClone();
                bdmModel.Distributions.Add(distribution.Name, distribution);
            }
            // Amplitudes
            Amplitude amplitude;
            foreach (var entry in _amplitudes)
            {
                amplitude = entry.Value.DeepClone();
                bdmModel.Amplitudes.Add(amplitude.Name, amplitude);
            }
            // Steps
            StaticStep staticStep = new StaticStep("Step-1");
            bdmModel._stepCollection.AddStep(staticStep, false);
            BoundaryDisplacementStep boundaryDisplacementStep = _stepCollection.GetBoundaryDisplacementStep();
            if (boundaryDisplacementStep == null)
                throw new CaeException("The boundary displacement step is missing.");
            // Add existing boundary conditions
            bool twoD = false;
            BoundaryCondition bc;
            foreach (var entry in boundaryDisplacementStep.BoundaryConditions)
            {
                bc = entry.Value.DeepClone();
                staticStep.AddBoundaryCondition(bc);
                twoD = bc.TwoD;
            }
            // Create BDM boundary conditions
            string name;
            double[] xyz;
            FeNodeSet nodeSet;
            DisplacementRotation displacementRotation;
            double zeroLength;
            //
            if (deformations != null)
            {
                foreach (var entry in deformations)
                {
                    if (!constrainedNodeIds.Contains(entry.Key))
                    {
                        // Node set
                        name = bdmModel.Mesh.NodeSets.GetNextNumberedKey("BDM", "_" + entry.Key);
                        nodeSet = new FeNodeSet(name, new int[] { entry.Key });
                        bdmModel.Mesh.NodeSets.Add(nodeSet.Name, nodeSet);
                        // Boundary condition
                        xyz = entry.Value;
                        name = staticStep.BoundaryConditions.GetNextNumberedKey("BDM-" + entry.Key);
                        displacementRotation = new DisplacementRotation(name, nodeSet.Name, RegionTypeEnum.NodeSetName, twoD,
                                                                        false, 0);
                        // A component below a thousandth of the vector is noise of the surface normal and leaves
                        // the node free in that direction, whatever the precision of the results file
                        zeroLength = 1E-3 * Math.Sqrt(xyz[0] * xyz[0] + xyz[1] * xyz[1] + xyz[2] * xyz[2]);
                        if (Math.Abs(xyz[0]) > zeroLength) displacementRotation.U1.SetEquationFromValue(xyz[0]);
                        if (Math.Abs(xyz[1]) > zeroLength) displacementRotation.U2.SetEquationFromValue(xyz[1]);
                        if (Math.Abs(xyz[2]) > zeroLength) displacementRotation.U3.SetEquationFromValue(xyz[2]);
                        staticStep.AddBoundaryCondition(displacementRotation);
                    }
                }
            }
            //
            return bdmModel;
        }
        // Copy
        public FeModel DeepCopy()
        {
            try
            {
                FeModel model = this.DeepClone();
                //
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        WriteToBinaryWriter(this, bw);
                        //
                        bw.Flush();
                        bw.BaseStream.Seek(0, SeekOrigin.Begin);
                        using (BinaryReader br = new BinaryReader(bw.BaseStream))
                        {
                            ReadFromBinaryReader(model, br, -1);
                        }
                    }
                }
                //
                return model;
            }
            catch
            {
                return null;
            }
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_name", _name, typeof(string));
            info.AddValue("_geometry", _geometry, typeof(FeMesh));
            info.AddValue("_mesh", _mesh, typeof(FeMesh));
            info.AddValue("_userViews", _userViews, typeof(OrderedDictionary<string, UserViewParameters>));
            info.AddValue("_parameters", _parameters, typeof(EquationParameterCollection));
            info.AddValue("_materials", _materials, typeof(OrderedDictionary<string, Material>));
            info.AddValue("_sections", _sections, typeof(OrderedDictionary<string, Section>));
            info.AddValue("_constraints", _constraints, typeof(OrderedDictionary<string, Constraint>));
            info.AddValue("_surfaceInteractions", _surfaceInteractions, typeof(OrderedDictionary<string, SurfaceInteraction>));
            info.AddValue("_contactPairs", _contactPairs, typeof(OrderedDictionary<string, ContactPair>));
            info.AddValue("_distributions", _distributions, typeof(OrderedDictionary<string, Distribution>));
            info.AddValue("_amplitudes", _amplitudes, typeof(OrderedDictionary<string, Amplitude>));
            info.AddValue("_initialConditions", _initialConditions, typeof(OrderedDictionary<string, InitialCondition>));
            info.AddValue("_stepCollection", _stepCollection, typeof(StepCollection));
            info.AddValue("_calculixUserKeywords", _calculixUserKeywords,
                          typeof(OrderedDictionary<int[], Calculix.CalculixUserKeyword>));
            info.AddValue("_calculixKeywordOverrides", _calculixKeywordOverrides,
                          typeof(List<Calculix.CalculixKeywordOverride>));
            info.AddValue("_properties", _properties, typeof(ModelProperties));
            info.AddValue("_unitSystem", _unitSystem, typeof(UnitSystem));
            info.AddValue("_hashName", _hashName, typeof(string));
        }

    }
}
