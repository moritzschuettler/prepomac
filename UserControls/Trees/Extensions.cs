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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public static class Extensions
    {
        public static TreeNode[] Find(this TreeNodeCollection nodes, string nodeName, bool searchAllChildren, bool caseSensitive)
        {
            TreeNode[] foundNodes = nodes.Find(nodeName, true);
            if (foundNodes == null) return null;
            //
            if (caseSensitive && foundNodes.Length > 0)
            {
                List<TreeNode> foundCSNodes = new List<TreeNode>();
                for (int i = 0; i < foundNodes.Length; i++)
                {
                    if (foundNodes[i].Name == nodeName) foundCSNodes.Add(foundNodes[i]);
                }
                foundNodes = foundCSNodes.ToArray();
            }
            //
            return foundNodes;
        }
    }
}
