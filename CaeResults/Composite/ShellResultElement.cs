// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeResults
{
    // The shell data of one result element: an expanded shell element or a layer of an expanded composite shell
    [Serializable]
    public class ShellResultElement
    {
        // Variables
        private int _shellElementId;
        private string _sectionName;
        private int _layerNumber;
        private string _materialName;
        private double[] _axis1;
        private double[] _axis2;
        private double[] _normal;


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
