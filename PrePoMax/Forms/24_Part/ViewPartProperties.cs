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
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewPartProperties
    {
        // Variables                                                                                                                
        private PartProperties _partProperties;
        private ElementTypeCount[] _elementTypeCounts;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Description("Name of the part.")]
        [Id(1, 1)]
        public string Name { get { return _partProperties.Name; } set { _partProperties.Name = value; } }
        //
        [Category("Data")]
        [OrderedDisplayName(1, 10, "Part type")]
        [Description("Part type.")]
        [Id(2, 1)]
        public PartType PartType { get { return _partProperties.PartType; } }
        //
        [Category("Data")]
        [OrderedDisplayName(2, 10, "Geometry")]
        [Description("Geometry type.")]
        [Id(3, 1)]
        public string GeometryType { get { return _partProperties.IsCADPart ? "CAD" : "STL"; } }
        //
        [Category("Mesh")]
        [OrderedDisplayName(0, 16, "Number of elements")]
        [Description("Number of elements.")]
        [Id(1, 2)]
        public int NumberOfElements { get { return _partProperties.NumberOfElements; } }
        //
        [Category("Mesh")]
        [OrderedDisplayName(0, 16, "Number of facets")]
        [Description("Number of facets.")]
        [Id(1, 2)]
        public int NumberOfFacets { get { return _partProperties.NumberOfElements; } }
        //
        [Category("Mesh")]
        [OrderedDisplayName(1, 16, "Linear beams")]
        [Description("Number of linear beam elements.")]
        [Id(2, 2)]
        public int NumberOfLinearBeamElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearBeamElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(2, 16, "Parabolic beams")]
        [Description("Number of parabolic beam elements.")]
        [Id(3, 2)]
        public int NumberOfParabolicBeamElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicBeamElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(3, 16, "Linear triangles")]
        [Description("Number of linear triangular elements.")]
        [Id(4, 2)]
        public int NumberOfLinearTriaElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearTriangleElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(4, 16, "Parabolic triangles")]
        [Description("Number of parabolic triangular elements.")]
        [Id(5, 2)]
        public int NumberOfParabolicTriaElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicTriangleElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(5, 16, "Linear quadrilaterals")]
        [Description("Number of linear quadrilateral elements.")]
        [Id(6, 2)]
        public int NumberOfLinearQuadElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearQuadrilateralElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(6, 16, "Parabolic quadrilaterals")]
        [Description("Number of parabolic quadrilateral elements.")]
        [Id(7, 2)]
        public int NumberOfParabolicQuadElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicQuadrilateralElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(7, 16, "Linear tetrahedra")]
        [Description("Number of linear tetrahedron elements.")]
        [Id(8, 2)]
        public int NumberOfLinearTetraElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearTetraElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(8, 16, "Parabolic tetrahedra")]
        [Description("Number of parabolic tetrahedron elements.")]
        [Id(9, 2)]
        public int NumberOfParabolicTetraElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicTetraElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(9, 16, "Linear pyramids")]
        [Description("Number of linear pyramid elements.")]
        [Id(10, 2)]
        public int NumberOfLinearPyramidElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearPyramidElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(10, 16, "Parabolic pyramids")]
        [Description("Number of parabolic pyramid elements.")]
        [Id(11, 2)]
        public int NumberOfParabolicPyramidElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicPyramidElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(11, 16, "Linear wedges")]
        [Description("Number of linear wedge elements.")]
        [Id(12, 2)]
        public int NumberOfLinearWedgeElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearWedgeElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(12, 16, "Parabolic wedges")]
        [Description("Number of parabolic wedge elements.")]
        [Id(13, 2)]
        public int NumberOfParabolicWedgeElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicWedgeElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(13, 16, "Linear hexahedra")]
        [Description("Number of linear hexahedron elements.")]
        [Id(14, 2)]
        public int NumberOfLinearHexaElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(LinearHexaElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(14, 16, "Parabolic hexahedra")]
        [Description("Number of parabolic hexahedron elements.")]
        [Id(15, 2)]
        public int NumberOfParabolicHexaElements
        {
            get { return ElementTypeCount.GetCount(_elementTypeCounts, typeof(ParabolicHexaElement)); }
        }
        //
        [Category("Mesh")]
        [OrderedDisplayName(15, 16, "Number of nodes")]
        [Description("Number of nodes.")]
        [Id(16, 2)]
        public int NumberOfNodes { get { return _partProperties.NumberOfNodes; } }
        //
        [Category("Mesh")]
        [OrderedDisplayName(15, 16, "Number of vertices")]
        [Description("Number of vertices.")]
        [Id(16, 2)]
        public int NumberOfVertices { get { return _partProperties.NumberOfNodes; } }
        //
        [Category("Element type")]
        [OrderedDisplayName(0, 20, "Linear triangle")]
        [Description("Select the type of the linear triangular elements.")]
        [Id(1, 3)]
        public FeElementTypeLinearTria LinearTriaType
        {
            get { return _partProperties.LinearTriaType; }
            set { _partProperties.LinearTriaType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(1, 20, "Parabolic triangle")]
        [Description("Select the type of the parabolic triangular elements.")]
        [Id(2, 3)]
        public FeElementTypeParabolicTria ParabolicTriaType
        {
            get { return _partProperties.ParabolicTriaType; }
            set { _partProperties.ParabolicTriaType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(2, 20, "Linear quadrilateral")]
        [Description("Select the type of the linear quadrilateral elements.")]
        [Id(3, 3)]
        public FeElementTypeLinearQuad LinearQuadType
        {
            get { return _partProperties.LinearQuadType; }
            set { _partProperties.LinearQuadType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(3, 20, "Parabolic quadrilateral")]
        [Description("Select the type of the parabolic quadrilateral elements.")]
        [Id(4, 3)]
        public FeElementTypeParabolicQuad ParabolicQuadType
        {
            get { return _partProperties.ParabolicQuadType; }
            set { _partProperties.ParabolicQuadType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(4, 20, "Linear tetrahedron")]
        [Description("Select the type of the linear tetrahedron elements.")]
        [Id(5, 3)]
        public FeElementTypeLinearTetra LinearTetraType 
        {
            get { return _partProperties.LinearTetraType; }
            set { _partProperties.LinearTetraType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(5, 20, "Parabolic tetrahedron")]
        [Description("Select the type of the parabolic tetrahedron elements.")]
        [Id(6, 3)]
        public FeElementTypeParabolicTetra ParabolicTetraType
        {
            get { return _partProperties.ParabolicTetraType; }
            set { _partProperties.ParabolicTetraType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(6, 20, "Linear pyramid")]
        [Description("Select the type of the linear pyramid elements.")]
        [Id(7, 3)]
        public FeElementTypeLinearPyramid LinearPyramidType
        {
            get { return _partProperties.LinearPyramidType; }
            set { _partProperties.LinearPyramidType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(7, 20, "Parabolic pyramid")]
        [Description("Select the type of the parabolic pyramid elements.")]
        [Id(8, 3)]
        public FeElementTypeParabolicPyramid ParabolicPyramidType
        {
            get { return _partProperties.ParabolicPyramidType; }
            set { _partProperties.ParabolicPyramidType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(8, 20, "Linear wedge")]
        [Description("Select the type of the linear wedge elements.")]
        [Id(9, 3)]
        public FeElementTypeLinearWedge LinearWedgeType
        {
            get { return _partProperties.LinearWedgeType; }
            set { _partProperties.LinearWedgeType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(9, 20, "Parabolic wedge")]
        [Description("Select the type of the parabolic wedge elements.")]
        [Id(10, 3)]
        public FeElementTypeParabolicWedge ParabolicWedgeType
        {
            get { return _partProperties.ParabolicWedgeType; }
            set { _partProperties.ParabolicWedgeType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(10, 20, "Linear hexahedron")]
        [Description("Select the type of the linear hexahedron elements.")]
        [Id(11, 3)]
        public FeElementTypeLinearHexa LinearHexaType
        {
            get { return _partProperties.LinearHexaType; }
            set { _partProperties.LinearHexaType = value; }
        }
        //
        [Category("Element type")]
        [OrderedDisplayName(11, 20, "Parabolic hexahedron")]
        [Description("Select the type of the parabolic hexahedron elements.")]
        [Id(12, 3)]
        public FeElementTypeParabolicHexa ParabolicHexaType
        {
            get { return _partProperties.ParabolicHexaType; }
            set { _partProperties.ParabolicHexaType = value; }
        }
        //
        [Category("Geometry")]
        [OrderedDisplayName(0, 10, "Volume")]
        [Description("Part volume.")]
        [TypeConverter(typeof(StringVolumeUnknownConverter))]
        [Id(1, 4)]
        public double Volume
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.Volume, 6);
                else return double.NaN;
            }
        }
        //
        [Category("Geometry")]
        [OrderedDisplayName(0, 10, "Undeformed volume")]
        [Description("Part undeformed volume.")]
        [TypeConverter(typeof(StringVolumeUnknownConverter))]
        [Id(1, 4)]
        public double UndeformedVolume
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.Volume, 6);
                else return double.NaN;
            }
        }
        //
        [Category("Geometry")]
        [OrderedDisplayName(1, 10, "Area")]
        [Description("Part area.")]
        [TypeConverter(typeof(StringAreaUnknownConverter))]
        [Id(2, 4)]
        public double Area
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.Area, 6);
                else return double.NaN;
            }
        }
        //
        [Category("Geometry")]
        [OrderedDisplayName(1, 10, "Undeformed area")]
        [Description("Part undeformed area.")]
        [TypeConverter(typeof(StringAreaUnknownConverter))]
        [Id(2, 4)]
        public double UndeformedArea
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.Area, 6);
                else return double.NaN;
            }
        }
        //
        [Category("Center of mass")]
        [OrderedDisplayName(0, 10, "X")]
        [Description("X coordinate of center of mass.")]
        [TypeConverter(typeof(StringLengthUnknownConverter))]
        [Id(1, 5)]
        public double X
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.CenterOfMass[0], 6);
                else return double.NaN;
            }
        }
        //
        [Category("Center of mass")]
        [OrderedDisplayName(1, 10, "Y")]
        [Description("Y coordinate of center of mass.")]
        [TypeConverter(typeof(StringLengthUnknownConverter))]
        [Id(2, 5)]
        public double Y
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.CenterOfMass[1], 6);
                else return double.NaN;
            }
        }
        //
        [Category("Center of mass")]
        [OrderedDisplayName(2, 10, "Z")]
        [Description("Z coordinate of center of mass.")]
        [TypeConverter(typeof(StringLengthUnknownConverter))]
        [Id(2, 5)]
        public double Z
        {
            get
            {
                if (_partProperties.MassProperties.CenterOfMass != null)
                    return Math.Round(_partProperties.MassProperties.CenterOfMass[2], 6);
                else return double.NaN;
            }
        }
        [Category("Appearance")]
        [OrderedDisplayName(0, 10, "Part color")]
        [Description("Select part color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 6)]
        public System.Drawing.Color Color
        {
            get { return _partProperties.Color; }
            set
            {
                _partProperties.Color = System.Drawing.Color.FromArgb(Math.Max((byte)25, value.A), value);
            }
        }


        // Constructors                                                                                                             
        public ViewPartProperties(PartProperties properties, ElementTypeCount[] elementTypeCounts,
                                  ViewGeometryModelResults currentView,
                                  Dictionary<Type, HashSet<string>> elementTypeNamesToRemove)
        {
            _partProperties = properties;
            _elementTypeCounts = elementTypeCounts;
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            SetVisibility(currentView, elementTypeNamesToRemove);
        }


        // Methods                                                                                                                  
        public PartProperties GetBase()
        {
            return _partProperties;
        }
        private void SetVisibility(ViewGeometryModelResults currentView, Dictionary<Type, HashSet<string>> elementTypeNamesToRemove)
        {
            bool solid = _partProperties.PartType == PartType.Solid || _partProperties.PartType == PartType.SolidAsShell ||
                         _partProperties.PartType == PartType.Compound;
            //
            _dctd.GetProperty(nameof(X)).SetIsBrowsable(!double.IsNaN(X));
            _dctd.GetProperty(nameof(Y)).SetIsBrowsable(!double.IsNaN(Y));
            _dctd.GetProperty(nameof(Z)).SetIsBrowsable(!double.IsNaN(Z));
            //
            bool mesh = currentView != ViewGeometryModelResults.Geometry;
            _dctd.GetProperty(nameof(NumberOfLinearBeamElements)).SetIsBrowsable(mesh && NumberOfLinearBeamElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicBeamElements)).SetIsBrowsable(mesh && NumberOfParabolicBeamElements > 0);
            _dctd.GetProperty(nameof(NumberOfLinearTriaElements)).SetIsBrowsable(mesh && NumberOfLinearTriaElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicTriaElements)).SetIsBrowsable(mesh && NumberOfParabolicTriaElements > 0);
            _dctd.GetProperty(nameof(NumberOfLinearQuadElements)).SetIsBrowsable(mesh && NumberOfLinearQuadElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicQuadElements)).SetIsBrowsable(mesh && NumberOfParabolicQuadElements > 0);
            _dctd.GetProperty(nameof(NumberOfLinearTetraElements)).SetIsBrowsable(mesh && NumberOfLinearTetraElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicTetraElements)).SetIsBrowsable(mesh && NumberOfParabolicTetraElements > 0);
            _dctd.GetProperty(nameof(NumberOfLinearPyramidElements)).SetIsBrowsable(mesh && NumberOfLinearPyramidElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicPyramidElements)).SetIsBrowsable(mesh && NumberOfParabolicPyramidElements > 0);
            _dctd.GetProperty(nameof(NumberOfLinearWedgeElements)).SetIsBrowsable(mesh && NumberOfLinearWedgeElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicWedgeElements)).SetIsBrowsable(mesh && NumberOfParabolicWedgeElements > 0);
            _dctd.GetProperty(nameof(NumberOfLinearHexaElements)).SetIsBrowsable(mesh && NumberOfLinearHexaElements > 0);
            _dctd.GetProperty(nameof(NumberOfParabolicHexaElements)).SetIsBrowsable(mesh && NumberOfParabolicHexaElements > 0);
            //
            bool visible;
            if (currentView == ViewGeometryModelResults.Geometry)
            {
                _dctd.GetProperty(nameof(GeometryType)).SetIsBrowsable(true);
                //
                _dctd.GetProperty(nameof(Volume)).SetIsBrowsable(solid && !double.IsNaN(Volume));
                _dctd.GetProperty(nameof(Area)).SetIsBrowsable(!solid && !double.IsNaN(Area));
                _dctd.GetProperty(nameof(UndeformedVolume)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(UndeformedArea)).SetIsBrowsable(false);
                //
                visible = _partProperties.PartType != PartType.Compound;
                _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(NumberOfFacets)).SetIsBrowsable(visible);
                _dctd.GetProperty(nameof(NumberOfNodes)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(NumberOfVertices)).SetIsBrowsable(visible);
                _dctd.GetProperty(nameof(Color)).SetIsBrowsable(visible);
                //
                _dctd.GetProperty(nameof(LinearTriaType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicTriaType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearQuadType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicQuadType)).SetIsBrowsable(false);
                //
                _dctd.GetProperty(nameof(LinearTetraType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicTetraType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearPyramidType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicPyramidType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearWedgeType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicWedgeType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearHexaType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicHexaType)).SetIsBrowsable(false);
            }
            else if (currentView == ViewGeometryModelResults.Model)
            {
                _dctd.GetProperty(nameof(GeometryType)).SetIsBrowsable(false);
                //
                _dctd.GetProperty(nameof(Volume)).SetIsBrowsable(solid && !double.IsNaN(Volume));
                _dctd.GetProperty(nameof(Area)).SetIsBrowsable(!solid && !double.IsNaN(Area));
                _dctd.GetProperty(nameof(UndeformedVolume)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(UndeformedArea)).SetIsBrowsable(false);
                //
                _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(true);
                _dctd.GetProperty(nameof(NumberOfFacets)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(NumberOfNodes)).SetIsBrowsable(true);
                _dctd.GetProperty(nameof(NumberOfVertices)).SetIsBrowsable(false);
                //
                Dictionary<Type, HashSet<string>> er = elementTypeNamesToRemove;
                PartProperties pp = _partProperties;
                //
                ShowHide((int)pp.LinearTriaType, nameof(LinearTriaType), typeof(FeElementTypeLinearTria), er);
                ShowHide((int)pp.ParabolicTriaType, nameof(ParabolicTriaType), typeof(FeElementTypeParabolicTria), er);
                ShowHide((int)pp.LinearQuadType, nameof(LinearQuadType), typeof(FeElementTypeLinearQuad), er);
                ShowHide((int)pp.ParabolicQuadType, nameof(ParabolicQuadType), typeof(FeElementTypeParabolicQuad), er);
                // Solid
                ShowHide((int)pp.LinearTetraType, nameof(LinearTetraType), typeof(FeElementTypeLinearTetra), er);
                ShowHide((int)pp.ParabolicTetraType, nameof(ParabolicTetraType), typeof(FeElementTypeParabolicTetra), er);
                ShowHide((int)pp.LinearPyramidType, nameof(LinearPyramidType), typeof(FeElementTypeLinearPyramid), er);
                ShowHide((int)pp.ParabolicPyramidType, nameof(ParabolicPyramidType), typeof(FeElementTypeParabolicPyramid), er);
                ShowHide((int)pp.LinearWedgeType, nameof(LinearWedgeType), typeof(FeElementTypeLinearWedge), er);
                ShowHide((int)pp.ParabolicWedgeType, nameof(ParabolicWedgeType), typeof(FeElementTypeParabolicWedge), er);
                ShowHide((int)pp.LinearHexaType, nameof(LinearHexaType), typeof(FeElementTypeLinearHexa), er);
                ShowHide((int)pp.ParabolicHexaType, nameof(ParabolicHexaType), typeof(FeElementTypeParabolicHexa), er);
            }
            else if (currentView == ViewGeometryModelResults.Results)
            {
                _dctd.GetProperty(nameof(GeometryType)).SetIsBrowsable(false);
                //
                _dctd.GetProperty(nameof(Volume)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(Area)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(UndeformedVolume)).SetIsBrowsable(solid && !double.IsNaN(UndeformedVolume));
                _dctd.GetProperty(nameof(UndeformedArea)).SetIsBrowsable(!solid && !double.IsNaN(UndeformedArea));
                //
                _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(true);
                _dctd.GetProperty(nameof(NumberOfFacets)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(NumberOfNodes)).SetIsBrowsable(true);
                _dctd.GetProperty(nameof(NumberOfVertices)).SetIsBrowsable(false);
                //
                _dctd.GetProperty(nameof(LinearTriaType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicTriaType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearQuadType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicQuadType)).SetIsBrowsable(false);
                //
                _dctd.GetProperty(nameof(LinearTetraType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicTetraType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearPyramidType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicPyramidType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearWedgeType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicWedgeType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(LinearHexaType)).SetIsBrowsable(false);
                _dctd.GetProperty(nameof(ParabolicHexaType)).SetIsBrowsable(false);
            }
        }
        private void ShowHide(int propertyElType, string propertyName, Type elType,
                              Dictionary<Type, HashSet<string>> elementTypeNamesToRemove)
        {
            HashSet<string> elementNames;
            //
            if (propertyElType != 0)
            {
                _dctd.GetProperty(propertyName).SetIsBrowsable(true);
                if (elementTypeNamesToRemove.TryGetValue(elType, out elementNames))
                    _dctd.GetProperty(propertyName).RemoveStandardValues(elementNames);
            }
            else _dctd.GetProperty(propertyName).SetIsBrowsable(false);
        }
        
    }
}
