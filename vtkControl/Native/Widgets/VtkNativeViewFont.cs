// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using Microsoft.Win32;

namespace vtkControl.Native
{
    // The font of the text of the views: a Windows font the views draw from its files and the forms by its name, so
    // that both show the same glyphs at the same size
    public static class VtkNativeViewFont
    {
        // Variables
        public const string PreferredFamilyName = "Segoe UI Semilight";
        public const string SystemFamilyName = "Segoe UI";
        public const string FallbackFamilyName = "Arial";
        public const int DefaultSize = 16;
        public const int MinimumSize = 8;
        public const int MaximumSize = 48;
        public const double LineSpacing = 1.5;
        private const string FontsKeyName = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";
        private static readonly string[][] StyleNames = new string[][]
        {
            new string[] { "", " Regular" }, new string[] { " Bold" }, new string[] { " Italic" },
            new string[] { " Bold Italic" }
        };
        private static readonly string[] FormatNames = new string[] { " (TrueType)", " (OpenType)" };
        private static readonly string[] FileExtensions = new string[] { ".ttf", ".otf" };
        private const int LetterCharacter = 'H';
        private static readonly string[] BitmapTableTags = new string[] { "EBLC", "EBDT", "bloc", "bdat", "CBLC", "CBDT" };
        private static readonly Dictionary<string, bool> DrawableFiles =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static string _familyName = FallbackFamilyName;


        // Properties
        public static string FamilyName
        {
            get
            {
                VtkNativeLibrary.EnsureLoaded();
                return _familyName;
            }
        }


        // Methods
        public static int ClampSize(int size)
        {
            return Math.Max(MinimumSize, Math.Min(MaximumSize, size));
        }
        // The distance in pixels between the baselines of two lines
        public static double GetLinePitch(int size)
        {
            VtkNativeLibrary.EnsureLoaded();
            VtkNativeTextStyle style = VtkNativeTextStyle.CreateWidgetStyle(size);
            double linePitch;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_text_style_get_line_pitch(ref style, out linePitch));
            return linePitch;
        }
        // The families both the views and the forms can draw: installed under the name the forms know and with a
        // font file of their regular style the views can read
        public static string[] GetFamilyNames()
        {
            Dictionary<string, string> registeredFonts = GetRegisteredFonts();
            List<string> familyNames = new List<string>();
            using (InstalledFontCollection installedFonts = new InstalledFontCollection())
            {
                foreach (FontFamily family in installedFonts.Families)
                {
                    if (FindFontFiles(family.Name, registeredFonts)[0] != null) familyNames.Add(family.Name);
                }
            }
            familyNames.Sort(StringComparer.OrdinalIgnoreCase);
            return familyNames.ToArray();
        }
        // The family the views draw with from now on: the asked one, or the preferred or the fallback family while it
        // cannot be found; the text drawn before keeps its font until it is styled again
        public static string SetFamily(string familyName)
        {
            VtkNativeLibrary.EnsureLoaded();
            if (string.Equals(familyName, _familyName, StringComparison.OrdinalIgnoreCase)) return _familyName;
            Install(familyName);
            return _familyName;
        }
        internal static void Install()
        {
            Install(PreferredFamilyName);
        }
        private static void Install(string familyName)
        {
            Dictionary<string, string> registeredFonts = GetRegisteredFonts();
            foreach (string name in new string[] { familyName, PreferredFamilyName, SystemFamilyName,
                                                         FallbackFamilyName })
            {
                if (string.IsNullOrEmpty(name)) continue;
                string[] files = FindFontFiles(name, registeredFonts);
                if (files[0] == null) continue;
                VtkNativeStatus status = VtkNativeMethods.vtkn_set_font_files(VtkNativeLibrary.ToUtf8(files[0]),
                                                                             VtkNativeLibrary.ToUtf8(files[1]),
                                                                             VtkNativeLibrary.ToUtf8(files[2]),
                                                                             VtkNativeLibrary.ToUtf8(files[3]));
                if (status == VtkNativeStatus.Ok && DrawsEverySize())
                {
                    _familyName = name;
                    return;
                }
                lock (DrawableFiles)
                {
                    DrawableFiles[files[0]] = false;
                }
            }
        }
        // The text of the views is measured by drawing it, which fails for a size the font draws nothing at
        private static bool DrawsEverySize()
        {
            for (int size = MinimumSize; size <= MaximumSize; size++)
            {
                VtkNativeTextStyle style = VtkNativeTextStyle.CreateWidgetStyle(size);
                double linePitch;
                if (VtkNativeMethods.vtkn_text_style_get_line_pitch(ref style, out linePitch) != VtkNativeStatus.Ok)
                    return false;
            }
            return true;
        }
        // The font files of the registry by their registered names, the per user fonts before the machine fonts
        private static Dictionary<string, string> GetRegisteredFonts()
        {
            Dictionary<string, string> registeredFonts =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            foreach (RegistryKey root in new RegistryKey[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                using (RegistryKey key = root.OpenSubKey(FontsKeyName))
                {
                    if (key == null) continue;
                    foreach (string name in key.GetValueNames())
                    {
                        string fileName = key.GetValue(name) as string;
                        if (string.IsNullOrEmpty(fileName) || registeredFonts.ContainsKey(name)) continue;
                        registeredFonts.Add(name, Path.IsPathRooted(fileName) ? fileName
                                                                              : Path.Combine(fontsFolder, fileName));
                    }
                }
            }
            return registeredFonts;
        }
        // The files of the regular, bold, italic and bold italic style; a font collection holds more than one face
        // and the views read only the first, so only single font files count
        private static string[] FindFontFiles(string familyName, Dictionary<string, string> registeredFonts)
        {
            string[] files = new string[StyleNames.Length];
            for (int i = 0; i < StyleNames.Length; i++)
            {
                foreach (string styleName in StyleNames[i])
                {
                    foreach (string formatName in FormatNames)
                    {
                        string path;
                        if (files[i] != null ||
                            !registeredFonts.TryGetValue(familyName + styleName + formatName, out path)) continue;
                        if (Array.IndexOf(FileExtensions, Path.GetExtension(path).ToLowerInvariant()) >= 0 &&
                            File.Exists(path) && IsDrawable(path)) files[i] = path;
                    }
                }
            }
            return files;
        }
        // The views draw the outlines of the letters: a font that maps no Unicode letters draws none, and one with
        // bitmaps of its glyphs draws nothing at their sizes
        private static bool IsDrawable(string path)
        {
            bool drawable;
            lock (DrawableFiles)
            {
                if (DrawableFiles.TryGetValue(path, out drawable)) return drawable;
            }
            try
            {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
                {
                    reader.BaseStream.Position = 4;
                    int tableCount = ReadUInt16(reader);
                    bool hasBitmaps = false;
                    long cmapOffset = -1;
                    for (int i = 0; i < tableCount; i++)
                    {
                        reader.BaseStream.Position = 12 + 16 * i;
                        string tag = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
                        reader.BaseStream.Position += 4;
                        long offset = ReadUInt32(reader);
                        if (Array.IndexOf(BitmapTableTags, tag) >= 0) hasBitmaps = true;
                        else if (tag == "cmap") cmapOffset = offset;
                    }
                    drawable = !hasBitmaps && cmapOffset >= 0 && HasUnicodeMap(reader, cmapOffset);
                }
            }
            catch (Exception)
            {
                drawable = false;
            }
            lock (DrawableFiles)
            {
                DrawableFiles[path] = drawable;
            }
            return drawable;
        }
        private static bool HasUnicodeMap(BinaryReader reader, long cmapOffset)
        {
            reader.BaseStream.Position = cmapOffset + 2;
            int subtableCount = ReadUInt16(reader);
            for (int i = 0; i < subtableCount; i++)
            {
                reader.BaseStream.Position = cmapOffset + 4 + 8 * i;
                int platformId = ReadUInt16(reader);
                int encodingId = ReadUInt16(reader);
                long subtableOffset = cmapOffset + ReadUInt32(reader);
                if ((platformId == 0 || (platformId == 3 && (encodingId == 1 || encodingId == 10))) &&
                    MapsCharacter(reader, subtableOffset, LetterCharacter)) return true;
            }
            return false;
        }
        private static bool MapsCharacter(BinaryReader reader, long subtableOffset, int character)
        {
            reader.BaseStream.Position = subtableOffset;
            int format = ReadUInt16(reader);
            if (format == 4)
            {
                reader.BaseStream.Position = subtableOffset + 6;
                int segmentCount = ReadUInt16(reader) / 2;
                long endCodes = subtableOffset + 14;
                long startCodes = endCodes + 2 * segmentCount + 2;
                long idDeltas = startCodes + 2 * segmentCount;
                long idRangeOffsets = idDeltas + 2 * segmentCount;
                for (int i = 0; i < segmentCount; i++)
                {
                    reader.BaseStream.Position = endCodes + 2 * i;
                    if (ReadUInt16(reader) < character) continue;
                    reader.BaseStream.Position = startCodes + 2 * i;
                    int startCode = ReadUInt16(reader);
                    if (startCode > character) return false;
                    reader.BaseStream.Position = idDeltas + 2 * i;
                    int idDelta = ReadUInt16(reader);
                    reader.BaseStream.Position = idRangeOffsets + 2 * i;
                    int idRangeOffset = ReadUInt16(reader);
                    if (idRangeOffset == 0) return ((character + idDelta) & 0xFFFF) != 0;
                    reader.BaseStream.Position = idRangeOffsets + 2 * i + idRangeOffset + 2 * (character - startCode);
                    int glyph = ReadUInt16(reader);
                    return glyph != 0 && ((glyph + idDelta) & 0xFFFF) != 0;
                }
            }
            else if (format == 12)
            {
                reader.BaseStream.Position = subtableOffset + 12;
                long groupCount = ReadUInt32(reader);
                for (long i = 0; i < groupCount; i++)
                {
                    long startCharacter = ReadUInt32(reader);
                    long endCharacter = ReadUInt32(reader);
                    long startGlyph = ReadUInt32(reader);
                    if (startCharacter <= character && character <= endCharacter)
                        return startGlyph + character - startCharacter != 0;
                }
            }
            return false;
        }
        private static int ReadUInt16(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            return (bytes[0] << 8) | bytes[1];
        }
        private static long ReadUInt32(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            return ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
        }
    }
}
