using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Xml;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Data;

namespace CaeGlobals
{
    public static class ExtensionMethods
    {
        // Faster serialization
        // https://github.com/tomba/netserializer/blob/master/Doc.md
        //

        // Deep clone
        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
        // Save clone to File
        public static void DumpToFile<T>(this T a, string fileName)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                //
                FileStream fs = new FileStream(fileName, FileMode.Create);
                //
                stream.WriteTo(fs);
                fs.Close();
            }
        }
        public static void DumpToStream<T>(this T a, BinaryWriter bw)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;

                long length = stream.Length;
                bw.Write(length);
                //stream.WriteTo(bw.BaseStream);
                stream.CopyTo(bw.BaseStream);
            }
        }
        public static string SerializeToXML<T>(this T value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter))
                {
                    xmlSerializer.Serialize(writer, value);
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
        }
        public static T GetNewObject<T>()
        {
            try
            {
                return (T)typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
            }
            catch
            {
                return default;
            }
        }
        // Dictionary
        public static bool ContainsValidKey<T>(this IDictionary<string, T> dictionary, string key)
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
            {
                if (value is NamedClass) return (value as NamedClass).Valid;
                else return true;  // act as ordinary ContainsKey
            }
            else return false;
        }
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dic, Dictionary<TKey, TValue> dicToAdd)
        {
            foreach (var item in dicToAdd) dic.Add(item.Key, item.Value);
        }
        public static void AddUniqueItemsFromRange<TKey, TValue>(this Dictionary<TKey, TValue> dic, Dictionary<TKey, TValue> dicToAdd)
        {
            foreach (var item in dicToAdd)
            {
                if (!dic.ContainsKey(item.Key)) dic.Add(item.Key, item.Value);
            }
                
        }
        public static string GetNextNumberedKey<T>(this IDictionary<string, T> dictionary, string key,
                                                   string postFix = "", string separator = "-")
        {
            int n = 0;
            string newKey;
            while (true)
            {
                n++;
                newKey = key + separator + n + postFix;
                if (!dictionary.ContainsKey(newKey)) break;
            }
            return newKey;
        }
        public static string GetNextNumberedKey(this HashSet<string> hashSet, string key,
                                                string postFix = "", string separator = "-")
        {
            int n = 0;
            string newKey;
            while (true)
            {
                n++;
                newKey = key + separator + n + postFix;
                if (!hashSet.Contains(newKey)) break;
            }
            return newKey;
        }
        public static string GetNextNumberedKey(this List<string> list, string key, string postFix = "")
        {
            return new HashSet<string>(list).GetNextNumberedKey(key, postFix);
        }
        public static string GetNextNumberedKey(this string[] array, string key, string postFix = "")
        {
            return new HashSet<string>(array).GetNextNumberedKey(key, postFix);
        }
        public static int FindFreeIntervalOfKeys<T>(this Dictionary<int, T> dic, int numOfKeys, int maxKey)
        {
            int count = 0;
            int firstId = 1;                    // start at 1
            //
            for (int i = 1; i <= maxKey; i++)   // start at 1
            {
                if (dic.ContainsKey(i))
                {
                    count = 0;
                    firstId = i + 1;
                }
                else
                {
                    if (++count == numOfKeys) break;
                }
            }
            //
            return firstId;
        }
        // Property grid items
        public static IEnumerable<GridItem> EnumerateAllItems(this PropertyGrid grid)
        {
            if (grid == null) yield break;
            // Get to root item
            GridItem start = grid.SelectedGridItem;
            while (start.Parent != null)
            {
                start = start.Parent;
            }
            //
            foreach (GridItem item in start.EnumerateAllItems())
            {
                yield return item;
            }
        }
        public static IEnumerable<GridItem> EnumerateAllItems(this GridItem item)
        {
            if (item == null) yield break;
            //
            yield return item;
            foreach (GridItem child in item.GridItems)
            {
                foreach (GridItem gc in child.EnumerateAllItems())
                {
                    yield return gc;
                }
            }
        }
        // controls
        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }
        // Int array
        public static string ToShortString(this int[] intArray, int numOfItems = 10)
        {
            string allNames = null;
            if (intArray != null)
            {
                if (numOfItems > intArray.Length) numOfItems = intArray.Length;
                //
                for (int i = 0; i < numOfItems; i++)
                {
                    if (i != 0) allNames += ", ";
                    allNames += intArray[i];
                }
                if (intArray.Length > numOfItems) allNames += ", ...";
            }
            return allNames;
        }
        // Float Array
        public static float[] ToFloat(this double[] arr) => Array.ConvertAll(arr, x => (float)x);
        // Double Array
        public static double[] ToDouble(this bool[] arr) => Array.ConvertAll(arr, x => x == true ? 1d : 0d);
        public static double[] ToDouble(this byte[] arr) => Array.ConvertAll(arr, x => (double)x);
        public static double[] ToDouble(this decimal[] arr) => Array.ConvertAll(arr, x => (double)x);
        public static double[] ToDouble(this int[] arr) => Array.ConvertAll(arr, x => (double)x);
        public static double[] ToDouble(this float[] arr) => Array.ConvertAll(arr, x => (double)x);
        //
        public static double[] ToFlatArray(this double[][] arr)
        {
            int index = 0;
            double[] result = new double[arr.Length * arr[0].Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i].CopyTo(result, index);
                index += arr[i].Length;
            }
            return result;
        }
        public static double[][] ToJaggedArray(this double[] arr, int numRows)
        {
            int numCol = arr.Length / numRows;
            double[][] result = new double[numRows][];
            for (int i = 0; i < numRows; i++)
            {
                result[i] = new double[numCol];
                Array.Copy(arr, i * numCol, result[i], 0, numCol);
            }
            return result;
        }
        // String
        public static string ToUTF8(this string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Encoding.Default.GetString(bytes);
        }
        public static string ToUnicode(this string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            return Encoding.Default.GetString(bytes);
        }
        public static string ToASCII(this string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            return Encoding.Default.GetString(bytes);
        }
        //
        public static string ToShortString(this string[] stringArray)
        {
            string allNames = null;
            if (stringArray != null)
            {
                if (stringArray.Length >= 1) allNames = stringArray[0];
                if (stringArray.Length >= 2) allNames += ", ...";
            }
            return allNames;
        }
        public static string ToDelimitedString(this string[] stringArray, string delimiter)
        {
            string allNames = "";
            if (stringArray != null)
            {
                foreach (var item in stringArray)
                {
                    if (allNames.Length > 0) allNames += delimiter;
                    allNames += item;
                }
            }
            return allNames;
        }
        //
        public static IEnumerable<int> AllIndicesOf(this string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            return Kmp(text, pattern);
        }
        private static IEnumerable<int> Kmp(string text, string pattern)
        {
            int M = pattern.Length;
            int N = text.Length;

            int[] lps = LongestPrefixSuffix(pattern);
            int i = 0, j = 0;

            while (i < N)
            {
                if (pattern[j] == text[i])
                {
                    j++;
                    i++;
                }
                if (j == M)
                {
                    yield return i - j;
                    j = lps[j - 1];
                }

                else if (i < N && pattern[j] != text[i])
                {
                    if (j != 0)
                    {
                        j = lps[j - 1];
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
        private static int[] LongestPrefixSuffix(string pattern)
        {
            int[] lps = new int[pattern.Length];
            int length = 0;
            int i = 1;

            while (i < pattern.Length)
            {
                if (pattern[i] == pattern[length])
                {
                    length++;
                    lps[i] = length;
                    i++;
                }
                else
                {
                    if (length != 0)
                    {
                        length = lps[length - 1];
                    }
                    else
                    {
                        lps[i] = length;
                        i++;
                    }
                }
            }
            return lps;
        }
        //
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        // vtkSelectBy
        public static bool IsGeometryBased(this vtkSelectBy selectBy)
        {
            return selectBy == vtkSelectBy.QueryEdge ||
                   selectBy == vtkSelectBy.QuerySurface ||
                   selectBy == vtkSelectBy.Geometry ||
                   selectBy == vtkSelectBy.GeometryVertex ||
                   selectBy == vtkSelectBy.GeometryEdge ||
                   selectBy == vtkSelectBy.GeometrySurface ||
                   selectBy == vtkSelectBy.GeometryEdgeAngle ||
                   selectBy == vtkSelectBy.GeometrySurfaceAngle ||
                   selectBy == vtkSelectBy.GeometryPart;
        }
        // Double
        public static string ToCalculiX16String(this double value, bool enforceSeparator = false)
        {
            string result = value.ToString().ToUpper();
            if (result.Length > 16) result = value.ToString("E8");
            else if (enforceSeparator && !result.Contains("."))
            {
                int location = result.IndexOf("E");
                // The first letter must not be E so > 0
                if (location > 0) result = result.Insert(location, ".");
                else result += ".";
            }
            return result;
        }
        // String[]
        public static string ToRows(this string[] names, int maxRows = 30)
        {
            string rows = "";
            for (int i = 0; i < Math.Min(names.Length, maxRows); i++)
            {
                rows += names[i];
                if (i < names.Length - 1) rows += Environment.NewLine;
            }
            if (maxRows < names.Length) rows += "...";
            return rows;
        }
        // Enum
        public static string GetDescription<T>(this T enumerationValue) where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }

            //Tries to find a DescriptionAttribute for a potential friendly name
            //for the enum
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    //Pull out the description value
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            //If we have no description attribute, just return the ToString of the enum
            return enumerationValue.ToString();
        }
        public static string GetDisplayedName<T>(this T enumerationValue) where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }

            //Tries to find a DescriptionAttribute for a potential friendly name
            //for the enum
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DynamicTypeDescriptor.StandardValueAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    //Pull out the description value
                    return ((DynamicTypeDescriptor.StandardValueAttribute)attrs[0]).DisplayName;
                }
            }
            //If we have no description attribute, just return the ToString of the enum
            return enumerationValue.ToString();
        }
        // NamedClass[]
        public static string[] GetNames(this NamedClass[] namedClasses)
        {
            if (namedClasses == null) return new string[0];
            //
            List<string> names = new List<string>();
            foreach (var namedClass in namedClasses) names.Add(namedClass.Name);
            return names.ToArray();
        }
        // Color
        public static Color Lighten(this Color color)
        {
            double hue;
            double saturation;
            double brightness;
            color.ToHSB(out  hue, out saturation, out brightness);
            {
                // Lighten
                double factor = 0.75;
                brightness += (1 - brightness) * factor;
                saturation -= saturation * factor;
                //
                return ColorFromHSB(hue, saturation, brightness);
            }
        }
        public static void ToHSB(this Color color, out double hue, out double saturation, out double brightness)
        {
            hue = color.GetHue();
            saturation = color.GetSaturation();
            brightness = color.GetBrightness();
        }
        public static Color ColorFromHSB(double hue, double saturation, double brightness)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            //
            brightness = brightness * 255;
            int v = Convert.ToInt32(brightness);
            int p = Convert.ToInt32(brightness * (1 - saturation));
            int q = Convert.ToInt32(brightness * (1 - f * saturation));
            int t = Convert.ToInt32(brightness * (1 - (1 - f) * saturation));
            //
            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
        public static void RgbToHsb(this Color rgb, out double hue, out double saturation, out double brightness)
        {
            // _NOTE #1: Even though we're dealing with a very small range of
            // numbers, the accuracy of all calculations is fairly important.
            // For this reason, I've opted to use double data types instead
            // of float, which gives us a little bit extra precision (recall
            // that precision is the number of significant digits with which
            // the result is expressed).

            var r = rgb.R / 255d;
            var g = rgb.G / 255d;
            var b = rgb.B / 255d;
            //
            var minValue = Math.Min(r, Math.Min(g, b));
            var maxValue = Math.Max(r, Math.Max(g, b));
            var delta = maxValue - minValue;
            //
            hue = 0;
            saturation = 0;
            brightness = maxValue * 100;
            //
            if (Math.Abs(maxValue - 0) < 0.00001 || Math.Abs(delta - 0) < 0.00001)
            {
                hue = 0;
                saturation = 0;
            }
            else
            {
                // _NOTE #2: FXCop insists that we avoid testing for floating 
                // point equality (CA1902). Instead, we'll perform a series of
                // tests with the help of 0.00001 that will provide 
                // a more accurate equality evaluation.

                if (Math.Abs(minValue - 0) < 0.00001)
                {
                    saturation = 100;
                }
                else
                {
                    saturation = delta / maxValue * 100;
                }

                if (Math.Abs(r - maxValue) < 0.00001)
                {
                    hue = (g - b) / delta;
                }
                else if (Math.Abs(g - maxValue) < 0.00001)
                {
                    hue = 2 + (b - r) / delta;
                }
                else if (Math.Abs(b - maxValue) < 0.00001)
                {
                    hue = 4 + (r - g) / delta;
                }
            }

            hue *= 60;
            if (hue < 0)
            {
                hue += 360;
            }
            saturation /= 100;
            brightness /= 100;
        }
        public static Color HsbToRgb(double hue, double saturation, double brightness)
        {
            double red = 0, green = 0, blue = 0;
            //
            double h = hue;
            var s = saturation;
            var b = brightness;
            //
            if (Math.Abs(s - 0) < 0.00001)
            {
                red = b;
                green = b;
                blue = b;
            }
            else
            {
                // The color wheel has six sectors.
                var sectorPosition = h / 60;
                var sectorNumber = (int)Math.Floor(sectorPosition);
                var fractionalSector = sectorPosition - sectorNumber;
                //
                var p = b * (1 - s);
                var q = b * (1 - s * fractionalSector);
                var t = b * (1 - s * (1 - fractionalSector));
                // Assign the fractional colors to r, g, and b based on the sector the angle is in.
                switch (sectorNumber)
                {
                    case 0:
                        red = b;
                        green = t;
                        blue = p;
                        break;
                    case 1:
                        red = q;
                        green = b;
                        blue = p;
                        break;
                    case 2:
                        red = p;
                        green = b;
                        blue = t;
                        break;
                    case 3:
                        red = p;
                        green = q;
                        blue = b;
                        break;

                    case 4:
                        red = t;
                        green = p;
                        blue = b;
                        break;

                    case 5:
                        red = b;
                        green = p;
                        blue = q;
                        break;
                }
            }
            //
            byte nRed = (byte)(red * 255);
            byte nGreen = (byte)(green * 255);
            byte nBlue = (byte)(blue * 255);
            //
            return Color.FromArgb(255, (int)nRed, (int)nGreen, (int)nBlue);
        }
    }





}
