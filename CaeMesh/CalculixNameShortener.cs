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
using System.Security.Cryptography;
using System.Text;

namespace CaeMesh
{
    public static class CalculixNameShortener
    {
        // Variables                                                                                                                
        // CalculiX reads a name of 80 characters, but prints it as an empty name in the .dat file and does not find it
        // in a *Contact print
        public const int MaxNameLength = 79;
        private const int HashLength = 8;
        private const string HashSeparator = "_";


        // Static methods                                                                                                           
        public static string GetShortenedName(string name)
        {
            return GetShortenedName(name, MaxNameLength);
        }
        public static string GetShortenedName(string name, int maxLength)
        {
            if (name == null || name.Length <= maxLength) return name;
            //
            string hashSuffix = GetHashSuffix(name);
            return name.Substring(0, maxLength - hashSuffix.Length) + hashSuffix;
        }
        public static bool IsNameCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_' || character == '-' || character == '(' ||
                   character == ')' || character == '@';
        }
        public static Dictionary<string, string> GetHashSuffixFullNames(IEnumerable<string> fullNames)
        {
            Dictionary<string, string> hashSuffixFullNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (fullNames == null) return hashSuffixFullNames;
            //
            string hashSuffix;
            foreach (string fullName in fullNames)
            {
                if (fullName == null || fullName.Length <= HashSeparator.Length + HashLength) continue;
                //
                hashSuffix = GetHashSuffix(fullName);
                if (!hashSuffixFullNames.ContainsKey(hashSuffix)) hashSuffixFullNames.Add(hashSuffix, fullName);
            }
            return hashSuffixFullNames;
        }
        public static string GetFullName(string name, Dictionary<string, string> hashSuffixFullNames)
        {
            int hashSuffixLength = HashSeparator.Length + HashLength;
            if (name == null || name.Length <= hashSuffixLength || hashSuffixFullNames == null) return name;
            //
            string fullName;
            string prefix = name.Substring(0, name.Length - hashSuffixLength);
            if (hashSuffixFullNames.TryGetValue(name.Substring(prefix.Length), out fullName) &&
                fullName.Length > name.Length && fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return fullName;
            return name;
        }
        private static string GetHashSuffix(string name)
        {
            byte[] hash;
            using (SHA1 sha1 = SHA1.Create())
            {
                hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(name.ToUpperInvariant()));
            }
            //
            StringBuilder sb = new StringBuilder(HashSeparator);
            for (int i = 0; i < HashLength / 2; i++) sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }
    }
}
