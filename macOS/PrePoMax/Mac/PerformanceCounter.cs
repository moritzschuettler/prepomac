// PrePoMax for macOS - the Windows performance counter "Memory", "Available MBytes" that the MMG runner asks for the memory
// limit of the mesher. macOS has no performance counters, so the value comes from the memory load of the system.
using System;

namespace System.Diagnostics
{
    public class PerformanceCounter : IDisposable
    {
        private readonly string _category;
        private readonly string _counter;

        public PerformanceCounter(string categoryName, string counterName)
        {
            _category = categoryName;
            _counter = counterName;
        }
        public float NextValue()
        {
            if (_category == "Memory" && _counter == "Available MBytes")
            {
                GCMemoryInfo info = GC.GetGCMemoryInfo();
                long available = info.TotalAvailableMemoryBytes - info.MemoryLoadBytes;
                if (available <= 0) available = info.TotalAvailableMemoryBytes / 2;
                return available / (1024f * 1024f);
            }
            throw new NotSupportedException("The performance counter " + _category + "/" + _counter +
                                            " does not exist on macOS.");
        }
        public void Dispose() { }
    }
}
