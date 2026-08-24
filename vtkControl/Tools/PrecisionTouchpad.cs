// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace vtkControl
{
    public struct TouchContact
    {
        public int Id;
        public double X;
        public double Y;
    }

    internal class PrecisionTouchpad
    {
        // Variables
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;
        private const int RIDI_DEVICEINFO = 0x2000000B;
        private const int RIDI_PREPARSEDDATA = 0x20000005;
        private const int RIM_TYPEHID = 2;
        private const int HIDP_INPUT = 0;
        private const int HIDP_STATUS_SUCCESS = 0x00110000;
        // Field offsets into the native HIDP_CAPS and HIDP_VALUE_CAPS structures
        private const int CAPS_SIZE = 64;
        private const int CAPS_NUMBER_INPUT_VALUE_CAPS = 48;
        private const int VALUE_CAPS_SIZE = 76;
        private const int VALUE_CAPS_USAGE_PAGE = 0;
        private const int VALUE_CAPS_LINK_COLLECTION = 6;
        private const int VALUE_CAPS_IS_RANGE = 12;
        private const int VALUE_CAPS_UNITS_EXP = 32;
        private const int VALUE_CAPS_LOGICAL_MIN = 40;
        private const int VALUE_CAPS_LOGICAL_MAX = 44;
        private const int VALUE_CAPS_PHYSICAL_MIN = 48;
        private const int VALUE_CAPS_PHYSICAL_MAX = 52;
        private const int VALUE_CAPS_USAGE = 56;
        //
        private const ushort USAGE_PAGE_GENERIC = 0x01;
        private const ushort USAGE_PAGE_DIGITIZER = 0x0D;
        private const ushort USAGE_TOUCH_PAD = 0x05;
        private const ushort USAGE_X = 0x30;
        private const ushort USAGE_Y = 0x31;
        private const ushort USAGE_TIP_SWITCH = 0x42;
        private const ushort USAGE_CONTACT_IDENTIFIER = 0x51;
        private const ushort USAGE_CONTACT_COUNT = 0x54;
        // A touchpad reports a physical size that is used to express the contacts in millimetres.
        // Values outside these bounds mean the descriptor could not be trusted and are replaced.
        private const double _minPadSizeMm = 30;
        private const double _maxPadSizeMm = 250;
        private const double _defaultPadWidthMm = 100;
        private const double _defaultPadHeightMm = 60;
        //
        private bool _found;
        private byte[] _rawInputBuffer;
        private Dictionary<IntPtr, TouchpadDevice> _devices;
        private List<TouchContact> _contacts;


        // Properties
        public bool Found { get { return _found; } }


        // Constructors
        public PrecisionTouchpad()
        {
            _found = false;
            _rawInputBuffer = new byte[1024];
            _devices = new Dictionary<IntPtr, TouchpadDevice>();
            _contacts = new List<TouchContact>();
            //
            _found = FindTouchpadDevice();
        }


        // Methods
        public bool Register(IntPtr windowHandle)
        {
            if (!_found || windowHandle == IntPtr.Zero) return false;
            //
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].UsagePage = USAGE_PAGE_DIGITIZER;
            devices[0].Usage = USAGE_TOUCH_PAD;
            devices[0].Flags = RIDEV_INPUTSINK;
            devices[0].WindowHandle = windowHandle;
            //
            return RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }
        public TouchContact[] GetContacts(IntPtr rawInputHandle)
        {
            int size = _rawInputBuffer.Length;
            int read = GetRawInputData(rawInputHandle, RID_INPUT, _rawInputBuffer, ref size,
                                       Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            if (read <= 0)
            {
                // The report did not fit - ask for the required size and grow the buffer
                size = 0;
                if (GetRawInputData(rawInputHandle, RID_INPUT, null, ref size,
                                    Marshal.SizeOf(typeof(RAWINPUTHEADER))) != 0 || size <= 0) return null;
                _rawInputBuffer = new byte[size];
                read = GetRawInputData(rawInputHandle, RID_INPUT, _rawInputBuffer, ref size,
                                       Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                if (read <= 0) return null;
            }
            // RAWINPUTHEADER is 24 bytes on x64 and is followed by RAWHID: dwSizeHid, dwCount, bRawData
            if (BitConverter.ToInt32(_rawInputBuffer, 0) != RIM_TYPEHID) return null;
            IntPtr deviceHandle = new IntPtr(BitConverter.ToInt64(_rawInputBuffer, 8));
            int reportSize = BitConverter.ToInt32(_rawInputBuffer, 24);
            int reportCount = BitConverter.ToInt32(_rawInputBuffer, 28);
            if (reportSize <= 0 || reportCount <= 0) return null;
            //
            TouchpadDevice device = GetDevice(deviceHandle);
            if (device == null)
            {
                TrackpadTrace.Write("no usable touchpad device for this report");
                return null;
            }
            //
            _contacts.Clear();
            byte[] report = new byte[reportSize];
            for (int i = 0; i < reportCount; i++)
            {
                int offset = 32 + i * reportSize;
                if (offset + reportSize > _rawInputBuffer.Length) break;
                Buffer.BlockCopy(_rawInputBuffer, offset, report, 0, reportSize);
                AddContactsFromReport(device, report);
            }
            //
            TrackpadTrace.Write("report size " + reportSize + " x " + reportCount + " - contacts = " +
                                _contacts.Count);
            //
            return _contacts.ToArray();
        }
        private void AddContactsFromReport(TouchpadDevice device, byte[] report)
        {
            uint value;
            int contactCount = int.MaxValue;
            if (HidP_GetUsageValue(HIDP_INPUT, USAGE_PAGE_DIGITIZER, 0, USAGE_CONTACT_COUNT, out value,
                                   device.PreparsedData, report, report.Length) == HIDP_STATUS_SUCCESS &&
                value > 0) contactCount = (int)value;
            //
            foreach (ushort linkCollection in device.LinkCollections)
            {
                if (_contacts.Count >= contactCount) break;
                // A contact is only reported while the tip switch is set
                int usageLength = device.UsageList.Length;
                if (HidP_GetUsages(HIDP_INPUT, USAGE_PAGE_DIGITIZER, linkCollection, device.UsageList,
                                   ref usageLength, device.PreparsedData, report, report.Length) !=
                    HIDP_STATUS_SUCCESS) continue;
                //
                bool tipSwitch = false;
                for (int i = 0; i < usageLength; i++)
                {
                    if (device.UsageList[i] == USAGE_TIP_SWITCH) { tipSwitch = true; break; }
                }
                if (!tipSwitch) continue;
                //
                uint x;
                uint y;
                if (HidP_GetUsageValue(HIDP_INPUT, USAGE_PAGE_GENERIC, linkCollection, USAGE_X, out x,
                                       device.PreparsedData, report, report.Length) != HIDP_STATUS_SUCCESS) continue;
                if (HidP_GetUsageValue(HIDP_INPUT, USAGE_PAGE_GENERIC, linkCollection, USAGE_Y, out y,
                                       device.PreparsedData, report, report.Length) != HIDP_STATUS_SUCCESS) continue;
                //
                uint id;
                if (HidP_GetUsageValue(HIDP_INPUT, USAGE_PAGE_DIGITIZER, linkCollection, USAGE_CONTACT_IDENTIFIER,
                                       out id, device.PreparsedData, report, report.Length) !=
                    HIDP_STATUS_SUCCESS) id = (uint)linkCollection;
                //
                TouchContact contact = new TouchContact();
                contact.Id = (int)id;
                contact.X = x * device.MmPerUnitX;
                contact.Y = y * device.MmPerUnitY;
                _contacts.Add(contact);
            }
        }
        private TouchpadDevice GetDevice(IntPtr deviceHandle)
        {
            TouchpadDevice device;
            if (_devices.TryGetValue(deviceHandle, out device)) return device;
            //
            device = TouchpadDevice.Create(deviceHandle);
            _devices.Add(deviceHandle, device);
            return device;
        }
        private static bool FindTouchpadDevice()
        {
            uint count = 0;
            int listSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
            if (GetRawInputDeviceList(null, ref count, (uint)listSize) != 0 || count == 0) return false;
            //
            RAWINPUTDEVICELIST[] list = new RAWINPUTDEVICELIST[count];
            if (GetRawInputDeviceList(list, ref count, (uint)listSize) == uint.MaxValue) return false;
            //
            TrackpadTrace.Write("raw input devices = " + count);
            for (int i = 0; i < count; i++)
            {
                if (list[i].Type != RIM_TYPEHID) continue;
                //
                byte[] info = new byte[32];
                int size = info.Length;
                BitConverter.GetBytes(size).CopyTo(info, 0);
                if (GetRawInputDeviceInfo(list[i].Device, RIDI_DEVICEINFO, info, ref size) <= 0)
                {
                    TrackpadTrace.Write("  hid device " + i + " - device info failed");
                    continue;
                }
                // RID_DEVICE_INFO: cbSize, dwType, then the HID union at offset 8
                if (BitConverter.ToInt32(info, 4) != RIM_TYPEHID) continue;
                //
                ushort usagePage = BitConverter.ToUInt16(info, 20);
                ushort usage = BitConverter.ToUInt16(info, 22);
                TrackpadTrace.Write("  hid device " + i + " - usage page 0x" + usagePage.ToString("X2") +
                                    ", usage 0x" + usage.ToString("X2"));
                if (usagePage == USAGE_PAGE_DIGITIZER && usage == USAGE_TOUCH_PAD)
                {
                    TrackpadTrace.Write("  precision touchpad found");
                    return true;
                }
            }
            //
            TrackpadTrace.Write("no precision touchpad among the raw input devices");
            return false;
        }


        // Nested classes
        private class TouchpadDevice
        {
            public IntPtr PreparsedData;
            public ushort[] LinkCollections;
            public ushort[] UsageList;
            public double MmPerUnitX;
            public double MmPerUnitY;

            public static TouchpadDevice Create(IntPtr deviceHandle)
            {
                int size = 0;
                if (GetRawInputDeviceInfo(deviceHandle, RIDI_PREPARSEDDATA, null, ref size) != 0 || size <= 0)
                {
                    TrackpadTrace.Write("preparsed data size query failed");
                    return null;
                }
                //
                IntPtr preparsedData = Marshal.AllocHGlobal(size);
                if (GetRawInputDeviceInfo(deviceHandle, RIDI_PREPARSEDDATA, preparsedData, ref size) <= 0)
                {
                    TrackpadTrace.Write("preparsed data query failed");
                    Marshal.FreeHGlobal(preparsedData);
                    return null;
                }
                //
                byte[] caps = new byte[CAPS_SIZE];
                int status = HidP_GetCaps(preparsedData, caps);
                if (status != HIDP_STATUS_SUCCESS)
                {
                    TrackpadTrace.Write("HidP_GetCaps failed with 0x" + status.ToString("X8"));
                    Marshal.FreeHGlobal(preparsedData);
                    return null;
                }
                //
                ushort valueCapsLength = BitConverter.ToUInt16(caps, CAPS_NUMBER_INPUT_VALUE_CAPS);
                TrackpadTrace.Write("input value caps = " + valueCapsLength);
                if (valueCapsLength == 0)
                {
                    Marshal.FreeHGlobal(preparsedData);
                    return null;
                }
                //
                byte[] valueCaps = new byte[valueCapsLength * VALUE_CAPS_SIZE];
                status = HidP_GetValueCaps(HIDP_INPUT, valueCaps, ref valueCapsLength, preparsedData);
                if (status != HIDP_STATUS_SUCCESS)
                {
                    TrackpadTrace.Write("HidP_GetValueCaps failed with 0x" + status.ToString("X8"));
                    Marshal.FreeHGlobal(preparsedData);
                    return null;
                }
                // Every contact sits in its own link collection - the ones holding an X axis identify them
                List<ushort> linkCollections = new List<ushort>();
                double mmPerUnitX = 0;
                double mmPerUnitY = 0;
                for (int i = 0; i < valueCapsLength; i++)
                {
                    int offset = i * VALUE_CAPS_SIZE;
                    if (BitConverter.ToUInt16(valueCaps, offset + VALUE_CAPS_USAGE_PAGE) != USAGE_PAGE_GENERIC)
                        continue;
                    if (valueCaps[offset + VALUE_CAPS_IS_RANGE] != 0) continue;
                    //
                    ushort usage = BitConverter.ToUInt16(valueCaps, offset + VALUE_CAPS_USAGE);
                    ushort linkCollection = BitConverter.ToUInt16(valueCaps, offset + VALUE_CAPS_LINK_COLLECTION);
                    //
                    if (usage == USAGE_X)
                    {
                        if (!linkCollections.Contains(linkCollection)) linkCollections.Add(linkCollection);
                        if (mmPerUnitX == 0) mmPerUnitX = GetMmPerUnit(valueCaps, offset, _defaultPadWidthMm);
                    }
                    else if (usage == USAGE_Y)
                    {
                        if (mmPerUnitY == 0) mmPerUnitY = GetMmPerUnit(valueCaps, offset, _defaultPadHeightMm);
                    }
                }
                if (linkCollections.Count == 0)
                {
                    TrackpadTrace.Write("no contact collection carries an x axis");
                    Marshal.FreeHGlobal(preparsedData);
                    return null;
                }
                //
                int usageListLength = HidP_MaxUsageListLength(HIDP_INPUT, USAGE_PAGE_DIGITIZER, preparsedData);
                if (usageListLength <= 0) usageListLength = 16;
                //
                TrackpadTrace.Write("contact collections = " + linkCollections.Count + ", mm per unit = " +
                                    mmPerUnitX + " " + mmPerUnitY + ", usage list = " + usageListLength);
                //
                TouchpadDevice device = new TouchpadDevice();
                device.PreparsedData = preparsedData;
                device.LinkCollections = linkCollections.ToArray();
                device.UsageList = new ushort[usageListLength];
                device.MmPerUnitX = mmPerUnitX;
                device.MmPerUnitY = mmPerUnitY;
                return device;
            }
            private static double GetMmPerUnit(byte[] valueCaps, int offset, double defaultSizeMm)
            {
                double units = BitConverter.ToInt32(valueCaps, offset + VALUE_CAPS_LOGICAL_MAX) -
                               BitConverter.ToInt32(valueCaps, offset + VALUE_CAPS_LOGICAL_MIN);
                if (units <= 0) return 0;
                // The physical range is expressed in centimetres scaled by the unit exponent,
                // where exponents above 7 encode the negative values 8..15 as -8..-1
                double sizeMm = 0;
                double physical = BitConverter.ToInt32(valueCaps, offset + VALUE_CAPS_PHYSICAL_MAX) -
                                  BitConverter.ToInt32(valueCaps, offset + VALUE_CAPS_PHYSICAL_MIN);
                if (physical > 0)
                {
                    int exponent = (int)BitConverter.ToUInt32(valueCaps, offset + VALUE_CAPS_UNITS_EXP);
                    if (exponent > 7) exponent -= 16;
                    sizeMm = physical * 10 * Math.Pow(10, exponent);
                }
                if (sizeMm < _minPadSizeMm || sizeMm > _maxPadSizeMm) sizeMm = defaultSizeMm;
                //
                return sizeMm / units;
            }
        }


        // Interop
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public int Flags;
            public IntPtr WindowHandle;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICELIST
        {
            public IntPtr Device;
            public int Type;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public int Type;
            public int Size;
            public IntPtr Device;
            public IntPtr Param;
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] rawInputDevices, uint numDevices,
                                                           uint size);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceList([In, Out] RAWINPUTDEVICELIST[] rawInputDeviceList,
                                                         ref uint numDevices, uint size);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetRawInputDeviceInfo(IntPtr device, int command, [In, Out] byte[] data,
                                                        ref int size);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetRawInputDeviceInfo(IntPtr device, int command, IntPtr data, ref int size);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetRawInputData(IntPtr rawInput, int command, [In, Out] byte[] data, ref int size,
                                                  int headerSize);
        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, [In, Out] byte[] capabilities);
        [DllImport("hid.dll")]
        private static extern int HidP_GetValueCaps(int reportType, [In, Out] byte[] valueCaps, ref ushort length,
                                                    IntPtr preparsedData);
        [DllImport("hid.dll")]
        private static extern int HidP_GetUsageValue(int reportType, ushort usagePage, ushort linkCollection,
                                                     ushort usage, out uint usageValue, IntPtr preparsedData,
                                                     byte[] report, int reportLength);
        [DllImport("hid.dll")]
        private static extern int HidP_GetUsages(int reportType, ushort usagePage, ushort linkCollection,
                                                 [In, Out] ushort[] usageList, ref int usageLength,
                                                 IntPtr preparsedData, byte[] report, int reportLength);
        [DllImport("hid.dll")]
        private static extern int HidP_MaxUsageListLength(int reportType, ushort usagePage, IntPtr preparsedData);
    }
}
