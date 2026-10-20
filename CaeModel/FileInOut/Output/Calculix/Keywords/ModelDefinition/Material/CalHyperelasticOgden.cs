using System;
using System.Text;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalHyperelasticOgden : CalculixKeyword
    {
        // Variables
        private HyperelasticOgden _ogden;


        // Constructor
        public CalHyperelasticOgden(HyperelasticOgden ogden)
        {
            _ogden = ogden;
        }


        // Methods
        public override string GetKeywordString()
        {
            return string.Format("*Hyperelastic, Ogden, N={0}{1}", _ogden.N, Environment.NewLine);
        }
        public override string GetDataString()
        {
            // Format: mu1, alpha1[, mu2, alpha2[, mu3, alpha3]], D1[, D2[, D3]]
            StringBuilder sb = new StringBuilder();
            sb.Append(_ogden.Mu1.Value.ToCalculiX16String());
            sb.Append(", "); sb.Append(_ogden.Alpha1.Value.ToCalculiX16String());
            if (_ogden.N >= 2)
            {
                sb.Append(", "); sb.Append(_ogden.Mu2.Value.ToCalculiX16String());
                sb.Append(", "); sb.Append(_ogden.Alpha2.Value.ToCalculiX16String());
            }
            if (_ogden.N >= 3)
            {
                sb.Append(", "); sb.Append(_ogden.Mu3.Value.ToCalculiX16String());
                sb.Append(", "); sb.Append(_ogden.Alpha3.Value.ToCalculiX16String());
            }
            sb.Append(", "); sb.Append(_ogden.D1.Value.ToCalculiX16String());
            if (_ogden.N >= 2)
            {
                sb.Append(", "); sb.Append(_ogden.D2.Value.ToCalculiX16String());
            }
            if (_ogden.N >= 3)
            {
                sb.Append(", "); sb.Append(_ogden.D3.Value.ToCalculiX16String());
            }
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }
    }
}
