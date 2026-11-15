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
using System.Drawing;
using System.IO;
using System.Reflection;

namespace CaeGlobals
{
    // Maps the stable numbers of [PmxType] types to the types and back, and names the framework types a file may
    // carry by their full names
    public static class PmxTypeRegistry
    {
        // Variables                                                                                                                
        private static readonly object _lock = new object();
        private static readonly Dictionary<int, Type> _typeById = new Dictionary<int, Type>();
        private static readonly Dictionary<Type, int> _idByType = new Dictionary<Type, int>();
        private static readonly HashSet<Assembly> _scannedAssemblies = new HashSet<Assembly>();
        private static readonly Dictionary<string, Type> _frameworkTypeByName = CreateFrameworkTypes();
        private static bool _initialized = false;
        private static bool _referencedAssembliesLoaded = false;


        // Methods                                                                                                                  
        private static Dictionary<string, Type> CreateFrameworkTypes()
        {
            Type[] types = new Type[]
            {
                typeof(object), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int),
                typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(char),
                typeof(string), typeof(DateTime), typeof(TimeSpan), typeof(Guid), typeof(Type), typeof(FileInfo),
                typeof(Color), typeof(Size), typeof(SizeF), typeof(Point), typeof(PointF), typeof(Rectangle),
                typeof(RectangleF), typeof(List<>), typeof(Dictionary<,>), typeof(HashSet<>), typeof(LinkedList<>),
                typeof(Queue<>), typeof(Stack<>), typeof(SortedDictionary<,>), typeof(KeyValuePair<,>), typeof(Nullable<>),
                typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>)
            };
            Dictionary<string, Type> typeByName = new Dictionary<string, Type>();
            foreach (var type in types) typeByName.Add(type.FullName, type);
            return typeByName;
        }
        private static void Initialize()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Scan(assembly);
                _initialized = true;
            }
        }
        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            lock (_lock) Scan(args.LoadedAssembly);
        }
        private static void Scan(Assembly assembly)
        {
            if (assembly.IsDynamic || !_scannedAssemblies.Add(assembly)) return;
            // Only an assembly that references this one can declare a stored type
            if (!ReferencesThisAssembly(assembly)) return;
            //
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            //
            PmxTypeAttribute attribute;
            Type existing;
            foreach (var type in types)
            {
                if (type == null) continue;
                attribute = type.GetCustomAttribute<PmxTypeAttribute>(false);
                if (attribute == null) continue;
                //
                if (_typeById.TryGetValue(attribute.Id, out existing) && existing != type)
                    throw new InvalidOperationException("The stored type number " + attribute.Id + " is given to both " +
                                                        existing.FullName + " and " + type.FullName + ".");
                _typeById[attribute.Id] = type;
                _idByType[type] = attribute.Id;
            }
        }
        private static void LoadReferencedAssemblies()
        {
            if (_referencedAssembliesLoaded) return;
            _referencedAssembliesLoaded = true;
            //
            Assembly entry = Assembly.GetEntryAssembly();
            if (entry == null) return;
            //
            Queue<Assembly> queue = new Queue<Assembly>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(entry);
            while (queue.Count > 0)
            {
                Assembly assembly = queue.Dequeue();
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (!visited.Add(reference.FullName)) continue;
                    try
                    {
                        queue.Enqueue(Assembly.Load(reference));
                    }
                    catch
                    {
                    }
                }
            }
        }
        public static bool TryGetId(Type type, out int id)
        {
            Initialize();
            lock (_lock) return _idByType.TryGetValue(type, out id);
        }
        public static Type GetType(int id)
        {
            Initialize();
            Type type;
            lock (_lock)
            {
                if (_typeById.TryGetValue(id, out type)) return type;
            }
            LoadReferencedAssemblies();
            lock (_lock)
            {
                if (_typeById.TryGetValue(id, out type)) return type;
            }
            throw new PmxStorageException("The file holds an item of the unknown stored type number " + id + ". It was " +
                                          "written by a newer version of PrePoMax, or holds a type this version no " +
                                          "longer has.");
        }
        // A name from a file resolves only to a framework type of the list, an enumeration or a primitive, or a type of
        // an assembly that references this one, so that a file cannot make the reader create an arbitrary type
        public static bool TryGetFrameworkType(string name, out Type type)
        {
            if (_frameworkTypeByName.TryGetValue(name, out type)) return true;
            type = Type.GetType(name, false);
            if (type == null) return false;
            if (type.IsEnum || type.IsPrimitive || type.IsInterface) return true;
            Initialize();
            lock (_lock)
            {
                if (_scannedAssemblies.Contains(type.Assembly) && ReferencesThisAssembly(type.Assembly)) return true;
            }
            type = null;
            return false;
        }
        private static bool ReferencesThisAssembly(Assembly assembly)
        {
            if (assembly == typeof(PmxTypeRegistry).Assembly) return true;
            string name = typeof(PmxTypeRegistry).Assembly.GetName().Name;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == name) return true;
            }
            return false;
        }
        public static KeyValuePair<int, Type>[] GetAllTypes()
        {
            Initialize();
            LoadReferencedAssemblies();
            lock (_lock)
            {
                List<KeyValuePair<int, Type>> all = new List<KeyValuePair<int, Type>>(_typeById);
                all.Sort((a, b) => a.Key.CompareTo(b.Key));
                return all.ToArray();
            }
        }
    }
}
