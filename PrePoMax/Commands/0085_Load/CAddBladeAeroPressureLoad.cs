using System;
using CaeModel;
using CaeMesh;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddBladeAeroPressureLoad : PreprocessCommand
    {
        // Variables
        private string _stepName;
        private BladeAeroPressureSection[] _sections;
        private int _prevSectionCount;
        private FeSurfaceFaceTypes _surfaceFaceTypes;


        // Constructor
        public CAddBladeAeroPressureLoad(string stepName, BladeAeroPressureSection[] sections,
                                         int prevSectionCount = 0,
                                         FeSurfaceFaceTypes surfaceFaceTypes = FeSurfaceFaceTypes.Unknown)
            : base("Add blade aerodynamic pressure load")
        {
            _stepName = stepName;
            _sections = sections;
            _prevSectionCount = prevSectionCount;
            _surfaceFaceTypes = surfaceFaceTypes;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            // Remove stale artifacts up to the largest known section count (handles re-apply with fewer sections)
            int cleanCount = Math.Max(_sections.Length, _prevSectionCount);
            for (int idx = 1; idx <= cleanCount; idx++)
            {
                string loadName = string.Format("BladeAeroP-{0:D2}-Load", idx);
                string surfName = string.Format("_BladeAeroP-{0:D2}-Surf", idx);
                try { receiver.RemoveLoads(_stepName, new[] { loadName }); } catch { }
                try { receiver.RemoveSurfaces(new[] { surfName }, false); } catch { }
                foreach (FeFaceName f in Enum.GetValues(typeof(FeFaceName)))
                {
                    if (f == FeFaceName.Empty) continue;
                    string esName = string.Format("_BladeAeroP-{0:D2}-{1}-Elems", idx, f);
                    try { receiver.RemoveElementSets(new[] { esName }, false); } catch { }
                }
            }
            //
            foreach (var sec in _sections)
            {
                // One internal FeElementSet per face type in this zone
                foreach (var kv in sec.FaceGroups)
                {
                    if (kv.Value == null || kv.Value.Length == 0) continue;
                    var es = new FeElementSet(sec.ElemSetName(kv.Key), kv.Value);
                    es.Internal = true;
                    receiver.AddElementSet(es);
                }
                //
                // Internal FeSurface referencing the per-face-type element sets
                var surf = new FeSurface(sec.SurfaceName);
                surf.Internal = true;
                surf.Area = sec.ZoneArea;
                surf.SurfaceFaceTypes = _surfaceFaceTypes;
                foreach (var kv in sec.FaceGroups)
                {
                    if (kv.Value != null && kv.Value.Length > 0)
                        surf.AddElementFace(kv.Key, sec.ElemSetName(kv.Key));
                }
                receiver.AddInternalSurface(surf);
                //
                // Internal DLoad (distributed pressure) on the zone surface
                var dl = new DLoad(sec.LoadName, sec.SurfaceName,
                                   RegionTypeEnum.SurfaceName, sec.Pressure,
                                   false, false, 0);
                dl.Internal = true;
                receiver.AddLoad(_stepName, dl);
            }
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _sections.Length + " pressure zone(s)";
        }
    }
}
