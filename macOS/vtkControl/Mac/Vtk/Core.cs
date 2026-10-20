// PrePoMax for macOS - the core classes of the VTK compatible library the view is written against: objects with events,
// data arrays, points, id lists, cell arrays, matrices and transforms. The members follow ActiViz 5.8, the version of the
// Windows view.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kitware.VTK
{
    public class vtkObjectEventArgs : EventArgs
    {
        public vtkObject Caller;
        public uint EventId;
        public IntPtr CallData;

        public vtkObjectEventArgs(vtkObject caller, uint eventId, IntPtr callData)
        {
            Caller = caller;
            EventId = eventId;
            CallData = callData;
        }
    }

    // The event ids of vtkCommand
    public static class vtkCommandIds
    {
        public const uint AnyEvent = 1, DeleteEvent = 2, StartEvent = 3, EndEvent = 4, RenderEvent = 5, ProgressEvent = 6,
                          PickEvent = 7, StartPickEvent = 8, EndPickEvent = 9, AbortCheckEvent = 10, ExitEvent = 11,
                          LeftButtonPressEvent = 12, LeftButtonReleaseEvent = 13, MiddleButtonPressEvent = 14,
                          MiddleButtonReleaseEvent = 15, RightButtonPressEvent = 16, RightButtonReleaseEvent = 17,
                          EnterEvent = 18, LeaveEvent = 19, KeyPressEvent = 20, KeyReleaseEvent = 21, CharEvent = 22,
                          ExposeEvent = 23, ConfigureEvent = 24, TimerEvent = 25, MouseMoveEvent = 26,
                          MouseWheelForwardEvent = 27, MouseWheelBackwardEvent = 28, ResetCameraEvent = 29,
                          ResetCameraClippingRangeEvent = 30, ModifiedEvent = 31, StartInteractionEvent = 41,
                          InteractionEvent = 42, EndInteractionEvent = 43;
    }

    public partial class vtkObjectBase : IDisposable
    {
        private bool _disposed;

        public virtual void Dispose()
        {
            _disposed = true;
        }
        public bool IsDisposed { get { return _disposed; } }
        public virtual string GetClassName() { return GetType().Name; }
        public bool IsA(string name)
        {
            for (Type type = GetType(); type != null; type = type.BaseType) if (type.Name == name) return true;
            return false;
        }
        public void Register(vtkObjectBase o) { }
        public void UnRegister(vtkObjectBase o) { }
        public void FastDelete() { }
        public int GetReferenceCount() { return 1; }
    }

    public partial class vtkObject : vtkObjectBase
    {
        public delegate void vtkObjectEventHandler(vtkObject sender, vtkObjectEventArgs e);

        private static ulong _globalTime;
        private static bool _globalWarningDisplay = true;
        private ulong _mtime;
        private Dictionary<uint, vtkObjectEventHandler> _handlers;

        public vtkObject()
        {
            _mtime = ++_globalTime;
        }

        // Modification time
        public virtual void Modified()
        {
            _mtime = ++_globalTime;
            InvokeEvent(vtkCommandIds.ModifiedEvent, IntPtr.Zero);
        }
        public virtual uint GetMTime() { return (uint)_mtime; }
        internal ulong MTime { get { return _mtime; } }
        internal static ulong GlobalTime { get { return _globalTime; } }

        // Warnings
        public static void GlobalWarningDisplayOn() { _globalWarningDisplay = true; }
        public static void GlobalWarningDisplayOff() { _globalWarningDisplay = false; }
        public static void SetGlobalWarningDisplay(int value) { _globalWarningDisplay = value != 0; }
        public static int GetGlobalWarningDisplay() { return _globalWarningDisplay ? 1 : 0; }
        public void DebugOn() { }
        public void DebugOff() { }

        // Observers
        protected void AddHandler(uint eventId, vtkObjectEventHandler handler)
        {
            if (_handlers == null) _handlers = new Dictionary<uint, vtkObjectEventHandler>();
            _handlers.TryGetValue(eventId, out vtkObjectEventHandler existing);
            _handlers[eventId] = existing + handler;
        }
        protected void RemoveHandler(uint eventId, vtkObjectEventHandler handler)
        {
            if (_handlers == null || !_handlers.TryGetValue(eventId, out vtkObjectEventHandler existing)) return;
            existing -= handler;
            if (existing == null) _handlers.Remove(eventId);
            else _handlers[eventId] = existing;
        }
        public int HasObserver(uint eventId)
        {
            return _handlers != null && (_handlers.ContainsKey(eventId) || _handlers.ContainsKey(vtkCommandIds.AnyEvent))
                   ? 1 : 0;
        }
        public int HasObserver(string eventName) { return 0; }
        public virtual int InvokeEvent(uint eventId, IntPtr callData)
        {
            if (_handlers == null) return 0;
            vtkObjectEventArgs args = null;
            if (_handlers.TryGetValue(eventId, out vtkObjectEventHandler handler))
            {
                args = new vtkObjectEventArgs(this, eventId, callData);
                handler(this, args);
            }
            if (eventId != vtkCommandIds.AnyEvent && _handlers.TryGetValue(vtkCommandIds.AnyEvent, out handler))
                handler(this, args ?? new vtkObjectEventArgs(this, eventId, callData));
            return 0;
        }
        public int InvokeEvent(uint eventId) { return InvokeEvent(eventId, IntPtr.Zero); }
        public void RemoveAllObservers() { _handlers = null; }

        public event vtkObjectEventHandler AnyEvt
        { add { AddHandler(vtkCommandIds.AnyEvent, value); } remove { RemoveHandler(vtkCommandIds.AnyEvent, value); } }
        public event vtkObjectEventHandler ModifiedEvt
        { add { AddHandler(vtkCommandIds.ModifiedEvent, value); } remove { RemoveHandler(vtkCommandIds.ModifiedEvent, value); } }
        public event vtkObjectEventHandler StartEvt
        { add { AddHandler(vtkCommandIds.StartEvent, value); } remove { RemoveHandler(vtkCommandIds.StartEvent, value); } }
        public event vtkObjectEventHandler EndEvt
        { add { AddHandler(vtkCommandIds.EndEvent, value); } remove { RemoveHandler(vtkCommandIds.EndEvent, value); } }
        public event vtkObjectEventHandler RenderEvt
        { add { AddHandler(vtkCommandIds.RenderEvent, value); } remove { RemoveHandler(vtkCommandIds.RenderEvent, value); } }
        public event vtkObjectEventHandler ProgressEvt
        { add { AddHandler(vtkCommandIds.ProgressEvent, value); } remove { RemoveHandler(vtkCommandIds.ProgressEvent, value); } }
        public event vtkObjectEventHandler LeftButtonPressEvt
        {
            add { AddHandler(vtkCommandIds.LeftButtonPressEvent, value); }
            remove { RemoveHandler(vtkCommandIds.LeftButtonPressEvent, value); }
        }
        public event vtkObjectEventHandler LeftButtonReleaseEvt
        {
            add { AddHandler(vtkCommandIds.LeftButtonReleaseEvent, value); }
            remove { RemoveHandler(vtkCommandIds.LeftButtonReleaseEvent, value); }
        }
        public event vtkObjectEventHandler MiddleButtonPressEvt
        {
            add { AddHandler(vtkCommandIds.MiddleButtonPressEvent, value); }
            remove { RemoveHandler(vtkCommandIds.MiddleButtonPressEvent, value); }
        }
        public event vtkObjectEventHandler MiddleButtonReleaseEvt
        {
            add { AddHandler(vtkCommandIds.MiddleButtonReleaseEvent, value); }
            remove { RemoveHandler(vtkCommandIds.MiddleButtonReleaseEvent, value); }
        }
        public event vtkObjectEventHandler RightButtonPressEvt
        {
            add { AddHandler(vtkCommandIds.RightButtonPressEvent, value); }
            remove { RemoveHandler(vtkCommandIds.RightButtonPressEvent, value); }
        }
        public event vtkObjectEventHandler RightButtonReleaseEvt
        {
            add { AddHandler(vtkCommandIds.RightButtonReleaseEvent, value); }
            remove { RemoveHandler(vtkCommandIds.RightButtonReleaseEvent, value); }
        }
        public event vtkObjectEventHandler EnterEvt
        { add { AddHandler(vtkCommandIds.EnterEvent, value); } remove { RemoveHandler(vtkCommandIds.EnterEvent, value); } }
        public event vtkObjectEventHandler LeaveEvt
        { add { AddHandler(vtkCommandIds.LeaveEvent, value); } remove { RemoveHandler(vtkCommandIds.LeaveEvent, value); } }
        public event vtkObjectEventHandler KeyPressEvt
        { add { AddHandler(vtkCommandIds.KeyPressEvent, value); } remove { RemoveHandler(vtkCommandIds.KeyPressEvent, value); } }
        public event vtkObjectEventHandler KeyReleaseEvt
        {
            add { AddHandler(vtkCommandIds.KeyReleaseEvent, value); }
            remove { RemoveHandler(vtkCommandIds.KeyReleaseEvent, value); }
        }
        public event vtkObjectEventHandler CharEvt
        { add { AddHandler(vtkCommandIds.CharEvent, value); } remove { RemoveHandler(vtkCommandIds.CharEvent, value); } }
        public event vtkObjectEventHandler ConfigureEvt
        { add { AddHandler(vtkCommandIds.ConfigureEvent, value); } remove { RemoveHandler(vtkCommandIds.ConfigureEvent, value); } }
        public event vtkObjectEventHandler TimerEvt
        { add { AddHandler(vtkCommandIds.TimerEvent, value); } remove { RemoveHandler(vtkCommandIds.TimerEvent, value); } }
        public event vtkObjectEventHandler MouseMoveEvt
        { add { AddHandler(vtkCommandIds.MouseMoveEvent, value); } remove { RemoveHandler(vtkCommandIds.MouseMoveEvent, value); } }
        public event vtkObjectEventHandler MouseWheelForwardEvt
        {
            add { AddHandler(vtkCommandIds.MouseWheelForwardEvent, value); }
            remove { RemoveHandler(vtkCommandIds.MouseWheelForwardEvent, value); }
        }
        public event vtkObjectEventHandler MouseWheelBackwardEvt
        {
            add { AddHandler(vtkCommandIds.MouseWheelBackwardEvent, value); }
            remove { RemoveHandler(vtkCommandIds.MouseWheelBackwardEvent, value); }
        }
        public event vtkObjectEventHandler StartInteractionEvt
        {
            add { AddHandler(vtkCommandIds.StartInteractionEvent, value); }
            remove { RemoveHandler(vtkCommandIds.StartInteractionEvent, value); }
        }
        public event vtkObjectEventHandler InteractionEvt
        {
            add { AddHandler(vtkCommandIds.InteractionEvent, value); }
            remove { RemoveHandler(vtkCommandIds.InteractionEvent, value); }
        }
        public event vtkObjectEventHandler EndInteractionEvt
        {
            add { AddHandler(vtkCommandIds.EndInteractionEvent, value); }
            remove { RemoveHandler(vtkCommandIds.EndInteractionEvent, value); }
        }
    }

    // Arrays
    public partial class vtkAbstractArray
    {
        protected string _name;
        protected int _numberOfComponents = 1;

        public virtual void SetName(string name) { _name = name; }
        public string GetName() { return _name; }
        public virtual void SetNumberOfComponents(int n) { _numberOfComponents = Math.Max(1, n); }
        public int GetNumberOfComponents() { return _numberOfComponents; }
        public virtual long GetNumberOfTuples() { return 0; }
        public virtual long GetNumberOfValues() { return GetNumberOfTuples() * _numberOfComponents; }
        public virtual int GetDataType() { return 0; }
        public virtual int GetDataTypeSize() { return 8; }
        public virtual void Initialize() { }
        public virtual void Squeeze() { }
        public virtual void Reset() { }
        public virtual uint GetActualMemorySize() { return (uint)Math.Ceiling(GetNumberOfValues() * GetDataTypeSize() / 1024.0); }
        public long GetSize() { return GetNumberOfValues(); }
        public long GetMaxId() { return GetNumberOfValues() - 1; }
    }

    // A data array; the values are kept as doubles, the typed arrays convert on access
    public partial class vtkDataArray
    {
        protected double[] _data = new double[0];
        protected long _count;          // number of values
        private IntPtr _writePointer;
        private long _writeStart;
        private long _writeNumber;

        // Storage
        internal double[] RawData { get { return _data; } }
        internal long ValueCount { get { return _count; } }
        protected void EnsureCapacity(long values)
        {
            if (values <= _data.Length) return;
            long capacity = Math.Max(values, Math.Max(16, _data.Length * 2L));
            Array.Resize(ref _data, (int)capacity);
        }
        internal void SetValueCount(long values)
        {
            EnsureCapacity(values);
            if (values > _count) Array.Clear(_data, (int)_count, (int)(values - _count));
            _count = values;
        }
        internal void SetRaw(double[] data, long count)
        {
            _data = data;
            _count = count;
            Modified();
        }

        public override long GetNumberOfTuples() { return _count / _numberOfComponents; }
        public override long GetNumberOfValues() { return _count; }
        public void SetNumberOfTuples(long n) { SetValueCount(n * _numberOfComponents); }
        public void SetNumberOfValues(long n) { SetValueCount(n); }
        public override void Initialize() { _data = new double[0]; _count = 0; Modified(); }
        public override void Reset() { _count = 0; Modified(); }
        public override void Squeeze() { if (_data.Length != _count) Array.Resize(ref _data, (int)_count); }
        public void Allocate(long size, long ext) { EnsureCapacity(size); }
        public void Resize(long numTuples) { SetValueCount(numTuples * _numberOfComponents); }

        // Tuples
        public double GetTuple1(long i) { return _data[i * _numberOfComponents]; }
        public double[] GetTuple2(long i) { long k = i * _numberOfComponents; return new double[] { _data[k], _data[k + 1] }; }
        public double[] GetTuple3(long i)
        {
            long k = i * _numberOfComponents;
            return new double[] { _data[k], _data[k + 1], _data[k + 2] };
        }
        public double[] GetTuple4(long i)
        {
            long k = i * _numberOfComponents;
            return new double[] { _data[k], _data[k + 1], _data[k + 2], _data[k + 3] };
        }
        public double[] GetTuple(long i)
        {
            double[] tuple = new double[_numberOfComponents];
            Array.Copy(_data, i * _numberOfComponents, tuple, 0, _numberOfComponents);
            return tuple;
        }
        public void GetTuple(long i, double[] tuple) { Array.Copy(_data, i * _numberOfComponents, tuple, 0, _numberOfComponents); }
        public void SetTuple1(long i, double v) { _data[i * _numberOfComponents] = v; }
        public void SetTuple2(long i, double a, double b)
        {
            long k = i * _numberOfComponents;
            _data[k] = a; _data[k + 1] = b;
        }
        public void SetTuple3(long i, double a, double b, double c)
        {
            long k = i * _numberOfComponents;
            _data[k] = a; _data[k + 1] = b; _data[k + 2] = c;
        }
        public void SetTuple4(long i, double a, double b, double c, double d)
        {
            long k = i * _numberOfComponents;
            _data[k] = a; _data[k + 1] = b; _data[k + 2] = c; _data[k + 3] = d;
        }
        public void SetTuple(long i, double[] tuple) { Array.Copy(tuple, 0, _data, i * _numberOfComponents, _numberOfComponents); }
        public void InsertTuple1(long i, double v) { EnsureTuple(i); SetTuple1(i, v); }
        public void InsertTuple2(long i, double a, double b) { EnsureTuple(i); SetTuple2(i, a, b); }
        public void InsertTuple3(long i, double a, double b, double c) { EnsureTuple(i); SetTuple3(i, a, b, c); }
        public void InsertTuple4(long i, double a, double b, double c, double d) { EnsureTuple(i); SetTuple4(i, a, b, c, d); }
        public void InsertTuple(long i, double[] tuple) { EnsureTuple(i); SetTuple(i, tuple); }
        private void EnsureTuple(long i)
        {
            long needed = (i + 1) * _numberOfComponents;
            if (needed > _count) SetValueCount(needed);
        }
        public long InsertNextTuple1(double v) { long i = GetNumberOfTuples(); InsertTuple1(i, v); return i; }
        public long InsertNextTuple2(double a, double b) { long i = GetNumberOfTuples(); InsertTuple2(i, a, b); return i; }
        public long InsertNextTuple3(double a, double b, double c)
        {
            long i = GetNumberOfTuples();
            InsertTuple3(i, a, b, c);
            return i;
        }
        public long InsertNextTuple4(double a, double b, double c, double d)
        {
            long i = GetNumberOfTuples();
            InsertTuple4(i, a, b, c, d);
            return i;
        }
        public long InsertNextTuple(double[] tuple) { long i = GetNumberOfTuples(); InsertTuple(i, tuple); return i; }
        public double GetComponent(long i, int c) { return _data[i * _numberOfComponents + c]; }
        public void SetComponent(long i, int c, double v) { _data[i * _numberOfComponents + c] = v; }
        public void InsertComponent(long i, int c, double v) { EnsureTuple(i); SetComponent(i, c, v); }
        public void FillComponent(int c, double v)
        {
            long n = GetNumberOfTuples();
            for (long i = 0; i < n; i++) _data[i * _numberOfComponents + c] = v;
        }

        // Ranges
        public double[] GetRange() { return GetRange(0); }
        public double[] GetRange(int component)
        {
            double min = double.MaxValue;
            double max = -double.MaxValue;
            long n = GetNumberOfTuples();
            if (component < 0)
            {
                for (long i = 0; i < n; i++)
                {
                    double s = 0;
                    for (int c = 0; c < _numberOfComponents; c++)
                    {
                        double v = _data[i * _numberOfComponents + c];
                        s += v * v;
                    }
                    s = Math.Sqrt(s);
                    if (s < min) min = s;
                    if (s > max) max = s;
                }
            }
            else
            {
                for (long i = 0; i < n; i++)
                {
                    double v = _data[i * _numberOfComponents + component];
                    if (double.IsNaN(v)) continue;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
            if (min > max) { min = 0; max = 1; }
            return new double[] { min, max };
        }
        public void GetRange(double[] range, int component)
        {
            double[] r = GetRange(component);
            range[0] = r[0];
            range[1] = r[1];
        }
        public double GetMaxNorm() { return GetRange(-1)[1]; }

        // Copies
        public virtual void DeepCopy(vtkDataArray source)
        {
            if (source == null) return;
            _numberOfComponents = source._numberOfComponents;
            _name = source._name;
            _count = source._count;
            _data = new double[_count];
            Array.Copy(source._data, _data, _count);
            Modified();
        }
        public void ShallowCopy(vtkDataArray source) { DeepCopy(source); }
        public vtkDataArray NewInstance() { return (vtkDataArray)Activator.CreateInstance(GetType()); }
        public static vtkDataArray CreateDataArray(int dataType)
        {
            switch (dataType)
            {
                case 10: return new vtkFloatArray();
                case 6: return new vtkIntArray();
                case 12: return new vtkIdTypeArray();
                case 3: return new vtkUnsignedCharArray();
                case 9: return new vtkUnsignedLongArray();
                default: return new vtkDoubleArray();
            }
        }
        internal vtkDataArray CreateEmptyCopy()
        {
            vtkDataArray copy = NewInstance();
            copy._numberOfComponents = _numberOfComponents;
            copy._name = _name;
            return copy;
        }
        internal void CopyTupleFrom(vtkDataArray source, long sourceId, long targetId)
        {
            EnsureTuple(targetId);
            Array.Copy(source._data, sourceId * _numberOfComponents, _data, targetId * _numberOfComponents, _numberOfComponents);
        }
        internal long AppendTupleFrom(vtkDataArray source, long sourceId)
        {
            long id = GetNumberOfTuples();
            CopyTupleFrom(source, sourceId, id);
            return id;
        }
        public void InsertTuple(long i, long j, vtkAbstractArray source) { CopyTupleFrom((vtkDataArray)source, j, i); }
        public long InsertNextTuple(long j, vtkAbstractArray source) { return AppendTupleFrom((vtkDataArray)source, j); }

        // Raw memory, used to copy whole arrays from managed memory
        public IntPtr WriteVoidPointer(long id, long number)
        {
            FreeWritePointer();
            _writeStart = id;
            _writeNumber = number;
            _writePointer = Marshal.AllocHGlobal((IntPtr)Math.Max(1, number * GetDataTypeSize()));
            return _writePointer;
        }
        // A read only copy of the values, valid until the next call
        public IntPtr GetVoidPointer(long id)
        {
            if (_readPointer != IntPtr.Zero) Marshal.FreeHGlobal(_readPointer);
            long number = Math.Max(0, _count - id);
            _readPointer = Marshal.AllocHGlobal((IntPtr)Math.Max(1, number * GetDataTypeSize()));
            if (number > 0) WriteNative(_readPointer, id, number);
            return _readPointer;
        }
        private IntPtr _readPointer;
        public virtual void DataChanged()
        {
            if (_writePointer != IntPtr.Zero)
            {
                long values = _writeStart + _writeNumber;
                if (values > _count) SetValueCount(values);
                ReadNative(_writePointer, _writeStart, _writeNumber);
                FreeWritePointer();
            }
            Modified();
        }
        private void FreeWritePointer()
        {
            if (_writePointer != IntPtr.Zero) Marshal.FreeHGlobal(_writePointer);
            _writePointer = IntPtr.Zero;
        }
        protected virtual void ReadNative(IntPtr pointer, long start, long number)
        {
            double[] values = new double[number];
            Marshal.Copy(pointer, values, 0, (int)number);
            Array.Copy(values, 0, _data, start, number);
        }
        protected virtual void WriteNative(IntPtr pointer, long start, long number)
        {
            Marshal.Copy(_data, (int)start, pointer, (int)number);
        }
        ~vtkDataArray()
        {
            if (_writePointer != IntPtr.Zero) Marshal.FreeHGlobal(_writePointer);
            if (_readPointer != IntPtr.Zero) Marshal.FreeHGlobal(_readPointer);
        }
        // Lookup table of the scalars
        public void SetLookupTable(vtkLookupTable lut) { _lookupTable = lut; }
        public vtkLookupTable GetLookupTable() { return _lookupTable; }
        private vtkLookupTable _lookupTable;
    }

    public partial class vtkFloatArray
    {
        public override int GetDataType() { return 10; }
        public override int GetDataTypeSize() { return 4; }
        public float GetValue(long i) { return (float)_data[i]; }
        public void SetValue(long i, float v) { _data[i] = v; }
        public long InsertNextValue(float v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
        public void InsertValue(long i, float v) { if (i >= _count) SetValueCount(i + 1); _data[i] = v; }
        public float[] GetValueRange() { double[] r = GetRange(); return new float[] { (float)r[0], (float)r[1] }; }
        protected override void ReadNative(IntPtr pointer, long start, long number)
        {
            float[] values = new float[number];
            Marshal.Copy(pointer, values, 0, (int)number);
            for (long i = 0; i < number; i++) _data[start + i] = values[i];
        }
        protected override void WriteNative(IntPtr pointer, long start, long number)
        {
            float[] values = new float[number];
            for (long i = 0; i < number; i++) values[i] = (float)_data[start + i];
            Marshal.Copy(values, 0, pointer, (int)number);
        }
    }

    public partial class vtkDoubleArray
    {
        public override int GetDataType() { return 11; }
        public double GetValue(long i) { return _data[i]; }
        public void SetValue(long i, double v) { _data[i] = v; }
        public long InsertNextValue(double v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
        public void InsertValue(long i, double v) { if (i >= _count) SetValueCount(i + 1); _data[i] = v; }
    }

    public partial class vtkIntArray
    {
        public override int GetDataType() { return 6; }
        public override int GetDataTypeSize() { return 4; }
        public int GetValue(long i) { return (int)_data[i]; }
        public void SetValue(long i, int v) { _data[i] = v; }
        public long InsertNextValue(int v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
        public void InsertValue(long i, int v) { if (i >= _count) SetValueCount(i + 1); _data[i] = v; }
        protected override void ReadNative(IntPtr pointer, long start, long number)
        {
            int[] values = new int[number];
            Marshal.Copy(pointer, values, 0, (int)number);
            for (long i = 0; i < number; i++) _data[start + i] = values[i];
        }
        protected override void WriteNative(IntPtr pointer, long start, long number)
        {
            int[] values = new int[number];
            for (long i = 0; i < number; i++) values[i] = (int)_data[start + i];
            Marshal.Copy(values, 0, pointer, (int)number);
        }
    }

    public partial class vtkIdTypeArray
    {
        public override int GetDataType() { return 12; }
        public long GetValue(long i) { return (long)_data[i]; }
        public void SetValue(long i, long v) { _data[i] = v; }
        public long InsertNextValue(long v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
        public void InsertValue(long i, long v) { if (i >= _count) SetValueCount(i + 1); _data[i] = v; }
        protected override void ReadNative(IntPtr pointer, long start, long number)
        {
            long[] values = new long[number];
            Marshal.Copy(pointer, values, 0, (int)number);
            for (long i = 0; i < number; i++) _data[start + i] = values[i];
        }
        protected override void WriteNative(IntPtr pointer, long start, long number)
        {
            long[] values = new long[number];
            for (long i = 0; i < number; i++) values[i] = (long)_data[start + i];
            Marshal.Copy(values, 0, pointer, (int)number);
        }
    }

    public partial class vtkUnsignedCharArray
    {
        public override int GetDataType() { return 3; }
        public override int GetDataTypeSize() { return 1; }
        public byte GetValue(long i) { return (byte)_data[i]; }
        public void SetValue(long i, byte v) { _data[i] = v; }
        public long InsertNextValue(byte v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
        public void InsertValue(long i, byte v) { if (i >= _count) SetValueCount(i + 1); _data[i] = v; }
        protected override void ReadNative(IntPtr pointer, long start, long number)
        {
            byte[] values = new byte[number];
            Marshal.Copy(pointer, values, 0, (int)number);
            for (long i = 0; i < number; i++) _data[start + i] = values[i];
        }
        protected override void WriteNative(IntPtr pointer, long start, long number)
        {
            byte[] values = new byte[number];
            for (long i = 0; i < number; i++) values[i] = (byte)_data[start + i];
            Marshal.Copy(values, 0, pointer, (int)number);
        }
    }

    // The ids of the nodes and elements; unsigned long is 32 bits in the Windows build of VTK
    public partial class vtkUnsignedLongArray
    {
        public override int GetDataType() { return 9; }
        public override int GetDataTypeSize() { return 4; }
        public uint GetValue(long i) { return (uint)_data[i]; }
        public void SetValue(long i, uint v) { _data[i] = v; }
        public long InsertNextValue(uint v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
        public void InsertValue(long i, uint v) { if (i >= _count) SetValueCount(i + 1); _data[i] = v; }
        public static vtkUnsignedLongArray SafeDownCast(vtkObjectBase o) { return o as vtkUnsignedLongArray; }
        protected override void ReadNative(IntPtr pointer, long start, long number)
        {
            int[] values = new int[number];
            Marshal.Copy(pointer, values, 0, (int)number);
            for (long i = 0; i < number; i++) _data[start + i] = (uint)values[i];
        }
        protected override void WriteNative(IntPtr pointer, long start, long number)
        {
            int[] values = new int[number];
            for (long i = 0; i < number; i++) values[i] = unchecked((int)(uint)_data[start + i]);
            Marshal.Copy(values, 0, pointer, (int)number);
        }
    }

    public partial class vtkStringArray
    {
        private readonly List<string> _values = new List<string>();

        public override int GetDataType() { return 13; }
        public override long GetNumberOfTuples() { return _values.Count / _numberOfComponents; }
        public override long GetNumberOfValues() { return _values.Count; }
        public void SetNumberOfValues(long n)
        {
            while (_values.Count < n) _values.Add("");
            if (_values.Count > n) _values.RemoveRange((int)n, _values.Count - (int)n);
        }
        public string GetValue(long i) { return _values[(int)i]; }
        public void SetValue(long i, string v) { _values[(int)i] = v; }
        public long InsertNextValue(string v) { _values.Add(v); return _values.Count - 1; }
        public void InsertValue(long i, string v) { SetNumberOfValues(Math.Max(_values.Count, i + 1)); _values[(int)i] = v; }
        public override void Initialize() { _values.Clear(); }
    }

    // Id list
    public partial class vtkIdList
    {
        private long[] _ids = new long[0];
        private int _count;

        public long GetNumberOfIds() { return _count; }
        public void SetNumberOfIds(long n)
        {
            if (n > _ids.Length) Array.Resize(ref _ids, (int)Math.Max(n, _ids.Length * 2));
            _count = (int)n;
        }
        public long GetId(long i) { return _ids[i]; }
        public void SetId(long i, long id) { _ids[i] = id; }
        public void InsertId(long i, long id) { if (i >= _count) SetNumberOfIds(i + 1); _ids[i] = id; }
        public long InsertNextId(long id) { SetNumberOfIds(_count + 1); _ids[_count - 1] = id; return _count - 1; }
        public long InsertUniqueId(long id)
        {
            for (int i = 0; i < _count; i++) if (_ids[i] == id) return i;
            return InsertNextId(id);
        }
        public long IsId(long id)
        {
            for (int i = 0; i < _count; i++) if (_ids[i] == id) return i;
            return -1;
        }
        public void Reset() { _count = 0; }
        public void Initialize() { _count = 0; _ids = new long[0]; }
        public void Squeeze() { }
        public void Allocate(long size, int strategy) { if (size > _ids.Length) Array.Resize(ref _ids, (int)size); }
        public void DeepCopy(vtkIdList ids) { _ids = (long[])ids._ids.Clone(); _count = ids._count; }
        internal long[] ToArray() { long[] ids = new long[_count]; Array.Copy(_ids, ids, _count); return ids; }
        internal void SetIds(long[] ids) { _ids = (long[])ids.Clone(); _count = ids.Length; }
        internal void SetIds(int[] ids)
        {
            SetNumberOfIds(ids.Length);
            for (int i = 0; i < ids.Length; i++) _ids[i] = ids[i];
        }
    }

    // Points
    public partial class vtkPoints
    {
        private vtkDataArray _array;
        private double[] _bounds;
        private ulong _boundsTime;

        public vtkPoints()
        {
            _array = new vtkFloatArray();
            _array.SetNumberOfComponents(3);
        }

        internal double[] Raw { get { return _array.RawData; } }
        internal int Count { get { return (int)_array.GetNumberOfTuples(); } }

        public vtkDataArray GetData() { return _array; }
        public void SetData(vtkDataArray data)
        {
            if (data.GetNumberOfComponents() != 3) data.SetNumberOfComponents(3);
            _array = data;
            Modified();
        }
        public int GetDataType() { return _array.GetDataType(); }
        public void SetDataTypeToDouble()
        {
            if (_array is vtkDoubleArray) return;
            vtkDoubleArray array = new vtkDoubleArray();
            array.SetNumberOfComponents(3);
            array.DeepCopy(_array);
            _array = array;
        }
        public void SetDataTypeToFloat() { }
        public void SetDataType(int type) { if (type == 11) SetDataTypeToDouble(); }
        public long GetNumberOfPoints() { return _array.GetNumberOfTuples(); }
        public void SetNumberOfPoints(long n) { _array.SetNumberOfTuples(n); }
        public double[] GetPoint(long i) { return _array.GetTuple3(i); }
        public void GetPoint(long i, double[] x)
        {
            double[] d = _array.RawData;
            long k = 3 * i;
            x[0] = d[k]; x[1] = d[k + 1]; x[2] = d[k + 2];
        }
        public void GetPoint(long i, IntPtr x) { Marshal.Copy(GetPoint(i), 0, x, 3); }
        public void SetPoint(long i, double x, double y, double z) { _array.SetTuple3(i, x, y, z); }
        public void SetPoint(long i, double[] x) { _array.SetTuple3(i, x[0], x[1], x[2]); }
        public void InsertPoint(long i, double x, double y, double z) { _array.InsertTuple3(i, x, y, z); }
        public void InsertPoint(long i, double[] x) { _array.InsertTuple3(i, x[0], x[1], x[2]); }
        public long InsertNextPoint(double x, double y, double z) { return _array.InsertNextTuple3(x, y, z); }
        public long InsertNextPoint(double[] x) { return _array.InsertNextTuple3(x[0], x[1], x[2]); }
        public void Reset() { _array.Reset(); }
        public void Initialize() { _array.Initialize(); }
        public void Squeeze() { _array.Squeeze(); }
        public void Allocate(long size, long ext) { _array.Allocate(3 * size, ext); }
        public void DeepCopy(vtkPoints points)
        {
            vtkDataArray array = points._array.NewInstance();
            array.SetNumberOfComponents(3);
            array.DeepCopy(points._array);
            _array = array;
            Modified();
        }
        public void ShallowCopy(vtkPoints points) { _array = points._array; Modified(); }
        public uint GetActualMemorySize() { return _array.GetActualMemorySize(); }
        public override uint GetMTime() { return Math.Max(base.GetMTime(), _array.GetMTime()); }
        internal ulong DataTime { get { return Math.Max(MTime, _array.MTime); } }
        public double[] GetBounds()
        {
            if (_bounds != null && _boundsTime == DataTime) return (double[])_bounds.Clone();
            double[] b = new double[] { double.MaxValue, -double.MaxValue, double.MaxValue, -double.MaxValue,
                                        double.MaxValue, -double.MaxValue };
            double[] d = _array.RawData;
            long n = GetNumberOfPoints();
            for (long i = 0; i < n; i++)
            {
                long k = 3 * i;
                for (int c = 0; c < 3; c++)
                {
                    double v = d[k + c];
                    if (v < b[2 * c]) b[2 * c] = v;
                    if (v > b[2 * c + 1]) b[2 * c + 1] = v;
                }
            }
            if (n == 0) b = new double[] { 1, -1, 1, -1, 1, -1 };
            _bounds = b;
            _boundsTime = DataTime;
            return (double[])b.Clone();
        }
        public void ComputeBounds() { GetBounds(); }
    }

    // Cell array: the connectivity of the vertices, lines, polygons or strips of a poly data
    public partial class vtkCellArray
    {
        // Offsets into the connectivity; cell i has the point ids _connectivity[_offsets[i] .. _offsets[i + 1])
        private List<long> _offsets = new List<long> { 0 };
        private List<long> _connectivity = new List<long>();
        private long _traversal;

        internal int Count { get { return _offsets.Count - 1; } }
        internal List<long> Offsets { get { return _offsets; } }
        internal List<long> Connectivity { get { return _connectivity; } }
        internal int CellSize(int i) { return (int)(_offsets[i + 1] - _offsets[i]); }
        internal long CellStart(int i) { return _offsets[i]; }

        public long GetNumberOfCells() { return Count; }
        public long InsertNextCell(long npts)
        {
            // The point ids follow with InsertCellPoint
            _offsets.Add(_offsets[_offsets.Count - 1]);
            return Count - 1;
        }
        public void InsertCellPoint(long id)
        {
            _connectivity.Add(id);
            _offsets[_offsets.Count - 1]++;
            Modified();
        }
        public long InsertNextCell(vtkIdList ids)
        {
            long n = ids.GetNumberOfIds();
            for (long i = 0; i < n; i++) _connectivity.Add(ids.GetId(i));
            _offsets.Add(_connectivity.Count);
            Modified();
            return Count - 1;
        }
        public long InsertNextCell(long npts, long[] ids)
        {
            for (long i = 0; i < npts; i++) _connectivity.Add(ids[i]);
            _offsets.Add(_connectivity.Count);
            Modified();
            return Count - 1;
        }
        public long InsertNextCell(vtkCell cell) { return InsertNextCell(cell.GetPointIds()); }
        internal long InsertNextCell(int[] ids)
        {
            foreach (int id in ids) _connectivity.Add(id);
            _offsets.Add(_connectivity.Count);
            return Count - 1;
        }
        internal long InsertNextCell(long[] ids)
        {
            _connectivity.AddRange(ids);
            _offsets.Add(_connectivity.Count);
            return Count - 1;
        }
        public void UpdateCellCount(long npts) { }
        public void SetNumberOfCells(long n) { }
        public void Reset() { _offsets = new List<long> { 0 }; _connectivity.Clear(); Modified(); }
        public void Initialize() { Reset(); }
        public void Squeeze() { }
        public void Allocate(long size, long ext) { }
        public long EstimateSize(long numCells, int maxPtsPerCell) { return numCells * (1 + maxPtsPerCell); }
        public long GetNumberOfConnectivityEntries() { return _connectivity.Count + Count; }
        public long GetSize() { return GetNumberOfConnectivityEntries(); }
        public void InitTraversal() { _traversal = 0; }
        public int GetNextCell(vtkIdList pts)
        {
            if (_traversal >= Count) return 0;
            GetCell((int)_traversal++, pts);
            return 1;
        }
        internal void GetCell(int i, vtkIdList pts)
        {
            long start = _offsets[i];
            int n = CellSize(i);
            pts.SetNumberOfIds(n);
            for (int j = 0; j < n; j++) pts.SetId(j, _connectivity[(int)(start + j)]);
        }
        internal long[] GetCell(int i)
        {
            long start = _offsets[i];
            int n = CellSize(i);
            long[] ids = new long[n];
            for (int j = 0; j < n; j++) ids[j] = _connectivity[(int)(start + j)];
            return ids;
        }
        public void DeepCopy(vtkCellArray ca)
        {
            _offsets = new List<long>(ca._offsets);
            _connectivity = new List<long>(ca._connectivity);
            Modified();
        }
        public void ReverseCell(long i)
        {
            long start = _offsets[(int)i];
            int n = CellSize((int)i);
            _connectivity.Reverse((int)start, n);
        }
        // The legacy layout: npts, id0, id1, ... for every cell
        public vtkIdTypeArray GetData()
        {
            vtkIdTypeArray data = new vtkIdTypeArray();
            for (int i = 0; i < Count; i++)
            {
                data.InsertNextValue(CellSize(i));
                long start = _offsets[i];
                for (int j = 0; j < CellSize(i); j++) data.InsertNextValue(_connectivity[(int)(start + j)]);
            }
            return data;
        }
        public void SetCells(long ncells, vtkIdTypeArray cells)
        {
            Reset();
            long k = 0;
            for (long c = 0; c < ncells; c++)
            {
                long n = cells.GetValue(k++);
                for (long j = 0; j < n; j++) _connectivity.Add(cells.GetValue(k++));
                _offsets.Add(_connectivity.Count);
            }
            Modified();
        }
        public uint GetActualMemorySize() { return (uint)((_connectivity.Count + _offsets.Count) * 8 / 1024); }
        public int GetMaxCellSize()
        {
            int max = 0;
            for (int i = 0; i < Count; i++) max = Math.Max(max, CellSize(i));
            return max;
        }
    }
}
