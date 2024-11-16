using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CCreateElementSetFromElementQuality : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _setName;
        private string _elementQualityMetric;
        private string[] _partNames;
        private bool _largerThan;
        private double _limit;


        // Constructor                                                                                                              
        public CCreateElementSetFromElementQuality(string name, string elementQualityMetric, string[] partNames,
                                                   bool largerThan, double limit)
            : base("Create element set from element qualities")
        {
            _setName = name;
            _elementQualityMetric = elementQualityMetric;
            _partNames = partNames;
            _largerThan = largerThan;
            _limit = limit;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.CreateElementSetFromElementQuality(_setName, _elementQualityMetric, _partNames, _largerThan, _limit);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _setName;
        }
    }
}
