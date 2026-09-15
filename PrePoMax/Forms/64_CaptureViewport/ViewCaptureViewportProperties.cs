// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using System.Drawing.Design;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewCaptureViewportProperties
    {
        // Variables                                                                                                                
        private CaptureViewportProperties _properties;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("File")]
        [OrderedDisplayName(0, 10, "Folder")]
        [DescriptionAttribute("Select the folder to capture the viewport into.")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public string Folder { get { return _properties.Folder; } set { _properties.Folder = value; } }
        //
        [CategoryAttribute("File")]
        [OrderedDisplayName(1, 10, "Name")]
        [DescriptionAttribute("Set the name of the picture. The .png extension is added to it.")]
        [Id(2, 1)]
        public string FileName { get { return _properties.FileName; } set { _properties.FileName = value; } }
        //
        [CategoryAttribute("File")]
        [OrderedDisplayName(2, 10, "Auto increment")]
        [DescriptionAttribute("Select yes to add the first number the folder does not hold yet to the name, so that " +
                              "a run of captures replaces none of the pictures taken before it. Select no to write " +
                              "the name as it is, which replaces the file when it already exists.")]
        [Id(3, 1)]
        public bool AutoIncrement
        {
            get { return _properties.AutoIncrement; }
            set { _properties.AutoIncrement = value; }
        }
        //
        [CategoryAttribute("Picture")]
        [OrderedDisplayName(0, 10, "Transparent background")]
        [DescriptionAttribute("Select yes to leave the viewport background out of the picture, so that it can be put " +
                              "on a page of any color. The model, the legend and the labels keep their own colors.")]
        [Id(1, 2)]
        public bool TransparentBackground
        {
            get { return _properties.TransparentBackground; }
            set { _properties.TransparentBackground = value; }
        }
        //
        [CategoryAttribute("Picture")]
        [OrderedDisplayName(1, 10, "Magnification")]
        [DescriptionAttribute("Set how many times larger than the viewport the picture is rendered, for a picture that " +
                              "is going to be printed. Only the model is drawn larger: the legend, the scale bar and " +
                              "the triad keep the size they have on screen.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(2, 2)]
        public int Magnification
        {
            get { return _properties.Magnification; }
            set { _properties.Magnification = value; }
        }


        // Constructors                                                                                                             
        public ViewCaptureViewportProperties(CaptureViewportProperties properties)
        {
            _properties = properties;
            //
            _dctd = ProviderInstaller.Install(this);
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(AutoIncrement));
            _dctd.RenameBooleanPropertyToYesNo(nameof(TransparentBackground));
        }


        // Methods                                                                                                                  
        public CaptureViewportProperties GetBase()
        {
            return _properties;
        }
    }
}
