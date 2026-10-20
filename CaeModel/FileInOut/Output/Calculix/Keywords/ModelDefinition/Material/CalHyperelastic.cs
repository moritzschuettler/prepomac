using System;
using System.Text;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalHyperelastic : CalculixKeyword
    {
        // Variables
        private Hyperelastic _hyperelastic;


        // Constructor
        public CalHyperelastic(Hyperelastic hyperelastic)
        {
            _hyperelastic = hyperelastic;
        }


        // Methods
        public override string GetKeywordString()
        {
            return string.Format("*Hyperelastic, Mooney-Rivlin{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}, {1}, {2}{3}",
                                 _hyperelastic.C10.Value.ToCalculiX16String(),
                                 _hyperelastic.C01.Value.ToCalculiX16String(),
                                 _hyperelastic.D1.Value.ToCalculiX16String(),
                                 Environment.NewLine);
        }
    }
}
