using System;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(50044)]
    class CExtrudeShellFromSections : PreprocessCommand
    {
        [PmxKey(0)] private string _shellPartName;
        [PmxKey(1)] private string _newPartName;
        [PmxKey(2)] private bool _condenseNodes;
        [PmxKey(3)] private double _condenseTolerance;


        public CExtrudeShellFromSections(string shellPartName, string newPartName,
                                         bool condenseNodes, double condenseTolerance)
            : base("Extrude shell mesh from sections")
        {
            _shellPartName = shellPartName;
            _newPartName = newPartName;
            _condenseNodes = condenseNodes;
            _condenseTolerance = condenseTolerance;
        }


        public override bool Execute(Controller receiver)
        {
            receiver.ExtrudeShellFromSections(_shellPartName, _newPartName,
                                              _condenseNodes, _condenseTolerance);
            return true;
        }
    }
}
