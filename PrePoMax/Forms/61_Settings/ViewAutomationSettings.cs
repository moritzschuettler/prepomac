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
        //
        [CategoryAttribute("Connection")]
        [OrderedDisplayName(0, 10, "Port")]
        [DescriptionAttribute("The AI assistant reaches PrePoMax through this port on your own computer, and nothing " +
                              "outside your computer can connect to it. Leave it as it is unless another program " +
                              "already uses this port: PrePoMax then takes the next free one and writes which in " +
                              "the message window and the status bar.")]
        [Id(1, 2)]
        public int Port
        {
            get { return _automationSettings.Port; }
            set { _automationSettings.Port = value; }
        }
        //
        [CategoryAttribute("Connection")]
        [OrderedDisplayName(1, 10, "Address")]
        [DescriptionAttribute("Give this address to your AI assistant, together with the access token below. It " +
                              "answers only while PrePoMax is open and the automation server is switched on. So if " +
                              "the assistant says it cannot connect, PrePoMax is closed or the server is off: " +
                              "switch it on in Tools - Automation Server, or set Start with the application above " +
                              "and it will be on whenever PrePoMax is.")]
        [ReadOnly(true)]
        [Id(2, 2)]
        public string Endpoint
        {
            get { return Mcp.McpHttpServer.GetEndpointUrl(_automationSettings.Port); }
            set { }
        }
        //
        [CategoryAttribute("Connection")]
        [OrderedDisplayName(2, 10, "Access token")]
        [DescriptionAttribute("A password for the address above, so that only the assistant you set up can drive " +
                              "PrePoMax and no other program or web page on your computer can. Copy the address " +
                              "and this token into your assistant once; the server will not run without one. " +
                              "Treat it like any password - anyone who can use your Windows account can read it. " +
                              "Right click here to generate one, to replace it, or to clear it before you prepare " +
                              "a redistributable. It is kept in this folder, so a newly unpacked PrePoMax starts " +
                              "without one.")]
        [ReadOnly(true)]
        [Id(3, 2)]
        public string AccessToken
        {
            get { return _automationSettings.AccessToken; }
            set { _automationSettings.AccessToken = value; }
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
