// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace CaeResults
{
    // The shell data of one result element: an expanded shell element or a layer of an expanded composite shell
    [PmxType(5003)]
    public class ShellResultElement
    {
        // Variables
        [PmxKey(0)] private int _shellElementId;
        [PmxKey(1)] private string _sectionName;
        [PmxKey(2)] private int _layerNumber;
        [PmxKey(3)] private string _materialName;
        [PmxKey(4)] private double[] _axis1;
        [PmxKey(5)] private double[] _axis2;
        [PmxKey(6)] private double[] _normal;


        // Properties
        public int ShellElementId { get { return _shellElementId; } }
        public string SectionName { get { return _sectionName; } }
        // Layer 1 is the first layer of a composite shell section; 0 is a shell section of one material
        public int LayerNumber { get { return _layerNumber; } }
        public string MaterialName { get { return _materialName; } }
        public double[] Axis1 { get { return _axis1; } }
        public double[] Axis2 { get { return _axis2; } }
        public double[] Normal { get { return _normal; } }


        // Constructor
        public ShellResultElement(int shellElementId, string sectionName, int layerNumber, string materialName,
                                  double[] axis1, double[] axis2, double[] normal)
        {
            _shellElementId = shellElementId;
            _sectionName = sectionName;
            _layerNumber = layerNumber;
            _materialName = materialName;
            _axis1 = axis1;
            _axis2 = axis2;
            _normal = normal;
        }
    }
}
