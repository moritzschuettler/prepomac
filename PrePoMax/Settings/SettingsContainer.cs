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
using System.Threading.Tasks;
using CaeGlobals;
using System.IO;
using System.Text;
using CaeMesh;

namespace PrePoMax
{
    [PmxType(10542)]
    public class SettingsContainer
    {
        // Static variables                                                                                                         
        private static string _overrideFileName;
        private static bool _isSaveDisabled;
        // Variables                                                                                                                
        [NonSerialized] private Dictionary<string, vtkControl.vtkMaxColorSpectrum> _colorSpectrums;
        [NonSerialized] private string _regenerationWorkDirectory;
        //
        [PmxKey(0)] private GeneralSettings _general;
        [PmxKey(1)] private NumberFormatSettings _numberFormat;
        [PmxKey(2)] private GraphicsSettings _graphics;
        [PmxKey(3)] private NavigationSettings _navigation;
        [PmxKey(4)] private ColorSettings _color;
        [PmxKey(5)] private AnnotationSettings _annotations;
        [PmxKey(6)] private MeshingSettings _meshing;
        [PmxKey(7)] private PreSettings _pre;
        [PmxKey(8)] private SolverSettings _solver;
        [PmxKey(9)] private PostSettings _post;
        [PmxKey(10)] private LegendSettings _legend;
        [PmxKey(11)] private StatusBlockSettings _statusBlock;
        [PmxKey(12)] private AutomationSettings _automation;


        // Static properties                                                                                                        
        // An unattended run must be able to keep the settings of the interactive user untouched
        public static string OverrideFileName { get { return _overrideFileName; } set { _overrideFileName = value; } }
        public static bool IsSaveDisabled { get { return _isSaveDisabled; } set { _isSaveDisabled = value; } }


        // Properties                                                                                                               
        public GeneralSettings General { get { return _general; } set { _general = value; } }
        public NumberFormatSettings NumberFormat { get { return _numberFormat; } set { _numberFormat = value; } }
        public GraphicsSettings Graphics { get { return _graphics; } set { _graphics = value; } }
        public NavigationSettings Navigation { get { return _navigation; } set { _navigation = value; } }
        public ColorSettings Color { get { return _color; } set { _color = value; } }
        public AnnotationSettings Annotations { get { return _annotations; } set { _annotations = value; } }
        public MeshingSettings Meshing { get { return _meshing; } set { _meshing = value; } }
        public PreSettings Pre { get { return _pre; } set { _pre = value; } }
        public SolverSettings Solver { get { return _solver; } set { _solver = value; } }
        public PostSettings Post { get { return _post; } set { _post = value; } }
        public LegendSettings Legend { get { return _legend; } set { _legend = value; } }
        public StatusBlockSettings StatusBlock { get { return _statusBlock; } set { _statusBlock = value; } }
        public AutomationSettings Automation { get { return _automation; } set { _automation = value; } }


        // Constructors                                                                                                             
        public SettingsContainer()
        {
            Initialize();
        }
        public SettingsContainer(Dictionary<string, ISettings> items)
        {
            Initialize();
            FromDictionary(items);
        }


        // Methods                                                                                                                  
        public void Initialize()
        {
            _colorSpectrums = new Dictionary<string, vtkControl.vtkMaxColorSpectrum>();
            //
            _general = new GeneralSettings();
            _numberFormat = new NumberFormatSettings();
            _graphics = new GraphicsSettings();
            _navigation = new NavigationSettings();
            _color = new ColorSettings();
            _annotations = new AnnotationSettings();
            _meshing = new MeshingSettings();
            _pre = new PreSettings();
            _solver = new SolverSettings();
            _post = new PostSettings();
            _legend = new LegendSettings();
            _statusBlock = new StatusBlockSettings();
            _automation = new AutomationSettings();
        }
        public void Reset()
        {
            _general.Reset();
            _numberFormat.Reset();
            _graphics.Reset();
            _navigation.Reset();
            _color.Reset();
            _annotations.Reset();
            _meshing.Reset();
            _pre.Reset();
            _solver.Reset();
            _post.Reset();
            _legend.Reset();
            _statusBlock.Reset();
            _automation.Reset();
        }
        public void ClearColorSpectrums()
        {
            _colorSpectrums.Clear();
            _legend.ColorSpectrum.MinMaxType = vtkControl.vtkColorSpectrumMinMaxType.Automatic;
        }
        public void Set(SettingsContainer settingsContainer, ViewGeometryModelResults currentView,
                        CaeResults.FieldData currentFieldData)
        {
            Clone(settingsContainer);
            //
            if (currentView == ViewGeometryModelResults.Results && currentFieldData != null)
            {
                string key = currentFieldData.Name + "_" + currentFieldData.Component;
                // Save individual Min/Max setting
                if (_legend.ColorSpectrum.MinMaxType == vtkControl.vtkColorSpectrumMinMaxType.Manual)
                {
                    if (_colorSpectrums.ContainsKey(key)) _colorSpectrums[key] = _legend.ColorSpectrum.DeepClone();
                    else _colorSpectrums.Add(key, _legend.ColorSpectrum.DeepClone());
                }
                // Remove individual Min/Max setting
                else _colorSpectrums.Remove(key);
            }
            //
            _legend.ColorSpectrum.MinMaxType = vtkControl.vtkColorSpectrumMinMaxType.Automatic;
        }
        public SettingsContainer Get(ViewGeometryModelResults currentView, CaeResults.FieldData currentFieldData)
        {
            SettingsContainer clone = new SettingsContainer();
            clone.Clone(this);
            clone._regenerationWorkDirectory = _regenerationWorkDirectory;
            //
            if (currentView == ViewGeometryModelResults.Results && currentFieldData != null)
            {
                vtkControl.vtkMaxColorSpectrum colorSpectrum;
                string key = currentFieldData.Name + "_" + currentFieldData.Component;
                // Apply individual Min/Max setting if it exists
                if (_colorSpectrums.TryGetValue(key, out colorSpectrum) &&
                    colorSpectrum.MinMaxType == vtkControl.vtkColorSpectrumMinMaxType.Manual)
                {
                    clone._legend.ColorSpectrum.SetMinMax(colorSpectrum);
                }
            }
            //
            return clone;
        }
        private void Clone(SettingsContainer settingsContainer)
        {
            SettingsContainer clone = settingsContainer.DeepClone();
            //
            _general = clone._general;
            _numberFormat = clone._numberFormat;
            _graphics = clone._graphics;
            _navigation = clone._navigation;
            _color = clone._color;
            _annotations = clone._annotations;
            _meshing = clone._meshing;
            _pre = clone._pre;
            _solver = clone._solver;
            _post = clone._post;
            _legend = clone._legend;
            _statusBlock = clone._statusBlock;
            _automation = clone._automation;
        }
        public Dictionary<string, ISettings> ToDictionary()
        {
            Dictionary<string, ISettings> items = new Dictionary<string, ISettings>();
            items.Add(Globals.GeneralSettingsName, _general);
            items.Add(Globals.NumberFormatSettingsName, _numberFormat);
            items.Add(Globals.GraphicsSettingsName, _graphics);
            items.Add(Globals.NavigationSettingsName, _navigation);
            items.Add(Globals.ColorSettingsName, _color);
            items.Add(Globals.AnnotationSettingsName, _annotations);
            items.Add(Globals.MeshingSettingsName, _meshing);
            items.Add(Globals.PreSettingsName, _pre);
            items.Add(Globals.SolverSettingsName, _solver);
            items.Add(Globals.PostSettingsName, _post);
            items.Add(Globals.LegendSettingsName, _legend);
            items.Add(Globals.StatusBlockSettingsName, _statusBlock);
            items.Add(Globals.AutomationSettingsName, _automation);
            return items;
        }
        public void FromDictionary(Dictionary<string, ISettings> items)
        {
            try
            {
                _general = (GeneralSettings)items[Globals.GeneralSettingsName];
                _numberFormat = (NumberFormatSettings)items[Globals.NumberFormatSettingsName];
                _graphics = (GraphicsSettings)items[Globals.GraphicsSettingsName];
                _color = (ColorSettings)items[Globals.ColorSettingsName];
                _annotations = (AnnotationSettings)items[Globals.AnnotationSettingsName];
                _meshing = (MeshingSettings)items[Globals.MeshingSettingsName];
                _pre = (PreSettings)items[Globals.PreSettingsName];
                _solver = (SolverSettings)items[Globals.SolverSettingsName];
                _post = (PostSettings)items[Globals.PostSettingsName];
                _legend = (LegendSettings)items[Globals.LegendSettingsName];
                _statusBlock = (StatusBlockSettings)items[Globals.StatusBlockSettingsName];
                // Added after the settings file format was already in use
                ISettings navigation;
                if (items.TryGetValue(Globals.NavigationSettingsName, out navigation))
                    _navigation = (NavigationSettings)navigation;
                ISettings automation;
                if (items.TryGetValue(Globals.AutomationSettingsName, out automation))
                    _automation = (AutomationSettings)automation;
            }
            catch
            {

            }
        }
        public void SaveToFile()
        {
            if (_isSaveDisabled) return;
            //
            SaveToFile(GetDefaultFileName());
        }
        private static string GetDefaultFileName()
        {
            if (_overrideFileName != null) return _overrideFileName;
            else return Globals.GetUserDataFileName(Globals.SettingsFileName);
        }
        public void SaveToFile(string fileName)
        {
            if (_regenerationWorkDirectory == null)
            {
                // Use a temporary file to save the data and copy it at the end
                string tmpFileName = Tools.GetNonExistentRandomFileName(Path.GetDirectoryName(fileName), ".tmp");
                File.WriteAllText(tmpFileName, PmxJsonSerializer.Serialize(ToDictionary()), new UTF8Encoding(false));
                File.Copy(tmpFileName, fileName, true);
                File.Delete(tmpFileName);
            }
        }
        public void LoadFromFile()
        {
            try
            {
                Initialize();
                //
                string fileName = GetDefaultFileName();
                string legacyFileName = Path.Combine(System.Windows.Forms.Application.StartupPath,
                                                     Globals.LegacySettingsFileName);
                // A settings file of an older version copied in after the first start is newer than the settings file
                bool useLegacyFile = _overrideFileName == null && File.Exists(legacyFileName) &&
                                     (!File.Exists(fileName) ||
                                      File.GetLastWriteTimeUtc(legacyFileName) > File.GetLastWriteTimeUtc(fileName));
                if (useLegacyFile)
                {
                    var t = Task.Run(() => LoadFromFile(legacyFileName));
                    t.Wait();
                    SaveToFile();
                }
                else if (File.Exists(fileName))
                {
                    var t = Task.Run(() => LoadFromFile(fileName));
                    t.Wait();
                }
                // Reset the color limits
                ClearColorSpectrums();
            }
            catch
            {
                Initialize();
            }
        }
        // A settings file is JSON; the binary settings file of an older version is read once and then rewritten
        private void LoadFromFile(string fileName)
        {
            string text = File.ReadAllText(fileName);
            if (text.TrimStart().StartsWith("{"))
            {
                Dictionary<string, object> node = JsonReader.Parse(text) as Dictionary<string, object>;
                if (node == null) return;
                object settingsNode;
                Dictionary<string, ISettings> items = ToDictionary();
                foreach (var entry in new List<KeyValuePair<string, ISettings>>(items))
                {
                    if (!node.TryGetValue(entry.Key, out settingsNode) || !(settingsNode is Dictionary<string, object> fields))
                        continue;
                    // A section that cannot be read keeps its defaults, so an edit gone wrong costs only that section
                    try
                    {
                        PmxJsonSerializer.Populate(entry.Value, fields);
                    }
                    catch
                    {
                        items[entry.Key] = (ISettings)Activator.CreateInstance(entry.Value.GetType());
                    }
                }
                FromDictionary(items);
            }
            else
            {
                Dictionary<string, ISettings> items = Tools.LoadDumpFromFile<Dictionary<string, ISettings>>(fileName);
                FromDictionary(items);
            }
        }
        //
        public void SetRegenerationWorkDirectory(string workDirectory)
        {
            _regenerationWorkDirectory = workDirectory;
        }
        public string GetRegenerationWorkDirectory()
        {
            return _regenerationWorkDirectory;
        }
        public string GetWorkDirectory()
        {
            string lastFileName = _general.LastFileName;
            if (_regenerationWorkDirectory != null)
            {
                return _regenerationWorkDirectory;
            }
            else if (_solver.UsePmxFolderAsWorkDirectory && lastFileName != null && File.Exists(lastFileName) &&
                     Path.GetExtension(lastFileName) == ".pmx")
            {
                return Path.GetDirectoryName(lastFileName);
            }
            else
            {
                return _solver.WorkDirectoryForSettingsOnly;
            }
        }
        public void CheckWorkingDirectory()
        {
            string workDirectory = GetWorkDirectory();
            if (workDirectory != null && workDirectory.ContainsNonEnglishCharacters())
                MessageBoxes.ShowWarning(SolverSettings.NonEnglishDirectoryWarning);
        }
    }
}
