using System;
using System.Text;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalHyperfoam : CalculixKeyword
    {
        // Variables
        private Hyperfoam _hyperfoam;


        // Constructor
        public CalHyperfoam(Hyperfoam hyperfoam)
        {
            _hyperfoam = hyperfoam;
        }


        // Methods
        public override string GetKeywordString()
        {
            return string.Format("*Hyperfoam, N={0}{1}", _hyperfoam.N, Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_hyperfoam.Mu1.Value.ToCalculiX16String());
            sb.Append(", "); sb.Append(_hyperfoam.Alpha1.Value.ToCalculiX16String());
            sb.Append(", "); sb.Append(_hyperfoam.Nu1.Value.ToCalculiX16String());
            if (_hyperfoam.N >= 2)
            {
                sb.Append(", "); sb.Append(_hyperfoam.Mu2.Value.ToCalculiX16String());
                sb.Append(", "); sb.Append(_hyperfoam.Alpha2.Value.ToCalculiX16String());
                sb.Append(", "); sb.Append(_hyperfoam.Nu2.Value.ToCalculiX16String());
            }
            if (_hyperfoam.N >= 3)
            {
                sb.Append(", "); sb.Append(_hyperfoam.Mu3.Value.ToCalculiX16String());
                sb.Append(", "); sb.Append(_hyperfoam.Alpha3.Value.ToCalculiX16String());
                sb.Append(", "); sb.Append(_hyperfoam.Nu3.Value.ToCalculiX16String());
            }
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }
    }
}
