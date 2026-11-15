// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using DynamicTypeDescriptor;
using System.Runtime.InteropServices;

namespace PrePoMax
{
    [Serializable]
    [PmxType(10524)]
    public class AnnotationSettings : ISettings
    {
        // Variables                                                                                                                
        [PmxKey(0)] private AnnotationBackgroundType _backgroundType;
        [PmxKey(1)] private bool _drawBorder;
        //
        [PmxKey(2)] private bool _showNodeId;
        [PmxKey(3)] private bool _showCoordinates;
        //
        [PmxKey(4)] private bool _showEdgeSurId;
        [PmxKey(5)] private bool _showEdgeSurType;
        [PmxKey(6)] private bool _showEdgeSurTopology;
        [PmxKey(7)] private bool _showEdgeSurSize;
        [PmxKey(8)] private bool _showEdgeSurMax;
        [PmxKey(9)] private bool _showEdgeSurMin;
        [PmxKey(10)] private bool _showEdgeSurSum;
        [PmxKey(11)] private bool _showEdgeSurAvg;
        //
        [PmxKey(12)] private bool _showPartName;
        [PmxKey(13)] private bool _showPartId;
        [PmxKey(14)] private bool _showPartType;
        [PmxKey(15)] private bool _showPartNumberOfElements;
        [PmxKey(16)] private bool _showPartNumberOfNodes;
        [PmxKey(17)] private bool _showPartVolumeArea;
        [PmxKey(18)] private bool _showPartMass;


        // Properties                                                                                                               
        public AnnotationBackgroundType BackgroundType
        {
            get { return _backgroundType; }
            set
            {
                if (value != _backgroundType)
                {
                    _backgroundType = value;
                    if (_backgroundType == AnnotationBackgroundType.White) _drawBorder = true;
                }
            }
        }
        public bool DrawBorder { get { return _drawBorder; } set { _drawBorder = value; } }
        //
        public bool ShowNodeId { get { return _showNodeId; } set { _showNodeId = value; } }
        public bool ShowCoordinates { get { return _showCoordinates; } set { _showCoordinates = value; } }
        //
        public bool ShowEdgeSurId { get { return _showEdgeSurId; } set { _showEdgeSurId = value; } }
        public bool ShowEdgeSurType { get { return _showEdgeSurType; } set { _showEdgeSurType = value; } }
        public bool ShowEdgeSurTopology { get { return _showEdgeSurTopology; } set { _showEdgeSurTopology = value; } }
        public bool ShowEdgeSurSize { get { return _showEdgeSurSize; } set { _showEdgeSurSize = value; } }
        public bool ShowEdgeSurMax { get { return _showEdgeSurMax; } set { _showEdgeSurMax = value; } }
        public bool ShowEdgeSurMin { get { return _showEdgeSurMin; } set { _showEdgeSurMin = value; } }
        public bool ShowEdgeSurSum { get { return _showEdgeSurSum; } set { _showEdgeSurSum = value; } }
        public bool ShowEdgeSurAvg { get { return _showEdgeSurAvg; } set { _showEdgeSurAvg = value; } }
        //
        public bool ShowPartName { get { return _showPartName; } set { _showPartName = value; } }
        public bool ShowPartId { get { return _showPartId; } set { _showPartId = value; } }
        public bool ShowPartType { get { return _showPartType; } set { _showPartType = value; } }
        public bool ShowPartNumberOfElements
        {
            get { return _showPartNumberOfElements; }
            set { _showPartNumberOfElements = value; }
        }
        public bool ShowPartNumberOfNodes { get { return _showPartNumberOfNodes; } set { _showPartNumberOfNodes = value; } }
        public bool ShowPartVolumeArea { get { return _showPartVolumeArea; } set { _showPartVolumeArea = value; } }
        public bool ShowPartMass { get { return _showPartMass; } set { _showPartMass = value; } }


        // Constructors                                                                                                             
        public AnnotationSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _backgroundType = AnnotationBackgroundType.White;
            _drawBorder = true;
            //
            _showNodeId = true;
            _showCoordinates = true;
            //
            _showEdgeSurId = true;
            _showEdgeSurType = true;
            _showEdgeSurTopology = true;
            _showEdgeSurSize = true;
            _showEdgeSurMax = true;
            _showEdgeSurMin = true;
            _showEdgeSurSum = true;
            _showEdgeSurAvg = true;
            //
            _showPartName = true;
            _showPartId = true;
            _showPartType = true;
            _showPartNumberOfElements = true;
            _showPartNumberOfNodes = true;
            _showPartVolumeArea = true;
            _showPartMass = true;
        }
    }
}
