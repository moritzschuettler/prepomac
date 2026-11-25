// PrePoMax for macOS - UserControls/ProperyGrid/PropertyGridMissingValuePainter.cs recolors the painted rows of the
// Windows property grid through its window messages. The property grid of macOS has no such rows, so the missing values
// keep the text color of the grid.

using System.Drawing;
using System.Windows.Forms;

namespace UserControls
{
    public class PropertyGridMissingValuePainter
    {
        // Variables
        public static readonly Color MissingValueColor = Color.Red;


        // Constructors
        public PropertyGridMissingValuePainter(PropertyGrid propertyGrid)
        {
        }
    }
}
