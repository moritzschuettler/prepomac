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
    public class ViewSplitByExtendedFace : ViewGeometryFeature
    {
        // Variables
        private SplitByExtendedFace _splitByExtendedFace;


        // Properties
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(0, 10, "Face")]
        [DescriptionAttribute("Select the faces whose surfaces are continued past their boundaries into the " +
                              "splitting tool. Hold the Shift key to select more than one. A face may lie on a " +
                              "part that is not being split.")]
        [Id(1, 2)]
        public string SplitterSelection
        {
            get { return SelectionToString(_splitByExtendedFace.SplitterIds, "face"); }
            set { }
        }
        //
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(1, 10, "Surface type")]
        [DescriptionAttribute("The surfaces the picked faces lie on. Only a plane, a cylinder, a cone, a sphere " +
                              "and a torus continue past the face boundary; a face that lies on a spline patch " +
                              "is left out of the tool rather than stopping it, and gets no symbol.")]
        [ReadOnlyAttribute(true)]
        [Id(2, 2)]
        public string SurfaceType
        {
            get
            {
                GeomFaceType[] faceTypes = _splitByExtendedFace.FaceTypes;
                if (faceTypes == null || faceTypes.Length == 0) return "Empty";
                // The same surface picked twice is one entry, so a long selection still reads as a list
                List<string> names = new List<string>();
                int numOfLeftOutFaces = 0;
                foreach (GeomFaceType faceType in faceTypes)
                {
                    if (!SplitByExtendedFace.CanBeExtended(faceType)) { numOfLeftOutFaces++; continue; }
                    if (!names.Contains(faceType.ToString())) names.Add(faceType.ToString());
                }
                //
                if (names.Count == 0) return "None can be extended";
                //
                string text = string.Join(", ", names);
                if (numOfLeftOutFaces == 1) text += " - 1 face left out";
                else if (numOfLeftOutFaces > 1) text += " - " + numOfLeftOutFaces + " faces left out";
                return text;
            }
            set { }
        }


        //
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(2, 10, "Offset")]
        [DescriptionAttribute("Enter the distance each continued surface is moved along its own " +
                              "normal before it cuts. A positive value moves a plane along its " +
                              "normal and grows the radius of a cylinder, a cone, a sphere and a " +
                              "torus; an offset that would leave no surface at all takes the face " +
                              "out of the tool.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 2)]
        public double Offset
        {
            get { return _splitByExtendedFace.Offset; }
            set { _splitByExtendedFace.Offset = value; }
        }


        // Constructors
        public ViewSplitByExtendedFace(SplitByExtendedFace splitByExtendedFace)
        {
            _splitByExtendedFace = splitByExtendedFace;                     // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            //
            UpdateVisibility();
        }


        // Methods
        public override GeometryFeature GetBase()
        {
            return _splitByExtendedFace;
        }
    }
}
