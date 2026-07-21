// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
using System.IO;
using System.Collections.ObjectModel;
using System.Reflection;
using UserControls;

namespace PrePoMax.Forms
{
    
    public partial class FrmCalculixKeywordEditor : PrePoMaxChildForm
    {
        // dll routines                                                                                                             
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);


        // Variables                                                                                                                
        private List<CalculixKeyword> _keywords;
        private OrderedDictionary<int[], CalculixUserKeyword> _userKeywords;
        private List<CalculixKeywordOverride> _keywordOverrides;
        private const string HiddenDataPlaceholder = "... hidden data ...";
        private const string DotsImageKey = "Dots.ico";
        private const string DotsTransparentImageKey = "Dots_t.ico";
        private const string AddedKeywordImageKey = "Keyword_added.ico";
        private const string ModifiedKeywordImageKey = "Keyword_modified.ico";
        private Font _emphasizedKeywordFont;
        private int _selectedKeywordFirstLine;
        private int _selectedKeywordNumOfLines;
        private bool _adding;

        // Properties                                                                                                               
        public List<CalculixKeyword> Keywords { get { return _keywords; } set { _keywords = value; } }
        public OrderedDictionary<int[], CalculixUserKeyword> UserKeywords
        {
            get { return _userKeywords; }
            set { _userKeywords = value; }
        }
        public List<CalculixKeywordOverride> KeywordOverrides
        {
            get { return _keywordOverrides; }
            set
            {
                _keywordOverrides = new List<CalculixKeywordOverride>();
                if (value != null)
                {
                    foreach (CalculixKeywordOverride keywordOverride in value)
                    {
                        if (keywordOverride == null) continue;
                        CalculixKeywordOverride copy =
                            new CalculixKeywordOverride(keywordOverride.Indices, keywordOverride.OriginalSignature);
                        copy.Data = keywordOverride.Data;
                        copy.Disabled = keywordOverride.Disabled;
                        if (keywordOverride.IsSuppressed) copy.Suppress();
                        _keywordOverrides.Add(copy);
                    }
                }
            }
        }
        // Constructors                                                                                                             
        public FrmCalculixKeywordEditor()
        {
            InitializeComponent();
            cltvKeywordsTree.EnsureSelectedNodeVisible = false;
            _emphasizedKeywordFont = new Font(cltvKeywordsTree.Font,
                                               cltvKeywordsTree.Font.Style | FontStyle.Bold);
            InitializeKeywordTreeImages();
            _keywordOverrides = new List<CalculixKeywordOverride>();
            //
            citbKeyword.TextChanged += ictbKeyword_TextChanged;
            cltvKeywordsTree.AfterCollapse += cltvKeywordsTree_AfterCollapse;
            cltvKeywordsTree.AfterExpand += cltvKeywordsTree_AfterExpand;
        }


        // Event handlers                                                                                                           
        private void cltvKeywordsTree_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            UpdateKeywordNodeIcon(e.Node);
        }
        private void cltvKeywordsTree_AfterExpand(object sender, TreeViewEventArgs e)
        {
            UpdateKeywordNodeIcon(e.Node);
        }
        private void cltvKeywordsTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            btnMoveUp.Enabled = false;
            btnMoveDown.Enabled = false;
            btnDelete.Enabled = false;
            if (cltvKeywordsTree.SelectedNode != null)
            {
                bool userNode = false;
                bool generatedNode = false;
                bool titleNode = false;
                //
                if (cltvKeywordsTree.SelectedNode.Tag is KeywordAtNode kn1)
                {
                    userNode = kn1.Keyword is CalculixUserKeyword;
                    generatedNode = kn1.GeneratedIndices != null;
                    titleNode = kn1.Keyword is CalTitle;
                }
                btnMoveUp.Enabled = userNode && CanMoveUserKeyword(cltvKeywordsTree.SelectedNode, true);
                btnMoveDown.Enabled = userNode && CanMoveUserKeyword(cltvKeywordsTree.SelectedNode, false);
                btnDelete.Enabled = userNode;
                // De-reference text changed event
                citbKeyword.TextChanged -= ictbKeyword_TextChanged;
                // Clear keyword textbox
                citbKeyword.Clear();
                // Add keyword data to keyword textbox
                citbKeyword.Tag = cltvKeywordsTree.SelectedNode.Tag;
                if (citbKeyword.Tag != null && citbKeyword.Tag is KeywordAtNode kn2)
                {
                    citbKeyword.Text = kn2.Data;
                    bool hiddenData = kn2.Data.Contains(HiddenDataPlaceholder);
                    citbKeyword.ReadOnly = titleNode || !(userNode || generatedNode) || hiddenData;
                    // Activate/Deactivate
                    cbActive.CheckedChanged -= cbActive_CheckedChanged;
                    if (kn2.Keyword is CalculixUserKeyword ck)
                    {
                        cbActive.Checked = ck.Active;
                        cbActive.Enabled = true;
                    }
                    else if (generatedNode && !titleNode)
                    {
                        cbActive.Checked = kn2.KeywordOverride == null || !kn2.KeywordOverride.Disabled;
                        cbActive.Enabled = true;
                        if (!cbActive.Checked) citbKeyword.ReadOnly = true;
                    }
                    else
                    {
                        cbActive.Checked = true;
                        cbActive.Enabled = false;
                    }
                    cbActive.CheckedChanged += cbActive_CheckedChanged;
                    btnReset.Enabled = generatedNode && kn2.KeywordOverride != null;
                    //
                    _selectedKeywordNumOfLines = kn2.NumOfLines;
                }
                else
                {
                    _selectedKeywordFirstLine = 0;
                    _selectedKeywordNumOfLines = 0;
                    cbActive.Enabled = false;
                    btnReset.Enabled = false;
                }
                // Find the first line of the selected keyword
                bool nodeFound = false;
                _selectedKeywordFirstLine =
                    GetFirstLineOfSelectedNode(cltvKeywordsTree.Nodes[0], cltvKeywordsTree.SelectedNode, ref nodeFound);
                // Re-reference text changed event
                citbKeyword.TextChanged += ictbKeyword_TextChanged;
                //
                UpdateKeywordTextBox();
            }
        }
        //
        private void ictbKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Control && e.KeyCode == Keys.V)
            //{
            //    ((RichTextBox)sender).Paste(DataFormats.GetFormat("Text"));
            //    e.Handled = true;
            //}
        }
        private void ictbKeyword_TextChanged(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.Millisecond.ToString() + " ms");
            if (citbKeyword.Tag is KeywordAtNode kn && kn.GeneratedIndices != null)
            {
                UpdateGeneratedKeywordOverride(kn);
                kn.Data = citbKeyword.Text;
                kn.NumOfLines = citbKeyword.LineCount;
                UpdateKeywordNodeIcon(cltvKeywordsTree.SelectedNode);
            }
            tUpdate.Stop();
            tUpdate.Start();
            tUpdate.Interval = 500;
        }

        private void btnAddKeyword_Click(object sender, EventArgs e)
        {
            if (cltvKeywordsTree.SelectedNode != null)
            {
                if (cltvKeywordsTree.SelectedNode.Tag is KeywordAtNode kn && kn.Keyword is CalDeactivated)
                {
                    MessageBoxes.ShowWarning("Cannot add user keyword to a deactivated keyword.");
                }
                else
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
        }
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveUserKeyword(true);
        }
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveUserKeyword(false);
        }
        private bool CanMoveUserKeyword(TreeNode node, bool moveUp)
        {
            if (!(node?.Tag is KeywordAtNode kn) || !(kn.Keyword is CalculixUserKeyword) || node.Parent == null)
                return false;
            //
            if (moveUp && node.Index > 0) return true;
            if (!moveUp && node.Index < node.Parent.Nodes.Count - 1) return true;
            // At a child boundary the keyword can move one level out, except from the editor root.
            return node.Parent.Parent != null;
        }
        private bool CanMoveUserKeywordInto(TreeNode node)
        {
            if (node == null || node.Nodes.Count == 0) return false;
            return !(node.Tag is KeywordAtNode kn) || !(kn.Keyword is CalDeactivated);
        }
        private void MoveUserKeyword(bool moveUp)
        {
            TreeNode node = cltvKeywordsTree.SelectedNode;
            if (!CanMoveUserKeyword(node, moveUp)) return;
            //
            tUpdate.Stop();
            UpdateKeywordTextBox();
            TreeNode oldParent = node.Parent;
            TreeNode newParent = oldParent;
            int oldIndex = node.Index;
            int newIndex;
            if (moveUp)
            {
                if (oldIndex == 0)
                {
                    // Leave the current parent and take the position immediately before it.
                    newParent = oldParent.Parent;
                    newIndex = oldParent.Index;
                }
                else
                {
                    TreeNode previousNode = oldParent.Nodes[oldIndex - 1];
                    if (CanMoveUserKeywordInto(previousNode))
                    {
                        // Enter the previous parent after its existing child subtrees.
                        newParent = previousNode;
                        newIndex = previousNode.Nodes.Count;
                    }
                    else newIndex = oldIndex - 1;
                }
            }
            else
            {
                if (oldIndex == oldParent.Nodes.Count - 1)
                {
                    // Leave the current parent and take the position immediately after it.
                    newParent = oldParent.Parent;
                    newIndex = oldParent.Index + 1;
                }
                else
                {
                    TreeNode nextNode = oldParent.Nodes[oldIndex + 1];
                    if (CanMoveUserKeywordInto(nextNode))
                    {
                        // Enter the next parent before its existing child subtrees.
                        newParent = nextNode;
                        newIndex = 0;
                    }
                    else newIndex = oldIndex + 1;
                }
            }
            //
            LockWindowUpdate(cltvKeywordsTree.Handle);
            try
            {
                oldParent.Nodes.Remove(node);
                newParent.Nodes.Insert(newIndex, node);
                newParent.Expand();
                UpdateKeywordNodeIcon(oldParent);
                if (!ReferenceEquals(oldParent, newParent)) UpdateKeywordNodeIcon(newParent);
            }
            finally
            {
                LockWindowUpdate(IntPtr.Zero);
            }
            // Recreate the output because the moved keyword may now belong to a different subtree.
            WriteTreeToTextBox();
            cltvKeywordsTree.SelectedNode = node;
            cltvKeywordsTree_AfterSelect(null, null);
            cltvKeywordsTree.Focus();
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (cltvKeywordsTree.SelectedNode != null &&
                    cltvKeywordsTree.SelectedNode.Tag is KeywordAtNode kn &&
                    kn.Keyword is CalculixUserKeyword)
                    DeleteKeywordByTreeNode(cltvKeywordsTree.SelectedNode);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void cbActive_CheckedChanged(object sender, EventArgs e)
        {
            // De-reference text changed event
            citbKeyword.TextChanged -= ictbKeyword_TextChanged;
            string data = "";
            if (cbActive.Checked)
                data = citbKeyword.GetActivatedText();
            else
                data = citbKeyword.GetDeactivatedText();
            
            //
            citbKeyword.Text = data;
            // Re-reference text changed event
            citbKeyword.TextChanged += ictbKeyword_TextChanged;
            //
            UpdateKeywordTextBox();
        }
        private void cbHide_CheckedChanged(object sender, EventArgs e)
        {
            tUpdate.Stop();
            UpdateKeywordTextBox();
            UpdateAllKeywords();
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            tUpdate.Stop();
            if (cltvKeywordsTree.SelectedNode != null &&
                cltvKeywordsTree.SelectedNode.Tag is KeywordAtNode kn && kn.GeneratedIndices != null)
            {
                if (kn.KeywordOverride != null) _keywordOverrides.Remove(kn.KeywordOverride);
                kn.KeywordOverride = null;
                UpdateKeywordNodeIcon(cltvKeywordsTree.SelectedNode);
                UpdateKeywordDataInTreeNode(cltvKeywordsTree.SelectedNode);
                WriteTreeToTextBox();
                cltvKeywordsTree_AfterSelect(null, null);
                cltvKeywordsTree.Focus();
            }
        }
        //
        private void btnOK_Click(object sender, EventArgs e)
        {
            tUpdate.Stop();
            UpdateKeywordTextBox();
            if (_keywordOverrides.Any(item => item != null && !item.IsSuppressed) &&
                MessageBoxes.ShowWarningQuestionYesNo(
                    "Modified CalculiX keywords are output-level overrides. " +
                    "Later GUI model changes may reset affected modifications. Continue?") != DialogResult.Yes)
            {
                DialogResult = DialogResult.None;
                return;
            }
            var newUserKeywords = new OrderedDictionary<int[], CalculixUserKeyword>("User CalculiX keywords");
            // First add suppressed keywords
            if (_userKeywords != null && _userKeywords.Count > 0)
            {
                foreach (var entry in _userKeywords)
                {
                    if (entry.Value.IsSuppressed) newUserKeywords.Add(entry.Key, entry.Value);
                }
            }
            // Add keywords from the tree - except suppressed keywords which are not visible
            FindUserKeywords(cltvKeywordsTree.Nodes[0], ref newUserKeywords);
            //
            _userKeywords = newUserKeywords;
            //
            this.DialogResult = DialogResult.OK;
        }
        private void FindUserKeywords(TreeNode node, ref OrderedDictionary<int[], CalculixUserKeyword> userKeywords)
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
                FindUserKeywords(childNode, ref userKeywords);
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
            btnMoveUp.Enabled = false;
            btnMoveDown.Enabled = false;
            btnDelete.Enabled = false;
            citbKeyword.Clear();
            citbInpFile.Clear();
            //
            TreeNode node = new TreeNode();
            node.Text = "CalculiX inp file";
            // Build the keyword tree
            for (int i = 0; i < _keywords.Count; i++)
            {
                int index = node.Nodes.Add(new TreeNode());
                AddKeywordToTreeNode(_keywords[i], node.Nodes[index], new int[] { i });
            }
            // Add user keywords
            if (_userKeywords != null)
            {
                foreach (var entry in _userKeywords)
                {
                    AddUserKeywordToTreeByIndex(node, entry.Key, entry.Value.DeepClone());
                }
            }
            // At the end add nodes to the tree
            cltvKeywordsTree.Nodes.Add(node);
            // Make changed generated keywords visible
            ExpandKeywordOverrideParentNodes(node);
            // Clear the keyword editor
            citbKeyword.Clear();
            // Output tree to the inp read-only textbox
            WriteTreeToTextBox();
            // Expand first tree node
            node.Expand();
            UpdateKeywordNodeIcons(node);
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
        private void UpdateKeywordNodeIcons(TreeNode node)
        {
            UpdateKeywordNodeIcon(node);
            foreach (TreeNode childNode in node.Nodes) UpdateKeywordNodeIcons(childNode);
        }
        private void UpdateKeywordNodeIcon(TreeNode node)
        {
            string imageKey;
            bool emphasize = false;
            if (node.Tag is KeywordAtNode kn && kn.Keyword is CalculixUserKeyword)
            {
                imageKey = AddedKeywordImageKey;
                emphasize = true;
            }
            else if (node.Tag is KeywordAtNode generatedKn && generatedKn.GeneratedIndices != null &&
                     generatedKn.KeywordOverride != null)
            {
                imageKey = ModifiedKeywordImageKey;
                emphasize = true;
            }
            else if (node.Nodes.Count > 0 && node.IsExpanded)
                imageKey = DotsTransparentImageKey;
            else imageKey = DotsImageKey;
            //
            if (node.ImageKey != imageKey)
            {
                node.ImageKey = imageKey;
                node.SelectedImageKey = imageKey;
            }
            Font nodeFont = emphasize ? _emphasizedKeywordFont : null;
            if (!ReferenceEquals(node.NodeFont, nodeFont)) node.NodeFont = nodeFont;
            Color foreColor = emphasize ? SystemColors.Highlight : cltvKeywordsTree.ForeColor;
            if (node.ForeColor != foreColor) cltvKeywordsTree.SetNodeForeColor(node, foreColor);
        }
        private void ExpandKeywordOverrideParentNodes(TreeNode node)
        {
            if (node.Tag is KeywordAtNode kn && kn.GeneratedIndices != null && kn.KeywordOverride != null)
            {
                TreeNode parentNode = node.Parent;
                while (parentNode != null)
                {
                    parentNode.Expand();
                    parentNode = parentNode.Parent;
                }
            }
            //
            foreach (TreeNode childNode in node.Nodes) ExpandKeywordOverrideParentNodes(childNode);
        }
        // Add keywords to tree
        private void AddKeywordToTreeNode(CalculixKeyword keyword, TreeNode node, int[] generatedIndices)
        {
            KeywordAtNode keywordAtNode = new KeywordAtNode();
            keywordAtNode.Keyword = keyword;
            keywordAtNode.GeneratedIndices = (int[])generatedIndices.Clone();
            keywordAtNode.OriginalSignature =
                FileInOut.Output.CalculixFileWriter.GetKeywordSignature(keyword);
            keywordAtNode.KeywordOverride =
                FindKeywordOverride(keywordAtNode.GeneratedIndices, keywordAtNode.OriginalSignature);
            keywordAtNode.Data = GetKeywordData(keywordAtNode);
            keywordAtNode.NumOfLines =
                keywordAtNode.Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
            node.Tag = keywordAtNode;
            UpdateKeywordNodeIcon(node);
            //
            string nodeText;
            if (keywordAtNode.KeywordOverride == null && keyword is CalTitle ct) nodeText = ct.Title;
            else nodeText = keywordAtNode.Data;
            node.Text = GetFirstLineFromMultiline(nodeText);
            node.Name = node.Text;
            //
            int count = 0;
            foreach (var childKeyword in keyword.Keywords)
            {
                TreeNode childNode = new TreeNode();
                node.Nodes.Add(childNode);
                int[] childIndices = new int[generatedIndices.Length + 1];
                Array.Copy(generatedIndices, childIndices, generatedIndices.Length);
                childIndices[childIndices.Length - 1] = count;
                AddKeywordToTreeNode(childKeyword, childNode, childIndices);
                count++;
            }
        }
        private void AddUserKeywordToTreeByIndex(TreeNode node, int[] indices, CalculixUserKeyword keyword)
        {
            if (keyword.IsSuppressed) return;
            //
            for (int i = 0; i < indices.Length - 1; i++)
            {
                node.Expand();
                if (indices[i] < node.Nodes.Count) node = node.Nodes[indices[i]];
                else return;
            }
            //
            TreeNode child = node.Nodes.Insert(indices[indices.Length - 1], "");
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
            UpdateKeywordNodeIcon(node);
        }
        private void InitializeKeywordTreeImages()
        {
            ImageList keywordImageList = new ImageList(components);
            keywordImageList.ColorDepth = ColorDepth.Depth32Bit;
            keywordImageList.ImageSize = new Size(16, 16);
            keywordImageList.TransparentColor = Color.Transparent;
            keywordImageList.Images.Add(DotsImageKey, Properties.Resources.Dots);
            keywordImageList.Images.Add(DotsTransparentImageKey, Properties.Resources.Dots_t);
            keywordImageList.Images.Add(AddedKeywordImageKey, Properties.Resources.Keyword_added);
            keywordImageList.Images.Add(ModifiedKeywordImageKey, Properties.Resources.Keyword_modified);
            cltvKeywordsTree.ImageList = keywordImageList;
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
                kn.Data = GetKeywordData(kn);
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
                citbInpFile.Text = sw.ToString();
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
                citbKeyword.ApplyCalculixStyles();

                // De-reference text changed event
                citbKeyword.TextChanged -= ictbKeyword_TextChanged;
                // rtbKeyword.Tag is set by clicking on the tree node
                if (citbKeyword.Tag != null && citbKeyword.Tag is KeywordAtNode kn &&
                    cltvKeywordsTree.SelectedNode != null)
                {
                    if (kn.Keyword is CalculixUserKeyword ck && ck != null)
                    {
                        ck.Data = citbKeyword.GetTextRows();
                        kn.Data = citbKeyword.Text;
                        kn.NumOfLines = citbKeyword.LineCount;
                        //
                        ReplaceText(_selectedKeywordFirstLine, _selectedKeywordNumOfLines, ck.Data);
                        _selectedKeywordNumOfLines = citbKeyword.LineCount;
                        // Change the name of the Selected tree node
                        LockWindowUpdate(cltvKeywordsTree.Handle);
                        if (citbKeyword.LineCount > 0 && citbKeyword.GetLine(0).Length > 0)
                        {
                            if (cltvKeywordsTree.SelectedNode.Text != citbKeyword.GetLine(0))
                                cltvKeywordsTree.SelectedNode.Text = citbKeyword.GetLine(0);
                        }
                        else
                            cltvKeywordsTree.SelectedNode.Text = "User keyword";
                        LockWindowUpdate(IntPtr.Zero);
                        // Enabled/disabled keyword
                        ck.Active = cbActive.Checked;
                        citbKeyword.ReadOnly = !ck.Active;
                    }
                    else if (kn.Keyword is CalTitle)
                    {
                        citbKeyword.ReadOnly = true;
                    }
                    else if (kn.GeneratedIndices != null)
                    {
                        UpdateGeneratedKeywordOverride(kn);
                        kn.Data = citbKeyword.Text;
                        kn.NumOfLines = citbKeyword.LineCount;
                        //
                        ReplaceText(_selectedKeywordFirstLine, _selectedKeywordNumOfLines, kn.Data);
                        _selectedKeywordNumOfLines = citbKeyword.LineCount;
                        // Change the name of the selected tree node
                        string nodeText;
                        if (kn.KeywordOverride == null && kn.Keyword is CalTitle ct) nodeText = ct.Title;
                        else if (citbKeyword.LineCount > 0 && citbKeyword.GetLine(0).Length > 0)
                            nodeText = citbKeyword.GetLine(0);
                        else nodeText = "Empty keyword fragment";
                        LockWindowUpdate(cltvKeywordsTree.Handle);
                        if (cltvKeywordsTree.SelectedNode.Text != nodeText)
                            cltvKeywordsTree.SelectedNode.Text = nodeText;
                        LockWindowUpdate(IntPtr.Zero);
                        // Enabled/disabled generated fragment
                        btnReset.Enabled = kn.KeywordOverride != null;
                        UpdateKeywordNodeIcon(cltvKeywordsTree.SelectedNode);
                        citbKeyword.ReadOnly = !cbActive.Checked || kn.Data.Contains(HiddenDataPlaceholder);
                    }
                }
                //
                SelectKeywordLinesAndScrollToSelection();
                // Re-reference text changed event
                citbKeyword.TextChanged += ictbKeyword_TextChanged;
                }
            catch
            { }
        }
        private void UpdateGeneratedKeywordOverride(KeywordAtNode kn)
        {
            bool disabled = !cbActive.Checked;
            bool hiddenData = citbKeyword.Text.Contains(HiddenDataPlaceholder);
            if (hiddenData)
            {
                if (disabled)
                {
                    GetOrCreateKeywordOverride(kn).Disabled = true;
                }
                else if (kn.KeywordOverride != null)
                {
                    kn.KeywordOverride.Disabled = false;
                    if (kn.KeywordOverride.Data == null) RemoveKeywordOverride(kn);
                }
                return;
            }
            //
            string data = disabled ? citbKeyword.GetActivatedText() : citbKeyword.GetTextRows();
            string originalData =
                FileInOut.Output.CalculixFileWriter.GetShortKeywordData(kn.Keyword, false);
            string replacementData = KeywordDataEquals(data, originalData) ? null : data;
            //
            if (replacementData != null || disabled)
            {
                CalculixKeywordOverride keywordOverride = GetOrCreateKeywordOverride(kn);
                keywordOverride.Data = replacementData;
                keywordOverride.Disabled = disabled;
            }
            else if (kn.KeywordOverride != null) RemoveKeywordOverride(kn);
        }
        private CalculixKeywordOverride FindKeywordOverride(int[] indices, string originalSignature)
        {
            CalculixKeywordOverride keywordOverride = _keywordOverrides.FirstOrDefault(
                item => item != null && item.HasSameIndices(indices) &&
                        item.OriginalSignature == originalSignature);
            if (keywordOverride != null) keywordOverride.Unsuppress();
            return keywordOverride;
        }
        private CalculixKeywordOverride GetOrCreateKeywordOverride(KeywordAtNode kn)
        {
            if (kn.KeywordOverride != null) return kn.KeywordOverride;
            _keywordOverrides.RemoveAll(
                item => item != null && item.HasSameIndices(kn.GeneratedIndices));
            CalculixKeywordOverride keywordOverride =
                new CalculixKeywordOverride(kn.GeneratedIndices, kn.OriginalSignature);
            _keywordOverrides.Add(keywordOverride);
            kn.KeywordOverride = keywordOverride;
            return keywordOverride;
        }
        private void RemoveKeywordOverride(KeywordAtNode kn)
        {
            _keywordOverrides.Remove(kn.KeywordOverride);
            kn.KeywordOverride = null;
        }
        private string GetKeywordData(KeywordAtNode kn)
        {
            if (kn.GeneratedIndices != null)
            {
                string data = kn.KeywordOverride != null && kn.KeywordOverride.Data != null
                                  ? kn.KeywordOverride.Data
                                  : FileInOut.Output.CalculixFileWriter.GetShortKeywordData(kn.Keyword, cbHide.Checked);
                if (kn.KeywordOverride != null && kn.KeywordOverride.Disabled)
                    return GetDeactivatedKeywordData(data);
                return data;
            }
            return FileInOut.Output.CalculixFileWriter.GetShortKeywordData(kn.Keyword, cbHide.Checked);
        }
        private static bool KeywordDataEquals(string first, string second)
        {
            string normalizedFirst = (first ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            string normalizedSecond = (second ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            return normalizedFirst == normalizedSecond;
        }
        private static string GetDeactivatedKeywordData(string data)
        {
            if (string.IsNullOrEmpty(data)) return "** Disabled by CalculiX keyword editor";
            string normalizedData = data.Replace("\r\n", "\n").Replace("\r", "\n");
            if (normalizedData.EndsWith("\n")) normalizedData = normalizedData.Substring(0, normalizedData.Length - 1);
            string[] lines = normalizedData.Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append("**").Append(lines[i]);
            }
            return sb.ToString();
        }
        //
        private string ReplaceText(int firstLine, int numOfLines, string newText)
        {
            return citbInpFile.ReplaceLines(firstLine, numOfLines, newText, _adding);

        }
        private void SelectKeywordLinesAndScrollToSelection()
        {
            if (_selectedKeywordFirstLine == -1) return;
            citbInpFile.SelectLinesAndScrollToSelection(_selectedKeywordFirstLine, _selectedKeywordNumOfLines);

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
            WriteTreeToTextBox();
        }
    }
}
