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
using CaeMesh;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [PmxType(50029)]
    internal class CalLayupShellSection : CalculixKeyword
    {
        // Variables
        [PmxKey(0)] private string _elementSetName;
        [PmxKey(1)] private string _globalOrientationName;  // Optional global orientation for the section
        [PmxKey(2)] private List<CompositeShellPlyData> _pliesData;
        [PmxKey(3)] private double? _offset;


        // Properties


        // Constructor
        /// <summary>
        /// Creates a composite shell section.
        /// </summary>
        /// <param name="elementSetName">Name of the element set for this section.</param>
        /// <param name="globalOrientationName">Optional global orientation name (can be null or empty).</param>
        /// <param name="pliesData">List of ply data (thickness, material, orientation name).</param>
        public CalLayupShellSection(string elementSetName, string globalOrientationName,
                                        List<CompositeShellPlyData> pliesData, double? offset = null)
        {
            _elementSetName = elementSetName;
            _globalOrientationName = globalOrientationName;
            _pliesData = pliesData;
            _offset = offset;
        }


        // Methods
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Shell section, Composite, Elset={0}", _elementSetName);
            //
            if (!string.IsNullOrEmpty(_globalOrientationName))
            {
                sb.AppendFormat(", Orientation={0}", _globalOrientationName);
            }
            if (_offset.HasValue)
            {
                sb.AppendFormat(", Offset={0}", _offset.Value.ToCalculiX16String());
            }
            sb.AppendLine();
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            foreach (var plyData in _pliesData)
            {
                // Format: thickness, , material, orientation_name
                // The second field (integration points) is left empty
                sb.AppendFormat("{0}, , {1}, {2}{3}",
                    plyData.Thickness.ToCalculiX16String(),
                    plyData.MaterialName,
                    plyData.OrientationName,
                    Environment.NewLine);
            }
            //
            return sb.ToString();
        }
    }

    /// <summary>
    /// Data structure for a single ply in a composite shell section.
    /// </summary>
    [PmxType(50030)]
    internal class CompositeShellPlyData
    {
        [field: PmxKey(0)] public double Thickness { get; set; }
        [field: PmxKey(1)] public string MaterialName { get; set; }
        [field: PmxKey(2)] public string OrientationName { get; set; }

        public CompositeShellPlyData(double thickness, string materialName, string orientationName)
        {
            Thickness = thickness;
            MaterialName = materialName;
            OrientationName = orientationName;
        }
    }
}
