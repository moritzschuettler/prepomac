using System;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(50045)]
    class CExtrudeShellUniform : PreprocessCommand
    {
        [PmxKey(0)] private string _shellPartName;
        [PmxKey(1)] private string _newPartName;
        [PmxKey(2)] private double _totalThickness;
        [PmxKey(3)] private int _numSubdivisions;
        [PmxKey(4)] private bool _condenseNodes;
        [PmxKey(5)] private double _condenseTolerance;


        public CExtrudeShellUniform(string shellPartName, string newPartName,
                                    double totalThickness, int numSubdivisions,
                                    bool condenseNodes, double condenseTolerance)
            : base("Create solids by extrusion")
        {
            _shellPartName = shellPartName;
            _newPartName = newPartName;
            _totalThickness = totalThickness;
            _numSubdivisions = numSubdivisions;
            _condenseNodes = condenseNodes;
            _condenseTolerance = condenseTolerance;
        }


        public override bool Execute(Controller receiver)
        {
            receiver.ExtrudeShellUniform(_shellPartName, _newPartName,
                                         _totalThickness, _numSubdivisions,
                                         _condenseNodes, _condenseTolerance);
            return true;
        }
    }
}
