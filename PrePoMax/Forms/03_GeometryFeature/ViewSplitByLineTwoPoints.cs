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
    public class ViewSplitByLineTwoPoints : ViewGeometryFeature
    {
        // Variables
        private SplitByLineTwoPoints _splitByLineTwoPoints;


        // Properties
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(0, 10, "Points")]
        [DescriptionAttribute("Select two points that define the splitting line. Hold the Shift key to " +
                              "select both in one operation. A vertex gives its own position and a " +
                              "circular edge gives its center. The line is drawn on the surface itself, " +
                              "so on a curved surface it follows the surface instead of cutting across " +
                              "it, and it is continued to the boundary of the surface.")]
        [Id(1, 2)]
        public string SplitterSelection
        {
            // What is counted is the points that were read, not the picks that were made: an edge or a
            // face can be picked without landing on a point, and counting it would say otherwise
            get { return CountToString(_splitByLineTwoPoints.NumberOfPoints, "point"); }
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
            get { return _splitByLineTwoPoints.Points[0][0]; }
            set { _splitByLineTwoPoints.Points[0][0] = value; }
        }
        //
        [CategoryAttribute("First point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the first point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 3)]
        public double Y1
        {
            get { return _splitByLineTwoPoints.Points[0][1]; }
            set { _splitByLineTwoPoints.Points[0][1] = value; }
        }
        //
        [CategoryAttribute("First point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the first point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double Z1
        {
            get { return _splitByLineTwoPoints.Points[0][2]; }
            set { _splitByLineTwoPoints.Points[0][2] = value; }
        }
        //
        [CategoryAttribute("Second point")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the second point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 4)]
        public double X2
        {
            get { return _splitByLineTwoPoints.Points[1][0]; }
            set { _splitByLineTwoPoints.Points[1][0] = value; }
        }
        //
        [CategoryAttribute("Second point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the second point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 4)]
        public double Y2
        {
            get { return _splitByLineTwoPoints.Points[1][1]; }
            set { _splitByLineTwoPoints.Points[1][1] = value; }
        }
        //
        [CategoryAttribute("Second point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the second point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 4)]
        public double Z2
        {
            get { return _splitByLineTwoPoints.Points[1][2]; }
            set { _splitByLineTwoPoints.Points[1][2] = value; }
        }


        // Constructors
        public ViewSplitByLineTwoPoints(SplitByLineTwoPoints splitByLineTwoPoints)
        {
            _splitByLineTwoPoints = splitByLineTwoPoints;                  // 1 command
            _dctd = ProviderInstaller.Install(this);                       // 2 command
            //
            UpdateVisibility();
        }


        // Methods
        public override GeometryFeature GetBase()
        {
            return _splitByLineTwoPoints;
        }
    }
}
