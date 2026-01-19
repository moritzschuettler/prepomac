using CaeMesh;
using System;

namespace PrePoMax
{
    internal class GmshAPI : CaeMesh.GmshAPI
    {
        public GmshAPI(GmshData gmshData, Action<string> writeOutput) : base(gmshData, writeOutput)
        {
        }
    }
}