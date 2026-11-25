using System;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [PmxType(50025)]
    internal class CalHyperelasticNeoHooke : CalculixKeyword
    {
        // Variables
        [PmxKey(0)] private HyperelasticNeoHooke _neoHooke;


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
