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
        [CategoryAttribute("Endpoint")]
        [OrderedDisplayName(0, 10, "Port")]
        [DescriptionAttribute("Set the port the MCP endpoint listens on. It listens on this computer only. When the " +
                              "port is taken - by another PrePoMax, or by another program - the endpoint takes the " +
                              "first free port above it and says which one in the output.")]
        [Id(1, 2)]
        public int Port
        {
            get { return _automationSettings.Port; }
            set { _automationSettings.Port = value; }
        }
        //
        [CategoryAttribute("Endpoint")]
        [OrderedDisplayName(1, 10, "Address")]
        [DescriptionAttribute("The address to give the AI agent client. It answers only while PrePoMax is open with " +
                              "its automation server on, so a client that cannot connect is telling you that " +
                              "PrePoMax is closed or that the server is off - turn it on in Tools - Automation " +
                              "Server, or leave it on with Start with the application.")]
        [ReadOnly(true)]
        [Id(2, 2)]
        public string Endpoint
        {
            get { return Mcp.McpHttpServer.GetEndpointUrl(_automationSettings.Port); }
            set { }
        }
        //
        [CategoryAttribute("Endpoint")]
        [OrderedDisplayName(2, 10, "Access token")]
        [DescriptionAttribute("The token the client sends in an Authorization: Bearer header. It is made the first " +
                              "time the endpoint starts and then kept, so a client is configured once. Anything " +
                              "running as you can read it, exactly as anything running as you can drive PrePoMax; " +
                              "what it stops is a web page and another account on this computer. Replace it from " +
                              "Tools - Automation Server - Regenerate the access token.")]
        [ReadOnly(true)]
        [Id(3, 2)]
        public string AccessToken
        {
            get { return _automationSettings.AccessToken; }
            set { }
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
