// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Drawing;

namespace APIs.Python
{
    // A color reaches the API as a list of three or four channel values, or as a color name
    internal static class PythonColorConverter
    {
        // Methods
        public static Color ToColor(object color, string argumentName)
        {
            if (color == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            string colorName = color as string;
            if (colorName != null) return ToNamedColor(colorName, argumentName);
            //
            // The ID converter rejects zero, so the channels are read as plain numbers
            double[] values = PythonDoubleListConverter.ToDoubleArray(color, argumentName);
            if (values == null || (values.Length != 3 && values.Length != 4))
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 or 4 channel values.");
            //
            int[] channels = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < 0 || values[i] > 255)
                    throw new PythonAPIException("The channel values of the argument '" + argumentName +
                                                 "' must be between 0 and 255.");
                channels[i] = (int)values[i];
            }
            //
            if (channels.Length == 3) return Color.FromArgb(channels[0], channels[1], channels[2]);
            else return Color.FromArgb(channels[3], channels[0], channels[1], channels[2]);
        }
        public static IntListAPI ToChannels(Color color)
        {
            return new IntListAPI(new int[] { color.R, color.G, color.B, color.A });
        }
        public static byte ToAlpha(object transparency, string argumentName)
        {
            int value = PythonValueConverter.ToInt(transparency, argumentName);
            if (value < 0 || value > 255)
                throw new PythonAPIException("The argument '" + argumentName + "' must be between 0 and 255.");
            //
            return (byte)value;
        }
        //
        private static Color ToNamedColor(string colorName, string argumentName)
        {
            Color color = Color.FromName(colorName);
            // An unknown name silently produces a fully transparent black
            if (!color.IsKnownColor)
                throw new PythonAPIException("The color name '" + colorName + "' used for the argument '" +
                                             argumentName + "' is not known.");
            //
            return color;
        }
    }
}
