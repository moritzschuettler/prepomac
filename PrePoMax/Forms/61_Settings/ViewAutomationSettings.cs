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
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewAutomationSettings : IViewSettings, IReset
    {
        // Variables                                                                                                                
        private AutomationSettings _automationSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Server")]
        [OrderedDisplayName(0, 10, "Start with the application")]
        [DescriptionAttribute("Select yes to start the automation server when PrePoMax starts. The server lets an AI " +
                              "agent drive this session; it accepts only the user that runs PrePoMax and uses no " +
                              "network connection.")]
        [Id(1, 1)]
        public bool StartWithApplication
        {
            get { return _automationSettings.StartWithApplication; }
            set { _automationSettings.StartWithApplication = value; }
        }
        //
        [CategoryAttribute("Server")]
        [OrderedDisplayName(1, 10, "Read only")]
        [DescriptionAttribute("Select yes to let the automation server read the model but refuse every call that would " +
                              "change it.")]
        [Id(2, 1)]
        public bool IsReadOnly
        {
            get { return _automationSettings.IsReadOnly; }
            set { _automationSettings.IsReadOnly = value; }
        }
        //
        [CategoryAttribute("Server")]
        [OrderedDisplayName(2, 10, "Echo to the Python panel")]
        [DescriptionAttribute("Select yes to write everything the automation server runs into the Python panel, so it " +
                              "is visible while it happens.")]
        [Id(3, 1)]
        public bool EchoToPythonPanel
        {
            get { return _automationSettings.EchoToPythonPanel; }
            set { _automationSettings.EchoToPythonPanel = value; }
        }
        //
        [CategoryAttribute("Server")]
        [OrderedDisplayName(3, 10, "Request timeout [s]")]
        [DescriptionAttribute("Set the time the automation server waits for a request to finish before it reports the " +
                              "application as busy.")]
        [Id(4, 1)]
        public int RequestTimeout
        {
            get { return _automationSettings.RequestTimeout; }
            set { _automationSettings.RequestTimeout = value; }
        }


        // Constructors                                                                                                             
        public ViewAutomationSettings(AutomationSettings automationSettings)
        {
            _automationSettings = automationSettings;
            _dctd = ProviderInstaller.Install(this);
            _dctd.RenameBooleanPropertyToYesNo(nameof(StartWithApplication));
            _dctd.RenameBooleanPropertyToYesNo(nameof(IsReadOnly));
            _dctd.RenameBooleanPropertyToYesNo(nameof(EchoToPythonPanel));
        }


        // Methods                                                                                                                  
        public ISettings GetBase()
        {
            return _automationSettings;
        }
        public void Reset()
        {
            _automationSettings.Reset();
        }
    }
}
