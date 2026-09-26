// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CaeMesh
{
    /// <summary>
    /// Writes and reads back the message that reports the inverted or degenerate elements of a mesh part -
    /// "Solid_part-1 has 12 inverted or degenerate elements" - so that the elements can be shown again from it.
    /// The message names the part only: the elements are found again when they are shown, so nothing about them
    /// is kept, and the message still finds them after the mesh has been regenerated.
    /// </summary>
    public static class MessageInvertedElements
    {
        // Variables
        // The pattern depends on the names of the parts only, and the output window asks for it on every move
        // of the mouse, so the last one is kept
        private static string _partNamesKey;
        private static Regex _reference;


        // Methods
        public static string GetMessage(string partName, int numOfElements)
        {
            return "Warning: " + partName + " has " + numOfElements + " inverted or degenerate element" +
                   (numOfElements == 1 ? "" : "s") + ".";
        }
        /// <summary>
        /// Every report of a part of the mesh in the message, as its start and its length.
        /// </summary>
        public static int[][] GetReferenceRanges(string message, FeMesh mesh)
        {
            Regex reference = GetReferenceRegex(message, mesh);
            if (reference == null) return null;
            //
            List<int[]> ranges = new List<int[]>();
            foreach (Match match in reference.Matches(message)) ranges.Add(new int[] { match.Index, match.Length });
            return ranges.Count > 0 ? ranges.ToArray() : null;
        }
        /// <summary>
        /// The name of the part whose report covers the given position of the message, or null when no report of a
        /// part of the mesh covers it.
        /// </summary>
        public static string GetPartNameAt(string message, int position, FeMesh mesh)
        {
            Regex reference = GetReferenceRegex(message, mesh);
            if (reference == null || position < 0 || position >= message.Length) return null;
            //
            foreach (Match match in reference.Matches(message))
            {
                if (position < match.Index) break;
                if (position < match.Index + match.Length) return match.Groups[1].Value;
            }
            return null;
        }
        private static Regex GetReferenceRegex(string message, FeMesh mesh)
        {
            if (string.IsNullOrEmpty(message) || mesh == null || mesh.Parts == null || mesh.Parts.Count == 0)
                return null;
            // The longest name first, so that a name is not matched by another it ends with
            string[] partNames = mesh.Parts.Keys.OrderByDescending(name => name.Length).ToArray();
            string names = string.Join("|", partNames.Select(name => Regex.Escape(name)));
            //
            Regex reference = _reference;
            if (reference == null || _partNamesKey != names)
            {
                reference = new Regex(@"(?<![A-Za-z0-9_\-])(" + names + @") has \d+ inverted or degenerate elements?");
                _reference = reference;
                _partNamesKey = names;
            }
            return reference;
        }
    }
}
