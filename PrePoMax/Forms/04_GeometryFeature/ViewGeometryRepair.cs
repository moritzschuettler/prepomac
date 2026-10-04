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
    public class ViewGeometryRepair
    {
        // Variables                                                                                                                
        private GeometryRepair _geometryRepair;
        private DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Parts")]
        [DescriptionAttribute("Select the CAD parts to repair. Hold the Shift key to select more than one part. A " +
                              "sub-part is repaired together with its compound.")]
        [Id(1, 1)]
        public string RegionSelection
        {
            get
            {
                int count = _geometryRepair.CreationIds == null ? 0 : _geometryRepair.CreationIds.Length;
                if (count <= 0) return "Empty";
                else if (count == 1) return "1 part";
                else return count + " parts";
            }
            set { }
        }
        //
        [CategoryAttribute("Repairs")]
        [OrderedDisplayName(0, 10, "Sew faces")]
        [DescriptionAttribute("Sew loose faces and shells whose edges are closer than the tolerance into connected " +
                              "shells. Only parts without solids are sewn.")]
        [Id(1, 2)]
        public bool SewFaces { get { return _geometryRepair.SewFaces; } set { _geometryRepair.SewFaces = value; } }
        //
        [CategoryAttribute("Repairs")]
        [OrderedDisplayName(1, 10, "Fix shape")]
        [DescriptionAttribute("Fix the faults the modelling kernel finds in the topology: edge tolerances, wires " +
                              "that do not close, missing curves on surfaces and wrong orientations.")]
        [Id(2, 2)]
        public bool FixShape { get { return _geometryRepair.FixShape; } set { _geometryRepair.FixShape = value; } }
        //
        [CategoryAttribute("Repairs")]
        [OrderedDisplayName(2, 10, "Remove small faces")]
        [DescriptionAttribute("Remove faces narrower than the tolerance - spots and strips - and close the gap " +
                              "they leave.")]
        [Id(3, 2)]
        public bool RemoveSmallFaces
        {
            get { return _geometryRepair.RemoveSmallFaces; }
            set { _geometryRepair.RemoveSmallFaces = value; }
        }
        //
        [CategoryAttribute("Repairs")]
        [OrderedDisplayName(3, 10, "Remove small edges")]
        [DescriptionAttribute("Remove edges shorter than the tolerance by merging their vertices.")]
        [Id(4, 2)]
        public bool RemoveSmallEdges
        {
            get { return _geometryRepair.RemoveSmallEdges; }
            set { _geometryRepair.RemoveSmallEdges = value; }
        }
        //
        [CategoryAttribute("Repairs")]
        [OrderedDisplayName(4, 10, "Fuse faces")]
        [DescriptionAttribute("Fuse every two neighbouring faces that lie on one surface and bound the same solids " +
                              "into one face. This also fuses the faces a Split Shell divided.")]
        [Id(5, 2)]
        public bool FuseFaces { get { return _geometryRepair.FuseFaces; } set { _geometryRepair.FuseFaces = value; } }
        //
        [CategoryAttribute("Repairs")]
        [OrderedDisplayName(5, 10, "Join edges")]
        [DescriptionAttribute("Join every two edges that meet at a vertex no other edge meets and lie on one " +
                              "curve into one edge.")]
        [Id(6, 2)]
        public bool JoinEdges { get { return _geometryRepair.JoinEdges; } set { _geometryRepair.JoinEdges = value; } }
        //
        [CategoryAttribute("Options")]
        [OrderedDisplayName(0, 10, "Tolerance")]
        [DescriptionAttribute("Enter the size below which an edge or a face is small and up to which a gap is " +
                              "sewn. The default is one ten-thousandth of the diagonal of each part.")]
        [TypeConverter(typeof(StringLengthDefaultConverter))]
        [Id(1, 3)]
        public double Tolerance { get { return _geometryRepair.Tolerance; } set { _geometryRepair.Tolerance = value; } }


        // Constructors                                                                                                             
        public ViewGeometryRepair(GeometryRepair geometryRepair, double initialTolerance)
        {
            _geometryRepair = geometryRepair;                   // 1 command
            _dctd = ProviderInstaller.Install(this);            // 2 command
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(SewFaces));
            _dctd.RenameBooleanPropertyToYesNo(nameof(FixShape));
            _dctd.RenameBooleanPropertyToYesNo(nameof(RemoveSmallFaces));
            _dctd.RenameBooleanPropertyToYesNo(nameof(RemoveSmallEdges));
            _dctd.RenameBooleanPropertyToYesNo(nameof(FuseFaces));
            _dctd.RenameBooleanPropertyToYesNo(nameof(JoinEdges));
            //
            StringLengthDefaultConverter.SetInitialValue = initialTolerance.ToString();
        }


        // Methods                                                                                                                  
        public GeometryRepair GetBase()
        {
            return _geometryRepair;
        }
    }
}
