// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    [PmxType(5016)]
    public enum FieldResultFilterTypeEnum
    {
        None,
        Median,
        Average,
        Smooth,
    }
    [Serializable]
    [PmxType(5017)]
    public enum FieldResultFilterSourceTypeEnum
    {
        [StandardValue("AllNodes", DisplayName = "All nodes")]
        AllNodes,
        [StandardValue("SurfaceNodes", DisplayName = "Surface nodes")]
        SurfaceNodes,
    }
    //
    [Serializable]
    [PmxType(5018)]
    public class FieldResultFilter
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected FieldResultFilterTypeEnum _type;
        [PmxKey(1)] protected FieldResultFilterSourceTypeEnum _sourceType;
        [PmxKey(2)] protected int _numberOfNeighbouringLayers;
        [PmxKey(3)] protected int _numberOfIterations;


        // Properties                                                                                                               
        public FieldResultFilterTypeEnum Type { get { return _type; } set { _type = value; } }
        public FieldResultFilterSourceTypeEnum SourceType { get { return _sourceType; } set { _sourceType = value; } }
        public int NumberOfNeighbouringLayers
        {
            get { return _numberOfNeighbouringLayers; }
            set
            {
                _numberOfNeighbouringLayers = value;
                if (_numberOfNeighbouringLayers < 1) _numberOfNeighbouringLayers = 1;
            }
        }
        public int NumberOfIterations
        {
            get { return _numberOfIterations; }
            set
            {
                _numberOfIterations = value;
                if (_numberOfIterations < 1) _numberOfIterations = 1;
            }
        }


        // Constructors                                                                                                             
        public FieldResultFilter()
        {
            _type = FieldResultFilterTypeEnum.None;
            _sourceType = FieldResultFilterSourceTypeEnum.AllNodes;
            _numberOfNeighbouringLayers = 1;
            _numberOfIterations = 1;
        }
    }
}
