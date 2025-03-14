using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public partial class MultiIntegerTextBox : TextBox
    {
        // Variables                                                                                                                
        private const int WM_PASTE = 0x0302;
        private string[] separator = new string[] { " ", ",", ";", "\t", Environment.NewLine };


        // Properties                                                                                                               
        public int[] Values { get { return ValuesFromString(this.Text); } }


        // Constructors                                                                                                             
        public MultiIntegerTextBox()
        {
            InitializeComponent();
        }


        // Methods                                                                                                                  
        private int[] ValuesFromString(string text)
        {
            try
            {
                string[] tmp = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 0) return null;
                //
                int[] values = new int[tmp.Length];
                for (int i = 0; i < tmp.Length; i++) values[i] = int.Parse(tmp[i]);
                return values;
            }
            catch
            {
                return null;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg != WM_PASTE)
            {
                // Handle all other messages normally
                base.WndProc(ref m);
            }
            else
            {
                // Some simplified example code that complete replaces the
                // text box content only if the clipboard contains a valid double.
                // I'll leave improvement of this behavior as an exercise :)
                if (ValuesFromString(Clipboard.GetText()) != null)
                {
                    Text = Clipboard.GetText();
                }
            }
        }
        private void NumTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                if (!(e.KeyChar == ' ' || e.KeyChar == ',' || e.KeyChar == ';'))
                {
                    // Cancel key press
                    e.Handled = true;
                }
            }
        }
    }
}
