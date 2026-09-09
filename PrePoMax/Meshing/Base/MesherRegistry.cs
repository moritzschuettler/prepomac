// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;

namespace PrePoMax
{
    internal class MesherRegistry
    {
        // Variables                                                                                                                
        private Dictionary<MesherTypeEnum, IMesherRunner> _runners;


        // Constructors                                                                                                             
        public MesherRegistry(params IMesherRunner[] runners)
        {
            _runners = new Dictionary<MesherTypeEnum, IMesherRunner>();
            foreach (var runner in runners) _runners.Add(runner.MesherType, runner);
        }


        // Methods                                                                                                                  
        public IMesherRunner GetRunner(MesherTypeEnum mesherType)
        {
            IMesherRunner runner;
            if (_runners.TryGetValue(mesherType, out runner)) return runner;
            else throw new NotSupportedException("MesherTypeException");
        }
        public MesherTypeEnum GetMesherType(MeshSetupItem[] mesherSetupItems)
        {
            if (mesherSetupItems != null)
            {
                foreach (var mesherSetupItem in mesherSetupItems)
                {
                    if (mesherSetupItem.MesherType != MesherTypeEnum.None) return mesherSetupItem.MesherType;
                }
            }
            return MesherTypeEnum.None;
        }
        public MesherTypeEnum GetDefaultMesherType(BasePart part)
        {
            return MesherTypeEnum.Netgen;
        }
        public IMesherRunner GetBrepRunner(GeometryPart part, MeshSetupItem[] mesherSetupItems)
        {
            MesherTypeEnum mesherType = GetMesherType(mesherSetupItems);
            if (mesherType == MesherTypeEnum.None) mesherType = MesherTypeEnum.Netgen;
            //
            IMesherRunner runner = GetRunner(mesherType);
            if (!runner.CanCreateMeshFromBrep(part)) throw new NotSupportedException("MesherTypeException");
            return runner;
        }
        public IMesherRunner GetStlRunner(GeometryPart part, MeshSetupItem[] mesherSetupItems)
        {
            MesherTypeEnum mesherType = GetMesherType(mesherSetupItems);
            if (mesherType == MesherTypeEnum.None) mesherType = GetDefaultStlMesherType(part);
            //
            IMesherRunner runner = GetRunner(mesherType);
            if (!runner.CanCreateMeshFromStl(part)) throw new NotSupportedException("MesherTypeException");
            return runner;
        }
        private MesherTypeEnum GetDefaultStlMesherType(GeometryPart part)
        {
            if (part.PartType == PartType.Solid || part.PartType == PartType.SolidAsShell ||
                part.PartType == PartType.Shell)
                return MesherTypeEnum.Netgen;
            else throw new NotSupportedException();
        }
    }
}
