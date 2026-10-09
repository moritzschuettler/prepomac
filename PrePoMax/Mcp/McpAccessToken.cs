// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Security.Cryptography;

namespace PrePoMax.Mcp
{
    // What a client sends to prove it is the client the user configured. A loopback port, unlike a
    // named pipe, is reachable by every program on the machine and by every account logged into it,
    // and the operating system authenticates none of them - so the endpoint authenticates them itself.
    //
    // The value is made here and kept in the settings, beside the executable, where the user can read
    // it back to configure a client and where deleting the portable folder deletes it too. It is only
    // ever generated: a token the user could type would be a token the user could make short, and
    // every comparable tool - Jupyter, Syncthing - generates rather than asks.
    //
    // What the token bounds is worth being exact about: it is stored in a file the user can read, so
    // any program running as this user can read it, exactly as any such program can open the pipe. It
    // stops the browser, which cannot read a file at all. It stops another account on the machine only
    // when that account cannot read the PrePoMax folder: the settings file is given no access rules of
    // its own and inherits those of the folder, so the token is private to the user only in a folder
    // only the user can read, such as one inside the user profile. A folder under C:\ is readable by
    // every local account by default, and there the token is not private. It is not a defence against
    // code already running as the user, and nothing served over loopback can be.
    public static class McpAccessToken
    {
        // Variables                                                                                                                
        private const int ByteCount = 32;


        // Static methods                                                                                                           
        public static string Generate()
        {
            byte[] bytes = new byte[ByteCount];
            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
            {
                generator.GetBytes(bytes);
            }
            // Base64url, so the value survives a header, a command line and a JSON string untouched
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
        // Compares in a time that does not depend on how much of the token is right, so a wrong one
        // leaks no prefix to a caller that can measure
        public static bool IsMatch(string expected, string offered)
        {
            if (string.IsNullOrEmpty(expected) || offered == null) return false;
            //
            int difference = expected.Length ^ offered.Length;
            for (int i = 0; i < expected.Length; i++)
            {
                difference |= expected[i] ^ (i < offered.Length ? offered[i] : (char)0);
            }
            return difference == 0;
        }
    }
}
