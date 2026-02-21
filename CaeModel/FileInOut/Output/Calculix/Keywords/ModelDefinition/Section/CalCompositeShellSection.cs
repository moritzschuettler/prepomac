using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalCompositeShellSection : CalculixKeyword
    {
        // Variables
        private string _elementSetName;
        private string _globalOrientationName;  // Optional global orientation for the section
        private List<CompositeShellPlyData> _pliesData;


        // Properties


        // Constructor
        /// <summary>
        /// Creates a composite shell section.
        /// </summary>
        /// <param name="elementSetName">Name of the element set for this section.</param>
        /// <param name="globalOrientationName">Optional global orientation name (can be null or empty).</param>
        /// <param name="pliesData">List of ply data (thickness, material, orientation name).</param>
        public CalCompositeShellSection(string elementSetName, string globalOrientationName,
                                        List<CompositeShellPlyData> pliesData)
        {
            _elementSetName = elementSetName;
            _globalOrientationName = globalOrientationName;
            _pliesData = pliesData;
        }


        // Methods
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Shell section, Composite, Elset={0}", _elementSetName);
            //
            if (!string.IsNullOrEmpty(_globalOrientationName))
            {
                sb.AppendFormat(", Orientation={0}", _globalOrientationName);
            }
            sb.AppendLine();
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            foreach (var plyData in _pliesData)
            {
                // Format: thickness, , material, orientation_name
                // The second field (integration points) is left empty
                sb.AppendFormat("{0}, , {1}, {2}{3}",
                    plyData.Thickness.ToCalculiX16String(),
                    plyData.MaterialName,
                    plyData.OrientationName,
                    Environment.NewLine);
            }
            //
            return sb.ToString();
        }
    }

    /// <summary>
    /// Data structure for a single ply in a composite shell section.
    /// </summary>
    [Serializable]
    internal class CompositeShellPlyData
    {
        public double Thickness { get; set; }
        public string MaterialName { get; set; }
        public string OrientationName { get; set; }

        public CompositeShellPlyData(double thickness, string materialName, string orientationName)
        {
            Thickness = thickness;
            MaterialName = materialName;
            OrientationName = orientationName;
        }
    }
}
