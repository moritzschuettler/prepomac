using System;
using CaeModel;
using CaeMesh;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddBladeAerodynamicLoad : PreprocessCommand
    {
        // Variables
        private string _stepName;
        private BladeAeroSection[] _sections;


        // Constructor
        public CAddBladeAerodynamicLoad(string stepName, BladeAeroSection[] sections)
            : base("Add blade aerodynamic load")
        {
            _stepName = stepName;
            _sections = sections;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            foreach (var sec in _sections)
            {
                // Remove stale artifacts from a prior execution (idempotency)
                try { receiver.RemoveLoads(_stepName, new[] { sec.LoadName }); } catch { }
                try { receiver.RemoveConstraints(new[] { sec.ConstraintName }); } catch { }
                try { receiver.RemoveModelReferencePoints(new[] { sec.RefPointName }); } catch { }
                try { receiver.RemoveNodeSets(new[] { sec.NodeSetName }, false); } catch { }
                //
                // Internal node set — hidden from tree
                var ns = new FeNodeSet(sec.NodeSetName, sec.NodeIds);
                ns.Internal = true;
                receiver.AddNodeSet(ns);
                //
                // Reference point at section barycenter
                var rp = new FeReferencePoint(sec.RefPointName,
                    sec.Barycenter[0], sec.Barycenter[1], sec.Barycenter[2]);
                receiver.AddModelReferencePoint(rp);
                //
                // Rigid body coupling: RP master → node set slave
                var rb = new RigidBody(sec.ConstraintName, sec.RefPointName,
                    sec.NodeSetName, RegionTypeEnum.NodeSetName, false);
                receiver.AddConstraint(rb, update: true);
                //
                // Concentrated force on RP in computed force direction
                double f = sec.Force;
                double[] d = sec.ForceDir;
                var cl = new CLoad(sec.LoadName, sec.RefPointName,
                    RegionTypeEnum.ReferencePointName,
                    f * d[0], f * d[1], f * d[2], false, false, 0);
                receiver.AddLoad(_stepName, cl);
            }
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _sections.Length + " section(s)";
        }
    }
}
