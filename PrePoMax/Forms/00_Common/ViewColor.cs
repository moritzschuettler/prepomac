using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Drawing;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewColor
    {
        // Variables                                                                                                                
        private Color _color;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [Category("Appearance")]
        [OrderedDisplayName(0, 10, "Part color")]
        [Description("Select part color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = Color.FromArgb(Math.Max((byte)25, value.A), value);
            }
        }


        // Constructors                                                                                                             
        public ViewColor(Color color)
        {
            _color = color;
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public Color GetColor()
        {
            return _color;
        }
    }
}
