using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeJob;
using FileInOut.Output.Calculix;
using CaeGlobals;
using FastColoredTextBoxNS;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PrePoMax.Forms
{
    
    public partial class FrmCalculixKeywordEditor : UserControls.PrePoMaxChildForm
    {
        // dll routines
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);


        // Variables                                                                                                                
        private List<CalculixKeyword> _keywords;
        private OrderedDictionary<int[], CalculixUserKeyword> _userKeywords;
        private int _selectedKeywordFirstLine;
        private int _selectedKeywordNumOfLines;
        private bool _adding;
        // Styles
        private Style[] allStyles;
        private Style GreenStyle = new TextStyle(Brushes.Green,
                                                 new SolidBrush(Color.FromArgb(230, 255, 230)),
                                                 FontStyle.Regular);
        private Style BlueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private Style GrayStyle = new TextStyle(Brushes.Gray, null, FontStyle.Regular);
        

        // Properties                                                                                                               
        public List<CalculixKeyword> Keywords { get { return _keywords; } set { _keywords = value; } }
        public OrderedDictionary<int[], CalculixUserKeyword> UserKeywords { get { return _userKeywords; } set { _userKeywords = value; } }


        // Constructors                                                                                                             
        public FrmCalculixKeywordEditor()
        {
            InitializeComponent();
            //
            allStyles = new Style[] { GreenStyle, BlueStyle, GrayStyle };
        }


        // Event handlers                                                                                                           
        private void cltvKeywordsTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (cltvKeywordsTree.SelectedNode != null)
            {
                bool userNode = false;
                //
                if (cltvKeywordsTree.SelectedNode.Tag is KeywordAtNode kn1 && kn1.Keyword is CalculixUserKeyword)
                    userNode = true;
                // De-reference text changed event
                fctbKeyword.TextChanged -= fctbKeyword_TextChanged;
                // Clear keyword textbox
                fctbKeyword.Clear();
                // Add keyword data to keyword textbox
                fctbKeyword.Tag = cltvKeywordsTree.SelectedNode.Tag;
                if (fctbKeyword.Tag != null && fctbKeyword.Tag is KeywordAtNode kn2)
                {
                    fctbKeyword.Text = kn2.Data;
                    fctbKeyword.ReadOnly = !userNode;
                    //
                    _selectedKeywordNumOfLines = kn2.NumOfLines;
                }
                else
                {
                    _selectedKeywordFirstLine = 0;
                    _selectedKeywordNumOfLines = 0;
                }
                // Find the first line of the selected keyword
                bool nodeFound = false;
                _selectedKeywordFirstLine =
                    GetFirstLineOfSelectedNode(cltvKeywordsTree.Nodes[0], cltvKeywordsTree.SelectedNode, ref nodeFound);
                // Re-reference text changed event
                fctbKeyword.TextChanged += fctbKeyword_TextChanged;
                //
                UpdateKeywordTextBox();
            }
        }
        //
        private void fctbKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Control && e.KeyCode == Keys.V)
            //{
            //    ((RichTextBox)sender).Paste(DataFormats.GetFormat("Text"));
            //    e.Handled = true;
            //}
        }
        private void fctbKeyword_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(DateTime.Now.Millisecond.ToString() + " ms");
            tUpdate.Stop();
            tUpdate.Start();
            tUpdate.Interval = 500;
        }
        private void fctbInpFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set style
            e.ChangedRange.ClearStyle(allStyles);
            e.ChangedRange.SetStyle(GreenStyle, @"\*\*.*$", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(BlueStyle, @"\*.*$", RegexOptions.Multiline);
            e.ChangedRange.SetStyle(GrayStyle, @"\.\.\..*$", RegexOptions.Multiline);
        }

        private void btnAddKeyword_Click(object sender, EventArgs e)
        {
            if (cltvKeywordsTree.SelectedNode != null)
            {
                _adding = true;
                LockWindowUpdate(cltvKeywordsTree.Handle);
                //
                CalculixUserKeyword keyword = new CalculixUserKeyword("User keyword");
                TreeNode node = cltvKeywordsTree.SelectedNode.Nodes.Add("User keyword");
                //
                cltvKeywordsTree.Focus();   // must be first
                AddUserKeywordToTreeNode(keyword, node);
                cltvKeywordsTree.SelectedNode.Expand();
                cltvKeywordsTree.SelectedNode = node;
                //
                LockWindowUpdate(IntPtr.Zero);
                _adding = false;
            }
        }
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (cltvKeywordsTree.SelectedNode != null)
            {
                TreeNode node = cltvKeywordsTree.SelectedNode;
                //if (node.Tag is KeywordAtNode kn && kn.Keyword is CalculixUserKeyword)
                {
                    TreeNodeCollection collection;
                    TreeNode parent = cltvKeywordsTree.SelectedNode.Parent;
                    if (parent != null) collection = parent.Nodes;
                    else collection = cltvKeywordsTree.Nodes;
                    //
                    int index = collection.IndexOf(node);
                    if (index > 0)
                    {
                        TreeNode nodeTop = collection[index - 1];
                        TreeNode nodeBottom = collection[index];
                        // At least one keyword must be a user keyword
                        if (nodeTop.Tag is KeywordAtNode knt && knt.Keyword is CalculixUserKeyword ||
                            nodeBottom.Tag is KeywordAtNode knb && knb.Keyword is CalculixUserKeyword)
                        {
                            cltvKeywordsTree.SelectedNode = nodeTop;
                            btnMoveDown_Click(null, null);
                            cltvKeywordsTree.SelectedNode = node;
                        }
                    }
                }
            }
        }
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (cltvKeywordsTree.SelectedNode != null)
            {
                TreeNode node = cltvKeywordsTree.SelectedNode;
                //
                TreeNodeCollection collection;
                TreeNode parent = node.Parent;
                if (parent != null) collection = parent.Nodes;
                else collection = cltvKeywordsTree.Nodes;
                //
                int index = collection.IndexOf(node);
                if (index < collection.Count - 1)
                {
                    TreeNode nodeTop = collection[index];
                    TreeNode nodeBottom = collection[index + 1];
                    // At least one keyword must be a user keyword
                    if (nodeTop.Tag is KeywordAtNode knt && knt.Keyword is CalculixUserKeyword ||
                        nodeBottom.Tag is KeywordAtNode knb && knb.Keyword is CalculixUserKeyword)
                    {
                        //
                        int numOfLinesTop = GetNumberOfLinesOfNode(nodeTop);
                        string textTop = ReplaceText(_selectedKeywordFirstLine, numOfLinesTop, null);
                        int numOfLinesBottom = GetNumberOfLinesOfNode(nodeBottom);
                        string textBottom = ReplaceText(_selectedKeywordFirstLine, numOfLinesBottom, null);
                        //
                        ReplaceText(_selectedKeywordFirstLine, 0, textTop);
                        ReplaceText(_selectedKeywordFirstLine, 0, textBottom);
                        //
                        LockWindowUpdate(cltvKeywordsTree.Handle);
                        collection.RemoveAt(index);
                        collection.Insert(index + 1, node);
                        LockWindowUpdate(IntPtr.Zero);
                        //
                        cltvKeywordsTree.SelectedNode = node;
                    }
                }
            }
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (cltvKeywordsTree.SelectedNode != null) DeleteKeywordByTreeNode(cltvKeywordsTree.SelectedNode);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void cbHide_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAllKeywords();
        }
        //
        private void btnOK_Click(object sender, EventArgs e)
        {
            _userKeywords = new OrderedDictionary<int[], CalculixUserKeyword>("User CalculiX keywords");
            //
            FindUserKeywords(cltvKeywordsTree.Nodes[0], _userKeywords);
            //
            this.DialogResult = DialogResult.OK;
        }
        private void FindUserKeywords(TreeNode node, OrderedDictionary<int[], CalculixUserKeyword> userKeywords)
        {
            if (node.Tag != null && node.Tag is KeywordAtNode kn && kn.Keyword is CalculixUserKeyword userKeyword)
            {
                List<int> indices = new List<int>();
                GetNodeIndices(node, indices);
                userKeywords.Add(indices.ToArray(), userKeyword);
            }
            //
            foreach (TreeNode childNode in node.Nodes)
            {
                FindUserKeywords(childNode, userKeywords);
            }
        }
        private void GetNodeIndices(TreeNode node, List<int> indices)
        {
            TreeNode parent = node.Parent;
            if (parent != null)
            {
                GetNodeIndices(parent, indices);
                //
                indices.Add(parent.Nodes.IndexOf(node));
            }
        }


        // Methods                                                                                                                  
        public void PrepareForm()
        {
            cltvKeywordsTree.Nodes.Clear();
            fctbKeyword.Clear();
            fctbInpFile.Clear();
            //
            TreeNode node = new TreeNode();
            node.Text = "CalculiX inp file";
            cltvKeywordsTree.Nodes.Add(node);
            // Build the keyword tree
            int index;
            foreach (CalculixKeyword keyword in _keywords)
            {
                index = node.Nodes.Add(new TreeNode());
                AddKeywordToTreeNode(keyword, node.Nodes[index]);
            }
            // Add user keywords
            if (_userKeywords != null)
            {
                foreach (var entry in _userKeywords)
                {
                    AddUserKeywordToTreeByIndex(entry.Key, entry.Value.DeepClone());
                }
            }
            // Clear the keyword editor
            fctbKeyword.Clear();
            // Output tree to the inp read-only textbox
            WriteTreeToTextBox();
            // Expand first tree node
            node.Expand();
            //
            _selectedKeywordFirstLine = -1;
            _selectedKeywordNumOfLines = -1;
            _adding = false;
            //
            if (node.Nodes.Count > 1) cltvKeywordsTree.SelectedNode = node.Nodes[0];
        }
        private void UpdateAllKeywords()
        {
            UpdateKeywordDataInTree();
            //
            WriteTreeToTextBox();
            //
            cltvKeywordsTree_AfterSelect(null, null);
        }
        // Add keywords to tree
        private void AddKeywordToTreeNode(CalculixKeyword keyword, TreeNode node)
        {
            string nodeText;
            if (keyword is CalTitle ct) nodeText = ct.Title;
            else nodeText = keyword.GetKeywordString();
            //
            nodeText = GetFirstLineFromMultiline(nodeText);
            //
            node.Text = nodeText;
            node.Name = node.Text;
            //
            KeywordAtNode keywordAtNode = new KeywordAtNode();
            keywordAtNode.Keyword = keyword;
            keywordAtNode.Data = FileInOut.Output.CalculixFileWriter.GetShortKeywordData(keyword, cbHide.Checked);
            keywordAtNode.NumOfLines = 
                keywordAtNode.Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
            node.Tag = keywordAtNode;
            //
            foreach (var childKeyword in keyword.Keywords)
            {
                TreeNode childNode = new TreeNode();
                node.Nodes.Add(childNode);
                AddKeywordToTreeNode(childKeyword, childNode);
            }
        }
        private void AddUserKeywordToTreeByIndex(int[] indices, CalculixKeyword keyword)
        {
            bool deactivated = false;
            TreeNode node = cltvKeywordsTree.Nodes[0];
            //
            for (int i = 0; i < indices.Length - 1; i++)
            {
                node.Expand();
                if (indices[i] < node.Nodes.Count)
                {
                    node = node.Nodes[indices[i]];
                    if (node.Tag != null && node.Tag is CalDeactivated) deactivated = true;
                }
                else return;
            }
            //
            TreeNode child = node.Nodes.Insert(indices[indices.Length - 1], "");
            // User keyword should not be deactivated in the user editor to enable editing
            AddUserKeywordToTreeNode(keyword, child);
            //
            node.Expand();
        }
        private void AddUserKeywordToTreeNode(CalculixKeyword keyword, TreeNode node)
        {
            node.Text = GetFirstLineFromMultiline(keyword.GetKeywordString());
            //
            KeywordAtNode keywordAtNode = new KeywordAtNode();
            keywordAtNode.Keyword = keyword;
            keywordAtNode.Data = FileInOut.Output.CalculixFileWriter.GetShortKeywordData(keyword, cbHide.Checked);
            keywordAtNode.NumOfLines =
                keywordAtNode.Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
            node.Tag = keywordAtNode;
            Font font = new Font(cltvKeywordsTree.Font, FontStyle.Bold);
            node.NodeFont = font;

            //node.BackColor = Color.FromArgb(195, 195, 255);
            //node.BackColor = Color.FromArgb(230, 255, 230);
            //node.BackColor = Color.FromArgb(230, 255, 230);
            //node.ForeColor = Color.Green;
            //node.BackColor = Color.FromArgb(149, 91, 165);

            node.ForeColor = Color.Black;
            node.BackColor = Color.FromArgb(189, 160, 204);
        }
        private void UpdateKeywordDataInTree()
        {
            foreach (TreeNode node in cltvKeywordsTree.Nodes)
            {
                UpdateKeywordDataInTreeNode(node);
            }
        }
        private void UpdateKeywordDataInTreeNode(TreeNode node)
        {
            if (node.Tag != null && node.Tag is KeywordAtNode kn)
            {
                kn.Data = FileInOut.Output.CalculixFileWriter.GetShortKeywordData(kn.Keyword, cbHide.Checked);
                kn.NumOfLines = kn.Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
            }
            //
            foreach (TreeNode childNode in node.Nodes)
            {
                UpdateKeywordDataInTreeNode(childNode);
            }
        }
        // Write keywords from tree to inp textbox
        private void WriteTreeToTextBox()
        {
            if (cltvKeywordsTree.Nodes.Count > 0)
            {
                StringWriter sw = new StringWriter();
                WriteTreeNodeToStringWriter(sw, cltvKeywordsTree.Nodes[0]);
                //
                fctbInpFile.Text = sw.ToString();
            }
        }
        private void WriteTreeNodeToStringWriter(StringWriter sw, TreeNode node)
        {
            if (node.Tag != null && node.Tag is KeywordAtNode kn)
            {
                sw.WriteLine(kn.Data);
            }
            //
            foreach (TreeNode childNode in node.Nodes)
            {
                WriteTreeNodeToStringWriter(sw, childNode);
            }
        }
        private int GetFirstLineOfSelectedNode(TreeNode node, TreeNode selectedNode, ref bool nodeFound)
        {
            int count = 0;
            //
            if (!nodeFound)
            {
                if (node == selectedNode) nodeFound = true;
                else
                {
                    if (node.Tag != null && node.Tag is KeywordAtNode kn) count += kn.NumOfLines;
                    //
                    foreach (TreeNode childNode in node.Nodes)
                    {
                        count += GetFirstLineOfSelectedNode(childNode, selectedNode, ref nodeFound);
                        if (nodeFound) break;
                    }
                }
            }
            return count;
        }
        private int GetNumberOfLinesOfNode(TreeNode node)
        {
            int numOfLines = 0;
            if (node.Tag != null && node.Tag is KeywordAtNode kn) numOfLines += kn.NumOfLines;
            //
            foreach (TreeNode childNode in node.Nodes) numOfLines += GetNumberOfLinesOfNode(childNode);
            return numOfLines;
        }
        // Delete keywords
        private void DeleteKeywordByTreeNode(TreeNode node)
        {
            if (node.Tag is KeywordAtNode kn && kn.Keyword is CalculixUserKeyword)
            {
                int numOfLines = GetNumberOfLinesOfNode(node);
                ReplaceText(_selectedKeywordFirstLine, numOfLines, null);
                //
                TreeNode parent = node.Parent;
                if (parent != null)
                {
                    int index = Math.Max(0, node.Index - 1);
                    parent.Nodes.Remove(node);
                    if (parent.Nodes.Count > index) cltvKeywordsTree.SelectedNode = parent.Nodes[index];
                    else cltvKeywordsTree.SelectedNode = parent;
                }
                else
                {
                    cltvKeywordsTree.Nodes.Remove(node);
                    if (cltvKeywordsTree.Nodes.Count > 0) cltvKeywordsTree.SelectedNode = cltvKeywordsTree.Nodes[0];
                }
                //
                cltvKeywordsTree.Focus();
            }
        }
       
        // Update keyword text box
        private void UpdateKeywordTextBox()
        {
            try
            {
                // Set style
                fctbKeyword.Range.ClearStyle(allStyles);
                fctbKeyword.Range.SetStyle(GreenStyle, @"\*\*.*$", RegexOptions.Multiline);
                fctbKeyword.Range.SetStyle(BlueStyle, @"\*.*$", RegexOptions.Multiline);
                fctbKeyword.Range.SetStyle(GrayStyle, @"\.\.\..*$", RegexOptions.Multiline);
                // De-reference text changed event
                fctbKeyword.TextChanged -= fctbKeyword_TextChanged;
                // rtbKeyword.Tag is set by clicking on the tree node
                if (!fctbKeyword.ReadOnly && fctbKeyword.Tag != null && cltvKeywordsTree.SelectedNode != null)
                {
                    KeywordAtNode keywordAtNode = fctbKeyword.Tag as KeywordAtNode;
                    if (keywordAtNode.Keyword is CalculixUserKeyword userKeyword && userKeyword != null)
                    {
                        userKeyword.Data = "";
                        int count = 0;
                        //
                        foreach (var line in fctbKeyword.Lines)
                        {
                            if (count > 0) userKeyword.Data += Environment.NewLine;
                            userKeyword.Data += line;
                            count++;
                        }
                        keywordAtNode.Data = fctbKeyword.Text;
                        keywordAtNode.NumOfLines = count;
                        //
                        ReplaceText(_selectedKeywordFirstLine, _selectedKeywordNumOfLines, userKeyword.Data);
                        _selectedKeywordNumOfLines = count;
                        // Change the name of the Selected tree node
                        LockWindowUpdate(cltvKeywordsTree.Handle);
                        if (fctbKeyword.Lines.Count > 0 && fctbKeyword.Lines[0].Length > 0)
                        {
                            if (cltvKeywordsTree.SelectedNode.Text != fctbKeyword.Lines[0])
                                cltvKeywordsTree.SelectedNode.Text = fctbKeyword.Lines[0];
                        }
                        else
                            cltvKeywordsTree.SelectedNode.Text = "User keyword";
                        LockWindowUpdate(IntPtr.Zero);
                    }
                }
                //
                SelectKeywordLinesAndScrollToSelection();
                // Re-reference text changed event
                fctbKeyword.TextChanged += fctbKeyword_TextChanged;
            }
            catch
            { }
        }
        //
        private string ReplaceText(int firstLine, int numOfLines, string newText)
        {
            string oldText = null;
            //
            if (firstLine >= 0)
            {
                if (_adding) numOfLines = 0;
                //
                int lastLine = firstLine + numOfLines;
                fctbInpFile.Selection.Start = new Place(0, firstLine);
                fctbInpFile.Selection.End = new Place(0, lastLine);
                //
                oldText = "";
                for (int i = firstLine; i < firstLine + numOfLines; i++)
                {
                    if (i != firstLine) oldText += Environment.NewLine;
                    oldText += fctbInpFile.Selection.tb[i].Text;
                }
                //
                fctbInpFile.ClearSelected();
                //
                string text = newText;
                if (text != null) text += Environment.NewLine;
                fctbInpFile.InsertText(text);
            }
            //
            return oldText;
        }
        private void SelectKeywordLinesAndScrollToSelection()
        {
            if (_selectedKeywordFirstLine == -1) return;
            fctbInpFile.Navigate(_selectedKeywordFirstLine);
            fctbInpFile.Selection = new Range(fctbInpFile, 0, _selectedKeywordFirstLine, 
                                                           0, _selectedKeywordFirstLine + _selectedKeywordNumOfLines );
        }
        //
        private string GetFirstLineFromMultiline(string multiLineData)
        {
            string[] tmp = multiLineData.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length > 0) return tmp[0];
            else return "User keyword";
        }

        private void tUpdate_Tick(object sender, EventArgs e)
        {
            tUpdate.Stop();
            UpdateKeywordTextBox();
        }
    }
}


