// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard (Composites Fiber Modeler)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewCompositeLayup
    {
        // Variables                                                                                                                
        private CompositeLayup _compositeLayup;
        private List<PlyDataPoint> _plies;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Id(1, 1)]
        [DescriptionAttribute("Name of the composite layup.")]
        public string Name { get { return _compositeLayup.Name; } set { _compositeLayup.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Number of plies")]
        [Id(2, 1)]
        [DescriptionAttribute("The number of plies of the layup, of the mirrored ones too for a symmetric layup.")]
        public int NumberOfPlies { get { return _compositeLayup.Symmetric ? 2 * _plies.Count : _plies.Count; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Symmetric")]
        [Id(3, 1)]
        [DescriptionAttribute("Complete the layup with its mirror image: the plies are followed by the same plies in " +
                              "reverse order.")]
        public bool Symmetric { get { return _compositeLayup.Symmetric; } set { _compositeLayup.Symmetric = value; } }
        //
        [CategoryAttribute("Offset")]
        [OrderedDisplayName(0, 10, "Reference surface")]
        [Id(1, 2)]
        [DescriptionAttribute("Select the surface of the layup that lies on the shell elements.")]
        public CompositeLayupOffsetEnum OffsetType
        {
            get { return _compositeLayup.OffsetType; }
            set { _compositeLayup.OffsetType = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Offset")]
        [OrderedDisplayName(1, 10, "Offset")]
        [Id(2, 2)]
        [DescriptionAttribute("Set the offset of the shell mid-surface in regard to the shell elements. " +
                              "The unit is the total thickness.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString OffsetRatio
        {
            get { return _compositeLayup.OffsetRatio.Equation; }
            set { _compositeLayup.OffsetRatio.Equation = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(0, 10, "Definition")]
        [Id(1, 3)]
        [DescriptionAttribute("Select how the reference fiber direction of the plies is defined.")]
        public CompositeLayupOrientationEnum OrientationType
        {
            get { return _compositeLayup.OrientationType; }
            set { _compositeLayup.OrientationType = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(1, 10, "Coordinate system")]
        [Id(2, 3)]
        [DescriptionAttribute("Select the coordinate system whose axis defines the reference fiber direction.")]
        public string CoordinateSystemName
        {
            get { return _compositeLayup.CoordinateSystemName; }
            set { _compositeLayup.CoordinateSystemName = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(2, 10, "Reference axis")]
        [Id(3, 3)]
        [DescriptionAttribute("Select the axis of the coordinate system that is projected onto each shell element to " +
                              "become the reference fiber direction.")]
        public MaterialOrientationAxisEnum ReferenceAxis
        {
            get { return _compositeLayup.ReferenceAxis; }
            set { _compositeLayup.ReferenceAxis = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(3, 10, "Rotation angle")]
        [Id(4, 3)]
        [DescriptionAttribute("The angle that turns the reference fiber direction about the shell normal.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        public EquationString RotationAngle
        {
            get { return _compositeLayup.RotationAngle.Equation; }
            set { _compositeLayup.RotationAngle.Equation = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(4, 10, "Stacking points")]
        [Id(5, 3)]
        [DescriptionAttribute("The number of stacking points with fiber directions read from the discrete " +
                              "orientations file. Use Import Discrete Orientations to read a file.")]
        public int NumberOfStackingPoints
        {
            get
            {
                var data = _compositeLayup.DiscreteOrientationsData;
                return data == null || data.Points == null ? 0 : data.Points.Count;
            }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(5, 10, "Draped ply")]
        [Id(6, 3)]
        [DescriptionAttribute("The number of the ply whose draping simulation defines the fiber directions.")]
        public int DrapingPlyNumber
        {
            get { return _compositeLayup.DrapingPlyIndex + 1; }
            set { _compositeLayup.DrapingPlyIndex = value - 1; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(6, 10, "Draping cell size")]
        [Id(7, 3)]
        [DescriptionAttribute("The size of the fabric cells of the kinematic draping simulation.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        public EquationString DrapingCellSize
        {
            get { return _compositeLayup.DrapingCellSize.Equation; }
            set { _compositeLayup.DrapingCellSize.Equation = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(7, 10, "Shear angle limit")]
        [Id(8, 3)]
        [DescriptionAttribute("Fabric cells sheared more than this angle are marked in the draping preview.")]
        [TypeConverter(typeof(StringAngleDegConverter))]
        public double DrapingLimitShearDeg
        {
            get { return _compositeLayup.DrapingLimitShearDeg; }
            set { _compositeLayup.DrapingLimitShearDeg = value; }
        }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(0, 10, "Symbol density")]
        [Id(1, 4)]
        [DescriptionAttribute("Set the density of the fiber direction symbols as a percentage of the full density.")]
        [TypeConverter(typeof(StringIntegerPercentConverter))]
        public int SymbolDensity
        {
            get { return _compositeLayup.SymbolDensity; }
            set { _compositeLayup.SymbolDensity = value; }
        }
        //
        [Browsable(false)]
        public List<PlyDataPoint> Plies { get { return _plies; } }


        // Constructors                                                                                                             
        public ViewCompositeLayup(CompositeLayup compositeLayup)
        {
            _compositeLayup = compositeLayup;
            _plies = new List<PlyDataPoint>();
            foreach (Ply ply in _compositeLayup.Plies) _plies.Add(new PlyDataPoint(ply));
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.RenameBooleanPropertyToYesNo(nameof(Symmetric));
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public CompositeLayup GetBase()
        {
            List<Ply> plies = new List<Ply>();
            foreach (PlyDataPoint ply in _plies) plies.Add(ply.GetPly());
            _compositeLayup.Plies = plies;
            return _compositeLayup;
        }
        public void PopulateDropDownLists(string[] coordinateSystemNames)
        {
            List<string> names = new List<string>() { CoordinateSystem.DefaultCoordinateSystemName };
            names.AddRange(coordinateSystemNames);
            _dctd.PopulateOptionalProperty(nameof(CoordinateSystemName), names.ToArray(), false, 2);
        }
        public void UpdateVisibility()
        {
            if (_dctd == null) return;
            bool cs = _compositeLayup.OrientationType == CompositeLayupOrientationEnum.CoordinateSystem;
            bool draping = _compositeLayup.OrientationType == CompositeLayupOrientationEnum.Draping;
            bool discrete = _compositeLayup.OrientationType == CompositeLayupOrientationEnum.DiscreteOrientations;
            _dctd.GetProperty(nameof(CoordinateSystemName)).SetIsBrowsable(cs || draping);
            _dctd.GetProperty(nameof(ReferenceAxis)).SetIsBrowsable(cs || draping);
            _dctd.GetProperty(nameof(RotationAngle)).SetIsBrowsable(cs || draping);
            _dctd.GetProperty(nameof(NumberOfStackingPoints)).SetIsBrowsable(discrete);
            _dctd.GetProperty(nameof(DrapingPlyNumber)).SetIsBrowsable(draping);
            _dctd.GetProperty(nameof(DrapingCellSize)).SetIsBrowsable(draping);
            _dctd.GetProperty(nameof(DrapingLimitShearDeg)).SetIsBrowsable(draping);
            _dctd.GetProperty(nameof(OffsetRatio)).SetIsBrowsable(_compositeLayup.OffsetType == CompositeLayupOffsetEnum.Ratio);
        }
    }
}
