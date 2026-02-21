using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using FileInOut.Output.Calculix;

namespace FileInOut.Output
{
    /// <summary>
    /// Handles the generation of CalculiX keywords for composite layups.
    /// </summary>
    internal class CompositeLayupWriter
    {
        // Variables
        private CompositeLayup _layup;
        private FeMesh _mesh;
        private List<CalOrientation> _orientationKeywords;
        private List<CalCompositeShellSection> _sectionKeywords;
        private List<string> _warnings;


        // Properties
        public List<CalOrientation> OrientationKeywords { get { return _orientationKeywords; } }
        public List<CalCompositeShellSection> SectionKeywords { get { return _sectionKeywords; } }
        public List<string> Warnings { get { return _warnings; } }


        // Constructor
        public CompositeLayupWriter(CompositeLayup layup, FeMesh mesh)
        {
            _layup = layup;
            _mesh = mesh;
            _orientationKeywords = new List<CalOrientation>();
            _sectionKeywords = new List<CalCompositeShellSection>();
            _warnings = new List<string>();
        }


        // Methods
        /// <summary>
        /// Processes the composite layup and generates all required keywords.
        /// </summary>
        public void Process()
        {
            if (_layup.Plies == null || _layup.Plies.Count == 0)
                return;

            // Get the region from the first ply
            string regionName = _layup.Plies[0].Region;
            if (string.IsNullOrEmpty(regionName))
                return;

            // Check if all plies have the same region and warn if not
            CheckPlyRegionsConsistency(regionName);

            // Generate orientation for each ply
            List<CompositeShellPlyData> pliesData = new List<CompositeShellPlyData>();
            for (int i = 0; i < _layup.Plies.Count; i++)
            {
                Ply ply = _layup.Plies[i];
                string orientationName = GenerateOrientationForPly(ply, i);
                pliesData.Add(new CompositeShellPlyData(ply.Thickness, ply.Material, orientationName));
            }

            // Create the composite shell section
            CalCompositeShellSection section = new CalCompositeShellSection(regionName, null, pliesData);
            _sectionKeywords.Add(section);
        }

        /// <summary>
        /// Checks if all plies have the same region and adds a warning if not.
        /// </summary>
        private void CheckPlyRegionsConsistency(string firstRegion)
        {
            List<string> differentRegionPlies = new List<string>();
            //
            for (int i = 1; i < _layup.Plies.Count; i++)
            {
                Ply ply = _layup.Plies[i];
                if (ply.Region != firstRegion)
                {
                    differentRegionPlies.Add(string.Format("{0} (region: {1})", ply.Name, ply.Region));
                }
            }
            //
            if (differentRegionPlies.Count > 0)
            {
                string warning = string.Format(
                    "Composite layup '{0}': The following plies have a different region than the first ply '{1}' (region: {2}):\n- {3}\n\n" +
                    "Only the first ply's region will be used for the shell section.",
                    _layup.Name,
                    _layup.Plies[0].Name,
                    firstRegion,
                    string.Join("\n- ", differentRegionPlies));
                _warnings.Add(warning);
            }
        }

        /// <summary>
        /// Generates an orientation keyword for a ply and adds it to the list.
        /// Returns the orientation name.
        /// </summary>
        private string GenerateOrientationForPly(Ply ply, int plyIndex)
        {
            string orientationName = string.Format("Ori_{0}_Ply{1}", _layup.Name, plyIndex);
            CalOrientation orientation;
            //
            if (_layup.OrientationDefinition == OrientationDefinitionType.PartGlobal)
            {
                // Use global coordinate system with ply rotation angle
                orientation = new CalOrientation(orientationName, ply.RotationAngle);
            }
            else // OrientationDefinitionType.CoordinateSystem
            {
                // TODO: Use explicit coordinate system when implemented
                // For now, fall back to global
                orientation = new CalOrientation(orientationName, ply.RotationAngle);
            }
            //
            _orientationKeywords.Add(orientation);
            return orientationName;
        }
    }
}
