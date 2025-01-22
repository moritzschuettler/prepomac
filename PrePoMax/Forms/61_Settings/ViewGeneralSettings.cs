using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.IO.Compression;

namespace PrePoMax.Settings
{
    [Serializable]
    public enum CompressionEnum
    {
        [StandardValue("NoCompression", DisplayName = "No compression")]
        NoCompression,
        //
        [StandardValue("Fastest", DisplayName = "Fastest")]
        Fastest,
        //
        [StandardValue("Optimal", DisplayName = "Optimal")]
        Optimal,
    }
    //
    [Serializable]
    public class ViewGeneralSettings : IViewSettings, IReset
    {
        // Variables                                                                                                                
        private GeneralSettings _generalSettings;
        private DynamicCustomTypeDescriptor _dctd = null;
        private bool _splitPeriodicFaces;


        // Properties                                                                                                               
        [Category("General")]
        [OrderedDisplayName(0, 10, "Open last file")]
        [Description("When the program starts open the last file Saved/Opened.")]
        public bool OpenLastFile { get { return _generalSettings.OpenLastFile; } set { _generalSettings.OpenLastFile = value; } }
        //
        [Category("General")]
        [OrderedDisplayName(1, 10, "Last file name")]
        [Description("The name of the last file Saved/Opened.")]
        [ReadOnly(true)]
        public string LastFileName { get { return _generalSettings.LastFileName; } set { _generalSettings.LastFileName = value; } }
        //
        [Category("General")]
        [OrderedDisplayName(2, 10, "Save results in .pmx files")]
        [Description("Save the results in the PrePoMax .pmx file.")]
        public bool SaveResultsInPmx
        {
            get { return _generalSettings.SaveResultsInPmx; }
            set { _generalSettings.SaveResultsInPmx = value; }
        }
        //
        [Category("General")]
        [OrderedDisplayName(3, 10, "Compress .pmx files")]
        [Description("Select the .pmx file compression level.")]
        public CompressionEnum Compression
        {
            get
            {
                if (_generalSettings.CompressionLevel == CompressionLevel.NoCompression) return CompressionEnum.NoCompression;
                else if (_generalSettings.CompressionLevel == CompressionLevel.Fastest) return CompressionEnum.Fastest;
                else if (_generalSettings.CompressionLevel == CompressionLevel.Optimal) return CompressionEnum.Optimal;
                else throw new NotSupportedException();
            }
            set
            {
                if (value == CompressionEnum.NoCompression) _generalSettings.CompressionLevel = CompressionLevel.NoCompression;
                else if (value == CompressionEnum.Fastest) _generalSettings.CompressionLevel = CompressionLevel.Fastest;
                else if (value == CompressionEnum.Optimal) _generalSettings.CompressionLevel = CompressionLevel.Optimal;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("General")]
        [OrderedDisplayName(4, 10, "Default unit system")]
        [Description("Select the default unit system for new models.")]
        public string UnitSystemType
        {
            get
            {
                return _generalSettings.UnitSystemType.GetDescription();
            }
            set
            {
                foreach (UnitSystemType unitSystemType in Enum.GetValues(typeof(UnitSystemType)))
                {
                    if (unitSystemType.GetDescription() == value)
                    {
                        _generalSettings.UnitSystemType = unitSystemType;
                        return;
                    }
                }
                throw new NotSupportedException();
            }
        }
        //
        [Category("Import CAD")]
        [OrderedDisplayName(0, 10, "Split periodic faces")]
        [Description("Select yes to split CAD periodic faces during geometry import.")]
        public bool SplitPeriodicFaces
        {
            get { return _splitPeriodicFaces; }
            set
            {
                _splitPeriodicFaces = value;
                //
                if (_splitPeriodicFaces == true)
                {
                    if (_generalSettings.NumOfSplitFaces < 2) _generalSettings.NumOfSplitFaces = 2;
                }
                else _generalSettings.NumOfSplitFaces = 1;
                //
                UpdateVisibility();
            }
        }
        [Category("Import CAD")]
        [OrderedDisplayName(1, 10, "Number of resulting faces")]
        [Description("Select the number of faces the CAD periodic faces will be split into during geometry import.")]
        public int NumOfSplitFaces
        {
            get { return _generalSettings.NumOfSplitFaces; }
            set { _generalSettings.NumOfSplitFaces = value; }
        }
        //
        [Category("Import mesh")]
        [OrderedDisplayName(0, 10, "Edge angle")]
        [Description("Select the edge angle for the detection of model edges. The angle will be used for future imports.")]
        [TypeConverter(typeof(StringAngleDegConverter))]
        public double EdgeAngle { get { return _generalSettings.EdgeAngle; } set { _generalSettings.EdgeAngle = value; } }
        //
        [Category("Open results")]
        [OrderedDisplayName(0, 10, "Run history postprocessing")]
        [Description("Select yes to run existing history postprocessing commands from the results with the same file name.")]
        public bool RunPostprocessingCommands
        {
            get { return _generalSettings.RunHistoryPostprocessing; }
            set { _generalSettings.RunHistoryPostprocessing = value; }
        }


        // Constructors                                                                                                             
        public ViewGeneralSettings(GeneralSettings generalSettings)
        {
            _generalSettings = generalSettings;
            //
            if (_generalSettings.NumOfSplitFaces > 1) _splitPeriodicFaces = true;
            else _splitPeriodicFaces = false;
            //
            _dctd = ProviderInstaller.Install(this);
            // Now lets display Yes/No instead of True/False
            _dctd.RenameBooleanPropertyToYesNo(nameof(OpenLastFile));
            _dctd.RenameBooleanPropertyToYesNo(nameof(SaveResultsInPmx));
            _dctd.RenameBooleanPropertyToYesNo(nameof(SplitPeriodicFaces));
            _dctd.RenameBooleanPropertyToYesNo(nameof(RunPostprocessingCommands));
            // Add unit system types as description strings
            List<string> descriptions = new List<string>();
            foreach (UnitSystemType unitSystemType in Enum.GetValues(typeof(UnitSystemType)))
                descriptions.Add(unitSystemType.GetDescription());
            _dctd.PopulateProperty(nameof(UnitSystemType), descriptions.ToArray());
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public ISettings GetBase()
        {
            return _generalSettings;
        }
        public void Reset()
        {
            _generalSettings.Reset();
            _splitPeriodicFaces = true;
            //
            UpdateVisibility();
        }
        private void UpdateVisibility()
        {
            _dctd.GetProperty(nameof(NumOfSplitFaces)).SetIsBrowsable(_splitPeriodicFaces);
        }
    }

}
