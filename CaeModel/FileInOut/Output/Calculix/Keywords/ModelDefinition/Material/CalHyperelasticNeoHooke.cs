using System;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalHyperelasticNeoHooke : CalculixKeyword
    {
        // Variables
        private HyperelasticNeoHooke _neoHooke;


        // Constructor
        public CalHyperelasticNeoHooke(HyperelasticNeoHooke neoHooke)
        {
            _neoHooke = neoHooke;
        }


        // Methods
        public override string GetKeywordString()
        {
            return string.Format("*Hyperelastic, Neo Hooke{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}, {1}{2}",
                                 _neoHooke.C10.Value.ToCalculiX16String(),
                                 _neoHooke.D1.Value.ToCalculiX16String(),
                                 Environment.NewLine);
        }
    }
}
