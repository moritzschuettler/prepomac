// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewSplitByPlaneThreePoints : ViewGeometryFeature
    {
        // Variables
        private SplitByPlaneThreePoints _splitByPlaneThreePoints;


        // Properties
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(0, 10, "Points")]
        [DescriptionAttribute("Select three points that define the splitting plane. Hold the Shift key to select " +
                              "all three in one operation. A vertex gives its own position and a circular edge " +
                              "gives its center.")]
        [Id(1, 2)]
        public string SplitterSelection
        {
            // What is counted is the points that were read, not the picks that were made: an edge or a
            // face can be picked without landing on a point, and counting it would say otherwise
            get { return CountToString(_splitByPlaneThreePoints.NumberOfPoints, "point"); }
            set { }
        }
        //
        [CategoryAttribute("First point")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the first point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double X1
        {
            get { return _splitByPlaneThreePoints.Points[0][0]; }
            set { _splitByPlaneThreePoints.Points[0][0] = value; }
        }
        //
        [CategoryAttribute("First point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the first point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 3)]
        public double Y1
        {
            get { return _splitByPlaneThreePoints.Points[0][1]; }
            set { _splitByPlaneThreePoints.Points[0][1] = value; }
        }
        //
        [CategoryAttribute("First point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the first point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double Z1
        {
            get { return _splitByPlaneThreePoints.Points[0][2]; }
            set { _splitByPlaneThreePoints.Points[0][2] = value; }
        }
        //
        [CategoryAttribute("Second point")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the second point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 4)]
        public double X2
        {
            get { return _splitByPlaneThreePoints.Points[1][0]; }
            set { _splitByPlaneThreePoints.Points[1][0] = value; }
        }
        //
        [CategoryAttribute("Second point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the second point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 4)]
        public double Y2
        {
            get { return _splitByPlaneThreePoints.Points[1][1]; }
            set { _splitByPlaneThreePoints.Points[1][1] = value; }
        }
        //
        [CategoryAttribute("Second point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the second point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 4)]
        public double Z2
        {
            get { return _splitByPlaneThreePoints.Points[1][2]; }
            set { _splitByPlaneThreePoints.Points[1][2] = value; }
        }
        //
        [CategoryAttribute("Third point")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the third point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 5)]
        public double X3
        {
            get { return _splitByPlaneThreePoints.Points[2][0]; }
            set { _splitByPlaneThreePoints.Points[2][0] = value; }
        }
        //
        [CategoryAttribute("Third point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the third point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 5)]
        public double Y3
        {
            get { return _splitByPlaneThreePoints.Points[2][1]; }
            set { _splitByPlaneThreePoints.Points[2][1] = value; }
        }
        //
        [CategoryAttribute("Third point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the third point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 5)]
        public double Z3
        {
            get { return _splitByPlaneThreePoints.Points[2][2]; }
            set { _splitByPlaneThreePoints.Points[2][2] = value; }
        }
        //
        [CategoryAttribute("Plane")]
        [OrderedDisplayName(0, 10, "Offset")]
        [DescriptionAttribute("Enter the distance the splitting plane is moved along its normal. A positive " +
                              "value moves it the way the arrow on the plane points, a negative value the " +
                              "other way.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 6)]
        public double Offset
        {
            get { return _splitByPlaneThreePoints.Offset; }
            set { _splitByPlaneThreePoints.Offset = value; }
        }


        // Constructors
        public ViewSplitByPlaneThreePoints(SplitByPlaneThreePoints splitByPlaneThreePoints)
        {
            _splitByPlaneThreePoints = splitByPlaneThreePoints;             // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            //
            UpdateVisibility();
        }


        // Methods
        public override GeometryFeature GetBase()
        {
            return _splitByPlaneThreePoints;
        }
    }
}
