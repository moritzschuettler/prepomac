// PrePoMax for macOS: replaces GmshCommon (a C++/CLI wrapper of the gmsh C++ API that exists only on Windows).
// The classes and methods have the same names, signatures and behaviour as GmshCommon/GmshCommon.h, but call the gmsh C
// API of libgmsh.dylib through P/Invoke. Arrays returned by gmsh are allocated by gmsh and released with gmshFree.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GmshCommon
{
    public delegate double SizeCallback(int dim, int tag, double x, double y, double z, double lc);

    internal static unsafe class Native
    {
        public const string Lib = "gmsh";

        static Native()
        {
            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
        }

        public static void EnsureResolver() { }

        // libgmsh.dylib next to the application (app bundle), from GMSH_LIBRARY, or from Homebrew; libgmsh.so on Linux
        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != Lib) return IntPtr.Zero;
            List<string> candidates = new List<string>();
            string env = Environment.GetEnvironmentVariable("GMSH_LIBRARY");
            if (!string.IsNullOrEmpty(env)) candidates.Add(env);
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "libgmsh.dylib"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "..", "Frameworks", "libgmsh.dylib"));
            candidates.Add("/opt/homebrew/lib/libgmsh.dylib");
            candidates.Add("/usr/local/lib/libgmsh.dylib");
            // Linux (experimental): the library of the distribution or of the gmsh SDK
            string gmshDir = Environment.GetEnvironmentVariable("GMSH_DIR");
            if (!string.IsNullOrEmpty(gmshDir)) candidates.Add(Path.Combine(gmshDir, "lib", "libgmsh.so"));
            candidates.Add("/usr/lib/libgmsh.so");
            candidates.Add("/usr/local/lib/libgmsh.so");
            candidates.Add("/usr/lib/x86_64-linux-gnu/libgmsh.so");
            candidates.Add("/usr/lib/aarch64-linux-gnu/libgmsh.so");
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle)) return handle;
            }
            return IntPtr.Zero;
        }

        [DllImport(Lib)] public static extern void gmshFree(void* p);
        [DllImport(Lib)] public static extern void gmshInitialize(int argc, IntPtr argv, int readConfigFiles, int run, int* ierr);
        [DllImport(Lib)] public static extern int gmshIsInitialized(int* ierr);
        [DllImport(Lib)] public static extern void gmshFinalize(int* ierr);
        [DllImport(Lib)] public static extern void gmshOpen(byte[] fileName, int* ierr);
        [DllImport(Lib)] public static extern void gmshMerge(byte[] fileName, int* ierr);
        [DllImport(Lib)] public static extern void gmshWrite(byte[] fileName, int* ierr);
        [DllImport(Lib)] public static extern void gmshClear(int* ierr);
        // Option
        [DllImport(Lib)] public static extern void gmshOptionSetNumber(byte[] name, double value, int* ierr);
        [DllImport(Lib)] public static extern void gmshOptionGetNumber(byte[] name, double* value, int* ierr);
        [DllImport(Lib)] public static extern void gmshOptionSetString(byte[] name, byte[] value, int* ierr);
        [DllImport(Lib)] public static extern void gmshOptionGetString(byte[] name, IntPtr* value, int* ierr);
        // Model
        [DllImport(Lib)] public static extern void gmshModelAdd(byte[] name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetCurrent(IntPtr* name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelSetCurrent(byte[] name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetEntities(int** dimTags, nuint* dimTags_n, int dim, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelSetEntityName(int dim, int tag, byte[] name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetEntityName(int dim, int tag, IntPtr* name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetPhysicalGroups(int** dimTags, nuint* dimTags_n, int dim, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetEntitiesForPhysicalGroup(int dim, int tag, int** tags, nuint* tags_n, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelAddPhysicalGroup(int dim, int[] tags, nuint tags_n, int tag, byte[] name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelRemovePhysicalGroups(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelSetPhysicalName(int dim, int tag, byte[] name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetPhysicalName(int dim, int tag, IntPtr* name, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetBoundary(int[] dimTags, nuint dimTags_n, int** outDimTags, nuint* outDimTags_n, int combined, int oriented, int recursive, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetAdjacencies(int dim, int tag, int** upward, nuint* upward_n, int** downward, nuint* downward_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetBoundingBox(int dim, int tag, double* xmin, double* ymin, double* zmin, double* xmax, double* ymax, double* zmax, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelAddDiscreteEntity(int dim, int tag, int[] boundary, nuint boundary_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelRemoveEntities(int[] dimTags, nuint dimTags_n, int recursive, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelSetVisibility(int[] dimTags, nuint dimTags_n, int value, int recursive, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetType(int dim, int tag, IntPtr* entityType, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetValue(int dim, int tag, double[] parametricCoord, nuint parametricCoord_n, double** coord, nuint* coord_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetNormal(int tag, double[] parametricCoord, nuint parametricCoord_n, double** normals, nuint* normals_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetParametrization(int dim, int tag, double[] coord, nuint coord_n, double** parametricCoord, nuint* parametricCoord_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetParametrizationBounds(int dim, int tag, double** min, nuint* min_n, double** max, nuint* max_n, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelIsInside(int dim, int tag, double[] coord, nuint coord_n, int parametric, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelGetClosestPoint(int dim, int tag, double[] coord, nuint coord_n, double** closestCoord, nuint* closestCoord_n, double** parametricCoord, nuint* parametricCoord_n, int* ierr);
        // Geo
        [DllImport(Lib)] public static extern void gmshModelGeoSynchronize(int* ierr);
        [DllImport(Lib)] public static extern int gmshModelGeoAddVolume(int[] shellTags, nuint shellTags_n, int tag, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelGeoAddSurfaceLoop(int[] surfaceTags, nuint surfaceTags_n, int tag, int* ierr);
        // Mesh
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double NativeSizeCallback(int dim, int tag, double x, double y, double z, double lc, IntPtr data);
        [DllImport(Lib)] public static extern void gmshModelMeshGenerate(int dim, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetSizeCallback(NativeSizeCallback callback, IntPtr data, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRemoveSizeCallback(int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshOptimize(byte[] method, int force, int niter, int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshAffineTransform(double[] affineTransform, nuint affineTransform_n, int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshClear(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshAddNodes(int dim, int tag, IntPtr[] nodeTags, nuint nodeTags_n, double[] coord, nuint coord_n, double[] parametricCoord, nuint parametricCoord_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshAddFaces(int faceType, IntPtr[] faceTags, nuint faceTags_n, IntPtr[] faceNodes, nuint faceNodes_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshAddElements(int dim, int tag, int[] elementTypes, nuint elementTypes_n, IntPtr* elementTags, nuint* elementTags_n, nuint elementTags_nn, IntPtr* nodeTags, nuint* nodeTags_n, nuint nodeTags_nn, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshClassifySurfaces(double angle, int boundary, int forReparametrization, double curveAngle, int exportDiscrete, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshCreateGeometry(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshCreateTopology(int makeSimplyConnected, int exportDiscrete, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetIntegrationPoints(int elementType, byte[] integrationType, double** localCoord, nuint* localCoord_n, double** weights, nuint* weights_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetElementTypes(int** elementTypes, nuint* elementTypes_n, int dim, int tag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetNodes(nuint** nodeTags, nuint* nodeTags_n, double** coord, nuint* coord_n, double** parametricCoord, nuint* parametricCoord_n, int dim, int tag, int includeBoundary, int returnParametricCoord, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetElement(nuint elementTag, int* elementType, nuint** nodeTags, nuint* nodeTags_n, int* dim, int* tag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetNode(nuint nodeTag, double[] coord, nuint coord_n, double[] parametricCoord, nuint parametricCoord_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetElementProperties(int elementType, IntPtr* elementName, int* dim, int* order, int* numNodes, double** localNodeCoord, nuint* localNodeCoord_n, int* numPrimaryNodes, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetMaxNodeTag(nuint* maxTag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetMaxElementTag(nuint* maxTag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetElementQualities(IntPtr[] elementTags, nuint elementTags_n, double** elementsQuality, nuint* elementsQuality_n, byte[] qualityName, nuint task, nuint numTasks, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetElements(int** elementTypes, nuint* elementTypes_n, nuint*** elementTags, nuint** elementTags_n, nuint* elementTags_nn, nuint*** nodeTags, nuint** nodeTags_n, nuint* nodeTags_nn, int dim, int tag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRemoveDuplicateNodes(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshEmbed(int dim, int[] tags, nuint tags_n, int inDim, int inTag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRemoveConstraints(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRemoveDuplicateElements(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRemoveElements(int dim, int tag, IntPtr[] elementTags, nuint elementTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshCreateFaces(int[] dimTags, nuint dimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetAllFaces(int faceType, nuint** faceTags, nuint* faceTags_n, nuint** faceNodes, nuint* faceNodes_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetJacobians(int elementType, double[] localCoord, nuint localCoord_n, double** jacobians, nuint* jacobians_n, double** determinants, nuint* determinants_n, double** coord, nuint* coord_n, int tag, nuint task, nuint numTasks, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRecombine(int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRefine(int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetRecombine(int dim, int tag, double angle, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetAlgorithm(int dim, int tag, int val, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetSizeFromBoundary(int dim, int tag, int val, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetCompound(int dim, int[] tags, nuint tags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetTransfiniteCurve(int tag, int numNodes, byte[] meshType, double coef, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetTransfiniteSurface(int tag, byte[] arrangement, int[] cornerTags, nuint cornerTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetTransfiniteVolume(int tag, int[] cornerTags, nuint cornerTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetTransfiniteAutomatic(int[] dimTags, nuint dimTags_n, double cornerAngle, int recombine, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetSmoothing(int dim, int tag, int val, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetOutwardOrientation(int tag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshSetOrder(int order, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshRenumberElements(IntPtr[] oldTags, nuint oldTags_n, IntPtr[] newTags, nuint newTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelMeshGetSizes(int[] dimTags, nuint dimTags_n, double** sizes, nuint* sizes_n, int* ierr);
        // OCC
        [DllImport(Lib)] public static extern void gmshModelOccExtrude(int[] dimTags, nuint dimTags_n, double dx, double dy, double dz, int** outDimTags, nuint* outDimTags_n, int[] numElements, nuint numElements_n, double[] heights, nuint heights_n, int recombine, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccRevolve(int[] dimTags, nuint dimTags_n, double x, double y, double z, double ax, double ay, double az, double angle, int** outDimTags, nuint* outDimTags_n, int[] numElements, nuint numElements_n, double[] heights, nuint heights_n, int recombine, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccSynchronize(int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccImportShapes(byte[] fileName, int** outDimTags, nuint* outDimTags_n, int highestDimOnly, byte[] format, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccGetEntities(int** dimTags, nuint* dimTags_n, int dim, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccRemove(int[] dimTags, nuint dimTags_n, int recursive, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccRemoveAllDuplicates(int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccCopy(int[] dimTags, nuint dimTags_n, int** outDimTags, nuint* outDimTags_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccTranslate(int[] dimTags, nuint dimTags_n, double dx, double dy, double dz, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddCircleArc(int startTag, int middleTag, int endTag, int tag, int center, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccDefeature(int[] volumeTags, nuint volumeTags_n, int[] surfaceTags, nuint surfaceTags_n, int** outDimTags, nuint* outDimTags_n, int removeVolume, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccGetDistance(int dim1, int tag1, int dim2, int tag2, double* distance, double* x1, double* y1, double* z1, double* x2, double* y2, double* z2, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccFragment(int[] objectDimTags, nuint objectDimTags_n, int[] toolDimTags, nuint toolDimTags_n, int** outDimTags, nuint* outDimTags_n, int*** outDimTagsMap, nuint** outDimTagsMap_n, nuint* outDimTagsMap_nn, int tag, int removeObject, int removeTool, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccIntersect(int[] objectDimTags, nuint objectDimTags_n, int[] toolDimTags, nuint toolDimTags_n, int** outDimTags, nuint* outDimTags_n, int*** outDimTagsMap, nuint** outDimTagsMap_n, nuint* outDimTagsMap_nn, int tag, int removeObject, int removeTool, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccHealShapes(int** outDimTags, nuint* outDimTags_n, int[] dimTags, nuint dimTags_n, double tolerance, int fixDegenerated, int fixSmallEdges, int fixSmallFaces, int sewFaces, int makeSolids, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccGetMass(int dim, int tag, double* mass, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccGetCenterOfMass(int dim, int tag, double* x, double* y, double* z, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccGetEntitiesInBoundingBox(double xmin, double ymin, double zmin, double xmax, double ymax, double zmax, int** dimTags, nuint* dimTags_n, int dim, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddPoint(double x, double y, double z, double meshSize, int tag, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddLine(int startTag, int endTag, int tag, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddBSpline(int[] pointTags, nuint pointTags_n, int tag, int degree, double[] weights, nuint weights_n, double[] knots, nuint knots_n, int[] multiplicities, nuint multiplicities_n, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddCurveLoop(int[] curveTags, nuint curveTags_n, int tag, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddPlaneSurface(int[] wireTags, nuint wireTags_n, int tag, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddBSplineSurface(int[] pointTags, nuint pointTags_n, int numPointsU, int tag, int degreeU, int degreeV, double[] weights, nuint weights_n, double[] knotsU, nuint knotsU_n, double[] knotsV, nuint knotsV_n, int[] multiplicitiesU, nuint multiplicitiesU_n, int[] multiplicitiesV, nuint multiplicitiesV_n, int[] wireTags, nuint wireTags_n, int wire3D, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddWire(int[] curveTags, nuint curveTags_n, int tag, int checkClosed, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddSurfaceLoop(int[] surfaceTags, nuint surfaceTags_n, int tag, int sewing, int* ierr);
        [DllImport(Lib)] public static extern int gmshModelOccAddVolume(int[] shellTags, nuint shellTags_n, int tag, int* ierr);
        [DllImport(Lib)] public static extern void gmshModelOccMeshSetSize(int[] dimTags, nuint dimTags_n, double size, int* ierr);
        // Logger
        [DllImport(Lib)] public static extern void gmshLoggerWrite(byte[] message, byte[] level, int* ierr);
        [DllImport(Lib)] public static extern void gmshLoggerStart(int* ierr);
        [DllImport(Lib)] public static extern void gmshLoggerGet(IntPtr** log, nuint* log_n, int* ierr);
        [DllImport(Lib)] public static extern void gmshLoggerStop(int* ierr);
        [DllImport(Lib)] public static extern void gmshLoggerGetLastError(IntPtr* error, int* ierr);

        // Helpers
        public static byte[] S(string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
            Array.Resize(ref bytes, bytes.Length + 1);
            return bytes;
        }
        public static void Check(int ierr, string function)
        {
            if (ierr == 0) return;
            string message = null;
            try
            {
                IntPtr error;
                int err2;
                gmshLoggerGetLastError(&error, &err2);
                message = TakeString(error);
            }
            catch { }
            throw new Exception("gmsh " + function + " failed" + (string.IsNullOrEmpty(message) ? "." : ": " + message));
        }
        public static string TakeString(IntPtr p)
        {
            if (p == IntPtr.Zero) return string.Empty;
            string value = Marshal.PtrToStringUTF8(p) ?? string.Empty;
            gmshFree((void*)p);
            return value;
        }
        public static T[] Take<T>(T* p, nuint n) where T : unmanaged
        {
            T[] array = new T[(int)n];
            for (int i = 0; i < array.Length; i++) array[i] = p[i];
            if (p != null) gmshFree(p);
            return array;
        }
        public static IntPtr[] TakeSize(nuint* p, nuint n)
        {
            IntPtr[] array = new IntPtr[(int)n];
            for (int i = 0; i < array.Length; i++) array[i] = (IntPtr)(long)p[i];
            if (p != null) gmshFree(p);
            return array;
        }
        public static Tuple<int, int>[] TakeDimTags(int* p, nuint n)
        {
            Tuple<int, int>[] dimTags = new Tuple<int, int>[(int)n / 2];
            for (int i = 0; i < dimTags.Length; i++) dimTags[i] = new Tuple<int, int>(p[2 * i], p[2 * i + 1]);
            if (p != null) gmshFree(p);
            return dimTags;
        }
        public static int[] Flatten(Tuple<int, int>[] dimTags)
        {
            if (dimTags == null) return new int[0];
            int[] flat = new int[dimTags.Length * 2];
            for (int i = 0; i < dimTags.Length; i++)
            {
                flat[2 * i] = dimTags[i].Item1;
                flat[2 * i + 1] = dimTags[i].Item2;
            }
            return flat;
        }
        public static nuint N(Array array) { return array == null ? 0 : (nuint)array.Length; }
        public static Tuple<int, int>[][] TakeDimTagsMap(int** p, nuint* n, nuint nn)
        {
            Tuple<int, int>[][] map = new Tuple<int, int>[(int)nn][];
            for (int i = 0; i < map.Length; i++) map[i] = TakeDimTags(p[i], n[i]);
            if (p != null) gmshFree(p);
            if (n != null) gmshFree(n);
            return map;
        }
    }

    public static unsafe class Gmsh
    {
        static Gmsh() { Native.EnsureResolver(); }

        // Top-level functions
        public static void Initialize()
        {
            int ierr;
            Native.gmshInitialize(0, IntPtr.Zero, 1, 0, &ierr);
            Native.Check(ierr, "initialize");
        }
        public static int IsInitialized()
        {
            int ierr;
            int value = Native.gmshIsInitialized(&ierr);
            Native.Check(ierr, "isInitialized");
            return value;
        }
        public static void FinalizeAll()
        {
            int ierr;
            Native.gmshFinalize(&ierr);
            Native.Check(ierr, "finalize");
        }
        public static void Open(string filepath)
        {
            int ierr;
            Native.gmshOpen(Native.S(filepath), &ierr);
            Native.Check(ierr, "open");
        }
        public static void Merge(string filepath)
        {
            int ierr;
            Native.gmshMerge(Native.S(filepath), &ierr);
            Native.Check(ierr, "merge");
        }
        public static void Write(string filepath)
        {
            int ierr;
            Native.gmshWrite(Native.S(filepath), &ierr);
            Native.Check(ierr, "write");
        }
        public static void Clear()
        {
            int ierr;
            Native.gmshClear(&ierr);
            Native.Check(ierr, "clear");
        }

        public static class Option
        {
            public static void SetNumber(string parameter, double value)
            {
                int ierr;
                Native.gmshOptionSetNumber(Native.S(parameter), value, &ierr);
                Native.Check(ierr, "option.setNumber(" + parameter + ")");
            }
            public static double GetNumber(string parameter)
            {
                int ierr;
                double value = 0;
                Native.gmshOptionGetNumber(Native.S(parameter), &value, &ierr);
                Native.Check(ierr, "option.getNumber(" + parameter + ")");
                return value;
            }
            public static void SetString(string parameter, string value)
            {
                int ierr;
                Native.gmshOptionSetString(Native.S(parameter), Native.S(value), &ierr);
                Native.Check(ierr, "option.setString(" + parameter + ")");
            }
            public static string GetString(string parameter)
            {
                int ierr;
                IntPtr value;
                Native.gmshOptionGetString(Native.S(parameter), &value, &ierr);
                Native.Check(ierr, "option.getString(" + parameter + ")");
                return Native.TakeString(value);
            }
        }

        public static class Model
        {
            public static void Add(string name)
            {
                int ierr;
                Native.gmshModelAdd(Native.S(name), &ierr);
                Native.Check(ierr, "model.add");
            }
            public static string GetCurrent()
            {
                int ierr;
                IntPtr name;
                Native.gmshModelGetCurrent(&name, &ierr);
                Native.Check(ierr, "model.getCurrent");
                return Native.TakeString(name);
            }
            public static void SetCurrent(string name)
            {
                int ierr;
                Native.gmshModelSetCurrent(Native.S(name), &ierr);
                Native.Check(ierr, "model.setCurrent");
            }
            public static void GetEntities(out Tuple<int, int>[] dimTags)
            {
                GetEntities(out dimTags, -1);
            }
            public static void GetEntities(out Tuple<int, int>[] dimTags, int dim)
            {
                int ierr;
                int* p;
                nuint n;
                Native.gmshModelGetEntities(&p, &n, dim, &ierr);
                Native.Check(ierr, "model.getEntities");
                dimTags = Native.TakeDimTags(p, n);
            }
            public static void SetEntityName(int dim, int tag, string name)
            {
                int ierr;
                Native.gmshModelSetEntityName(dim, tag, Native.S(name), &ierr);
                Native.Check(ierr, "model.setEntityName");
            }
            public static string GetEntityName(int dim, int tag)
            {
                int ierr;
                IntPtr name;
                Native.gmshModelGetEntityName(dim, tag, &name, &ierr);
                Native.Check(ierr, "model.getEntityName");
                return Native.TakeString(name);
            }
            public static Tuple<int, int>[] GetPhysicalGroups(int dim)
            {
                int ierr;
                int* p;
                nuint n;
                Native.gmshModelGetPhysicalGroups(&p, &n, dim, &ierr);
                Native.Check(ierr, "model.getPhysicalGroups");
                return Native.TakeDimTags(p, n);
            }
            public static int[] GetEntitiesForPhysicalGroup(int dim, int tag)
            {
                int ierr;
                int* p;
                nuint n;
                Native.gmshModelGetEntitiesForPhysicalGroup(dim, tag, &p, &n, &ierr);
                Native.Check(ierr, "model.getEntitiesForPhysicalGroup");
                return Native.Take(p, n);
            }
            public static int AddPhysicalGroup(int dim, int[] tags, string name)
            {
                return AddPhysicalGroup(dim, tags, -1, name);
            }
            public static int AddPhysicalGroup(int dim, int[] tags, int tag, string name)
            {
                int ierr;
                int value = Native.gmshModelAddPhysicalGroup(dim, tags, Native.N(tags), tag, Native.S(name), &ierr);
                Native.Check(ierr, "model.addPhysicalGroup");
                return value;
            }
            public static void RemovePhysicalGroups(Tuple<int, int>[] dimTags)
            {
                int ierr;
                int[] flat = Native.Flatten(dimTags);
                Native.gmshModelRemovePhysicalGroups(flat, Native.N(flat), &ierr);
                Native.Check(ierr, "model.removePhysicalGroups");
            }
            public static void SetPhysicalName(int dim, int tag, string name)
            {
                int ierr;
                Native.gmshModelSetPhysicalName(dim, tag, Native.S(name), &ierr);
                Native.Check(ierr, "model.setPhysicalName");
            }
            public static string GetPhysicalName(int dim, int tag)
            {
                int ierr;
                IntPtr name;
                Native.gmshModelGetPhysicalName(dim, tag, &name, &ierr);
                Native.Check(ierr, "model.getPhysicalName");
                return Native.TakeString(name);
            }
            public static void GetBoundary(Tuple<int, int>[] tags, out Tuple<int, int>[] outDimTags, bool combined,
                                           bool oriented, bool recursive)
            {
                int ierr;
                int* p;
                nuint n;
                int[] flat = Native.Flatten(tags);
                Native.gmshModelGetBoundary(flat, Native.N(flat), &p, &n, combined ? 1 : 0, oriented ? 1 : 0,
                                            recursive ? 1 : 0, &ierr);
                Native.Check(ierr, "model.getBoundary");
                outDimTags = Native.TakeDimTags(p, n);
            }
            public static void GetAdjacencies(int dim, int tag, out int[] upward, out int[] downward)
            {
                int ierr;
                int* up;
                int* down;
                nuint upN, downN;
                Native.gmshModelGetAdjacencies(dim, tag, &up, &upN, &down, &downN, &ierr);
                Native.Check(ierr, "model.getAdjacencies");
                upward = Native.Take(up, upN);
                downward = Native.Take(down, downN);
            }
            public static void GetBoundingBox(int dim, int tag, out double xmin, out double ymin, out double zmin,
                                              out double xmax, out double ymax, out double zmax)
            {
                int ierr;
                double a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;
                Native.gmshModelGetBoundingBox(dim, tag, &a, &b, &c, &d, &e, &f, &ierr);
                Native.Check(ierr, "model.getBoundingBox");
                xmin = a; ymin = b; zmin = c; xmax = d; ymax = e; zmax = f;
            }
            public static int AddDiscreteEntity(int dim, int tag)
            {
                int ierr;
                int value = Native.gmshModelAddDiscreteEntity(dim, tag, new int[0], 0, &ierr);
                Native.Check(ierr, "model.addDiscreteEntity");
                return value;
            }
            public static void RemoveEntities(Tuple<int, int>[] tags)
            {
                RemoveEntities(tags, false);
            }
            public static void RemoveEntities(Tuple<int, int>[] tags, bool recursive)
            {
                int ierr;
                int[] flat = Native.Flatten(tags);
                Native.gmshModelRemoveEntities(flat, Native.N(flat), recursive ? 1 : 0, &ierr);
                Native.Check(ierr, "model.removeEntities");
            }
            public static void SetVisibility(Tuple<int, int>[] tags, int value)
            {
                SetVisibility(tags, value, false);
            }
            public static void SetVisibility(Tuple<int, int>[] tags, int value, bool recursive)
            {
                int ierr;
                int[] flat = Native.Flatten(tags);
                Native.gmshModelSetVisibility(flat, Native.N(flat), value, recursive ? 1 : 0, &ierr);
                Native.Check(ierr, "model.setVisibility");
            }
            public static new string GetType(int dim, int tag)
            {
                int ierr;
                IntPtr value;
                Native.gmshModelGetType(dim, tag, &value, &ierr);
                Native.Check(ierr, "model.getType");
                return Native.TakeString(value);
            }
            public static void GetValue(int dim, int tag, double[] parametricCoord, out double[] coord)
            {
                int ierr;
                double* p;
                nuint n;
                Native.gmshModelGetValue(dim, tag, parametricCoord, Native.N(parametricCoord), &p, &n, &ierr);
                Native.Check(ierr, "model.getValue");
                coord = Native.Take(p, n);
            }
            public static void GetNormal(int tag, double[] parametricCoord, out double[] normals)
            {
                int ierr;
                double* p;
                nuint n;
                Native.gmshModelGetNormal(tag, parametricCoord, Native.N(parametricCoord), &p, &n, &ierr);
                Native.Check(ierr, "model.getNormal");
                normals = Native.Take(p, n);
            }
            public static void GetParametrization(int dim, int tag, double[] coord, out double[] parametricCoord)
            {
                int ierr;
                double* p;
                nuint n;
                Native.gmshModelGetParametrization(dim, tag, coord, Native.N(coord), &p, &n, &ierr);
                Native.Check(ierr, "model.getParametrization");
                parametricCoord = Native.Take(p, n);
            }
            public static void GetParametrizationBounds(int dim, int tag, out double[] min, out double[] max)
            {
                int ierr;
                double* a;
                double* b;
                nuint an, bn;
                Native.gmshModelGetParametrizationBounds(dim, tag, &a, &an, &b, &bn, &ierr);
                Native.Check(ierr, "model.getParametrizationBounds");
                min = Native.Take(a, an);
                max = Native.Take(b, bn);
            }
            public static int IsInside(int dim, int tag, double[] coord, bool parametric)
            {
                int ierr;
                int value = Native.gmshModelIsInside(dim, tag, coord, Native.N(coord), parametric ? 1 : 0, &ierr);
                Native.Check(ierr, "model.isInside");
                return value;
            }
            public static void GetClosestPoint(int dim, int tag, double[] coord, out double[] closestCoord,
                                               out double[] parametricCoord)
            {
                int ierr;
                double* a;
                double* b;
                nuint an, bn;
                Native.gmshModelGetClosestPoint(dim, tag, coord, Native.N(coord), &a, &an, &b, &bn, &ierr);
                Native.Check(ierr, "model.getClosestPoint");
                closestCoord = Native.Take(a, an);
                parametricCoord = Native.Take(b, bn);
            }

            public static class Geo
            {
                public static void Synchronize()
                {
                    int ierr;
                    Native.gmshModelGeoSynchronize(&ierr);
                    Native.Check(ierr, "model.geo.synchronize");
                }
                public static int AddVolume(int[] shellTags)
                {
                    return AddVolume(shellTags, -1);
                }
                public static int AddVolume(int[] shellTags, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelGeoAddVolume(shellTags, Native.N(shellTags), tag, &ierr);
                    Native.Check(ierr, "model.geo.addVolume");
                    return value;
                }
                public static int AddSurfaceLoop(int[] surfaceTags)
                {
                    return AddSurfaceLoop(surfaceTags, -1);
                }
                public static int AddSurfaceLoop(int[] surfaceTags, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelGeoAddSurfaceLoop(surfaceTags, Native.N(surfaceTags), tag, &ierr);
                    Native.Check(ierr, "model.geo.addSurfaceLoop");
                    return value;
                }
                public static class Mesh { }
            }

            public static class Mesh
            {
                // Kept so that the delegates gmsh calls through are not collected while gmsh holds them
                private static SizeCallback _sizeCallback;
                private static Native.NativeSizeCallback _nativeSizeCallback;

                public static void Generate(int dim)
                {
                    int ierr;
                    Native.gmshModelMeshGenerate(dim, &ierr);
                    Native.Check(ierr, "model.mesh.generate");
                }
                public static void SetSizeCallback(SizeCallback callback)
                {
                    int ierr;
                    _sizeCallback = callback;
                    _nativeSizeCallback = (dim, tag, x, y, z, lc, data) => _sizeCallback(dim, tag, x, y, z, lc);
                    Native.gmshModelMeshSetSizeCallback(_nativeSizeCallback, IntPtr.Zero, &ierr);
                    Native.Check(ierr, "model.mesh.setSizeCallback");
                }
                public static void RemoveSizeCallback()
                {
                    int ierr;
                    Native.gmshModelMeshRemoveSizeCallback(&ierr);
                    Native.Check(ierr, "model.mesh.removeSizeCallback");
                    _sizeCallback = null;
                    _nativeSizeCallback = null;
                }
                public static void Optimize(string method, bool force, int niter, Tuple<int, int>[] dimTags)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelMeshOptimize(Native.S(method), force ? 1 : 0, niter, flat, Native.N(flat), &ierr);
                    Native.Check(ierr, "model.mesh.optimize");
                }
                public static void AffineTransform(double[] affineTransform, int[][] dimTags)
                {
                    // Like GmshCommon: the transformation is applied to the whole mesh
                    int ierr;
                    Native.gmshModelMeshAffineTransform(affineTransform, Native.N(affineTransform), new int[0], 0, &ierr);
                    Native.Check(ierr, "model.mesh.affineTransform");
                }
                public static void Clear()
                {
                    Clear(new Tuple<int, int>[0]);
                }
                public static void Clear(Tuple<int, int>[] dimTags)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelMeshClear(flat, Native.N(flat), &ierr);
                    Native.Check(ierr, "model.mesh.clear");
                }
                public static void AddNodes(int dim, int tag, IntPtr[] nodeTags, double[] coordinates)
                {
                    AddNodes(dim, tag, nodeTags, coordinates, new double[0]);
                }
                public static void AddNodes(int dim, int tag, IntPtr[] nodeTags, double[] coordinates,
                                            double[] parametricCoordinates)
                {
                    int ierr;
                    Native.gmshModelMeshAddNodes(dim, tag, nodeTags, Native.N(nodeTags), coordinates, Native.N(coordinates),
                                                 parametricCoordinates, Native.N(parametricCoordinates), &ierr);
                    Native.Check(ierr, "model.mesh.addNodes");
                }
                public static void AddFaces(int faceType, IntPtr[] faceTags, IntPtr[] faceNodes)
                {
                    int ierr;
                    Native.gmshModelMeshAddFaces(faceType, faceTags, Native.N(faceTags), faceNodes, Native.N(faceNodes), &ierr);
                    Native.Check(ierr, "model.mesh.addFaces");
                }
                public static void AddElements(int dim, int tag, int[] elementTypes, IntPtr[][] elementTags,
                                               IntPtr[][] nodeTags)
                {
                    int ierr;
                    int count = elementTags.Length;
                    GCHandle[] handles = new GCHandle[elementTags.Length + nodeTags.Length];
                    try
                    {
                        IntPtr* elementPointers = stackalloc IntPtr[Math.Max(1, elementTags.Length)];
                        nuint* elementCounts = stackalloc nuint[Math.Max(1, elementTags.Length)];
                        IntPtr* nodePointers = stackalloc IntPtr[Math.Max(1, nodeTags.Length)];
                        nuint* nodeCounts = stackalloc nuint[Math.Max(1, nodeTags.Length)];
                        for (int i = 0; i < elementTags.Length; i++)
                        {
                            handles[i] = GCHandle.Alloc(elementTags[i] ?? new IntPtr[0], GCHandleType.Pinned);
                            elementPointers[i] = handles[i].AddrOfPinnedObject();
                            elementCounts[i] = Native.N(elementTags[i]);
                        }
                        for (int i = 0; i < nodeTags.Length; i++)
                        {
                            handles[count + i] = GCHandle.Alloc(nodeTags[i] ?? new IntPtr[0], GCHandleType.Pinned);
                            nodePointers[i] = handles[count + i].AddrOfPinnedObject();
                            nodeCounts[i] = Native.N(nodeTags[i]);
                        }
                        Native.gmshModelMeshAddElements(dim, tag, elementTypes, Native.N(elementTypes), elementPointers,
                                                        elementCounts, (nuint)elementTags.Length, nodePointers, nodeCounts,
                                                        (nuint)nodeTags.Length, &ierr);
                    }
                    finally
                    {
                        foreach (var handle in handles) if (handle.IsAllocated) handle.Free();
                    }
                    Native.Check(ierr, "model.mesh.addElements");
                }
                public static void ClassifySurfaces(double angle, bool boundary, bool forReparametrization,
                                                    double curveAngle, bool exportDiscrete)
                {
                    int ierr;
                    Native.gmshModelMeshClassifySurfaces(angle, boundary ? 1 : 0, forReparametrization ? 1 : 0, curveAngle,
                                                         exportDiscrete ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.mesh.classifySurfaces");
                }
                public static void CreateGeometry()
                {
                    int ierr;
                    Native.gmshModelMeshCreateGeometry(new int[0], 0, &ierr);
                    Native.Check(ierr, "model.mesh.createGeometry");
                }
                public static void CreateTopology()
                {
                    CreateTopology(true, true);
                }
                public static void CreateTopology(bool makeSimplyConnected, bool exportDiscrete)
                {
                    int ierr;
                    Native.gmshModelMeshCreateTopology(makeSimplyConnected ? 1 : 0, exportDiscrete ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.mesh.createTopology");
                }
                public static void GetIntegrationPoints(int elementType, string integrationType, out double[] localCoord,
                                                        out double[] weights)
                {
                    int ierr;
                    double* a;
                    double* b;
                    nuint an, bn;
                    Native.gmshModelMeshGetIntegrationPoints(elementType, Native.S(integrationType), &a, &an, &b, &bn, &ierr);
                    Native.Check(ierr, "model.mesh.getIntegrationPoints");
                    localCoord = Native.Take(a, an);
                    weights = Native.Take(b, bn);
                }
                public static void GetElementTypes(out int[] types, int dim)
                {
                    GetElementTypes(out types, dim, -1);
                }
                public static void GetElementTypes(out int[] types)
                {
                    GetElementTypes(out types, -1, -1);
                }
                public static void GetElementTypes(out int[] types, int dim, int tag)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    Native.gmshModelMeshGetElementTypes(&p, &n, dim, tag, &ierr);
                    Native.Check(ierr, "model.mesh.getElementTypes");
                    types = Native.Take(p, n);
                }
                public static void GetNodes(out IntPtr[] nodeTags, out double[] coord, int dim, int tag,
                                            bool includeBoundary, bool returnParametricCoord)
                {
                    int ierr;
                    nuint* tags;
                    double* c;
                    double* pc;
                    nuint tn, cn, pcn;
                    Native.gmshModelMeshGetNodes(&tags, &tn, &c, &cn, &pc, &pcn, dim, tag, includeBoundary ? 1 : 0,
                                                 returnParametricCoord ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.mesh.getNodes");
                    nodeTags = Native.TakeSize(tags, tn);
                    coord = Native.Take(c, cn);
                    Native.Take(pc, pcn);
                }
                public static void GetElement(IntPtr elementTag, int elementType, out IntPtr[] nodeTags, out int dim,
                                              out int tag)
                {
                    int ierr;
                    int type = elementType;
                    nuint* p;
                    nuint n;
                    int d, t;
                    Native.gmshModelMeshGetElement((nuint)(long)elementTag, &type, &p, &n, &d, &t, &ierr);
                    Native.Check(ierr, "model.mesh.getElement");
                    nodeTags = Native.TakeSize(p, n);
                    dim = d;
                    tag = t;
                }
                public static void SetNode(IntPtr nodeTag, double[] coord, double[] parametricCoord)
                {
                    int ierr;
                    Native.gmshModelMeshSetNode((nuint)(long)nodeTag, coord, Native.N(coord), parametricCoord,
                                                Native.N(parametricCoord), &ierr);
                    Native.Check(ierr, "model.mesh.setNode");
                }
                public static string GetElementProperties(int elementType, out int dim, out int order, out int numNodes,
                                                          out double[] localNodeCoords, out int numPrimaryNodes)
                {
                    int ierr;
                    IntPtr name;
                    int d, o, nn, np;
                    double* p;
                    nuint n;
                    Native.gmshModelMeshGetElementProperties(elementType, &name, &d, &o, &nn, &p, &n, &np, &ierr);
                    Native.Check(ierr, "model.mesh.getElementProperties");
                    dim = d;
                    order = o;
                    numNodes = nn;
                    numPrimaryNodes = np;
                    localNodeCoords = Native.Take(p, n);
                    return Native.TakeString(name);
                }
                public static void GetMaxNodeTag(out IntPtr maxTag)
                {
                    int ierr;
                    nuint value;
                    Native.gmshModelMeshGetMaxNodeTag(&value, &ierr);
                    Native.Check(ierr, "model.mesh.getMaxNodeTag");
                    maxTag = new IntPtr((int)value);
                }
                public static void GetMaxElementTag(out IntPtr maxTag)
                {
                    int ierr;
                    nuint value;
                    Native.gmshModelMeshGetMaxElementTag(&value, &ierr);
                    Native.Check(ierr, "model.mesh.getMaxElementTag");
                    maxTag = new IntPtr((int)value);
                }
                public static void GetElementQualities(IntPtr[] elementTags, out double[] elementsQuality, string qualityName)
                {
                    int ierr;
                    double* p;
                    nuint n;
                    Native.gmshModelMeshGetElementQualities(elementTags, Native.N(elementTags), &p, &n, Native.S(qualityName),
                                                            0, 1, &ierr);
                    Native.Check(ierr, "model.mesh.getElementQualities");
                    elementsQuality = Native.Take(p, n);
                }
                public static void GetElements(out int[] elementTypes, out IntPtr[][] elementTags, out IntPtr[][] nodeTags,
                                               int dim, int tag)
                {
                    int ierr;
                    int* types;
                    nuint typesN;
                    nuint** eTags;
                    nuint* eTagsN;
                    nuint eTagsNN;
                    nuint** nTags;
                    nuint* nTagsN;
                    nuint nTagsNN;
                    Native.gmshModelMeshGetElements(&types, &typesN, &eTags, &eTagsN, &eTagsNN, &nTags, &nTagsN, &nTagsNN,
                                                    dim, tag, &ierr);
                    Native.Check(ierr, "model.mesh.getElements");
                    elementTypes = Native.Take(types, typesN);
                    elementTags = new IntPtr[Math.Max(1, (int)eTagsNN)][];
                    for (int i = 0; i < (int)eTagsNN; i++) elementTags[i] = Native.TakeSize(eTags[i], eTagsN[i]);
                    nodeTags = new IntPtr[Math.Max(1, (int)nTagsNN)][];
                    for (int i = 0; i < (int)nTagsNN; i++) nodeTags[i] = Native.TakeSize(nTags[i], nTagsN[i]);
                    if (eTags != null) Native.gmshFree(eTags);
                    if (eTagsN != null) Native.gmshFree(eTagsN);
                    if (nTags != null) Native.gmshFree(nTags);
                    if (nTagsN != null) Native.gmshFree(nTagsN);
                }
                public static void RemoveDuplicateNodes(Tuple<int, int>[] tags)
                {
                    int ierr;
                    int[] flat = Native.Flatten(tags);
                    Native.gmshModelMeshRemoveDuplicateNodes(flat, Native.N(flat), &ierr);
                    Native.Check(ierr, "model.mesh.removeDuplicateNodes");
                }
                public static void RemoveDuplicateNodes()
                {
                    RemoveDuplicateNodes(new Tuple<int, int>[0]);
                }
                public static void Embed(int dim, int[] tags, int inDim, int inTag)
                {
                    int ierr;
                    Native.gmshModelMeshEmbed(dim, tags, Native.N(tags), inDim, inTag, &ierr);
                    Native.Check(ierr, "model.mesh.embed");
                }
                public static void RemoveConstraints(Tuple<int, int>[] dimTags)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelMeshRemoveConstraints(flat, Native.N(flat), &ierr);
                    Native.Check(ierr, "model.mesh.removeConstraints");
                }
                public static void RemoveDuplicateElements()
                {
                    int ierr;
                    Native.gmshModelMeshRemoveDuplicateElements(new int[0], 0, &ierr);
                    Native.Check(ierr, "model.mesh.removeDuplicateElements");
                }
                public static void RemoveElements(int dim, int tag)
                {
                    int ierr;
                    Native.gmshModelMeshRemoveElements(dim, tag, new IntPtr[0], 0, &ierr);
                    Native.Check(ierr, "model.mesh.removeElements");
                }
                public static void CreateFaces()
                {
                    int ierr;
                    Native.gmshModelMeshCreateFaces(new int[0], 0, &ierr);
                    Native.Check(ierr, "model.mesh.createFaces");
                }
                public static void GetAllFaces(int dim, out IntPtr[] faceTags, out IntPtr[] faceNodes)
                {
                    int ierr;
                    nuint* a;
                    nuint* b;
                    nuint an, bn;
                    Native.gmshModelMeshGetAllFaces(dim, &a, &an, &b, &bn, &ierr);
                    Native.Check(ierr, "model.mesh.getAllFaces");
                    faceTags = Native.TakeSize(a, an);
                    faceNodes = Native.TakeSize(b, bn);
                }
                public static void GetJacobians(int elementType, int tag, double[] localCoord, out double[] jacobians,
                                                out double[] determinants, out double[] coord)
                {
                    int ierr;
                    double* j;
                    double* d;
                    double* c;
                    nuint jn, dn, cn;
                    Native.gmshModelMeshGetJacobians(elementType, localCoord, Native.N(localCoord), &j, &jn, &d, &dn, &c, &cn,
                                                     tag, 0, 1, &ierr);
                    Native.Check(ierr, "model.mesh.getJacobians");
                    jacobians = Native.Take(j, jn);
                    determinants = Native.Take(d, dn);
                    coord = Native.Take(c, cn);
                }
                public static void Recombine()
                {
                    int ierr;
                    Native.gmshModelMeshRecombine(&ierr);
                    Native.Check(ierr, "model.mesh.recombine");
                }
                public static void Refine()
                {
                    int ierr;
                    Native.gmshModelMeshRefine(&ierr);
                    Native.Check(ierr, "model.mesh.refine");
                }
                public static void SetRecombine(int dim, int tag)
                {
                    SetRecombine(dim, tag, 45.0);
                }
                public static void SetRecombine(int dim, int tag, double angle)
                {
                    int ierr;
                    Native.gmshModelMeshSetRecombine(dim, tag, angle, &ierr);
                    Native.Check(ierr, "model.mesh.setRecombine");
                }
                public static void SetAlgorithm(int dim, int tag, int val)
                {
                    int ierr;
                    Native.gmshModelMeshSetAlgorithm(dim, tag, val, &ierr);
                    Native.Check(ierr, "model.mesh.setAlgorithm");
                }
                public static void SetSizeFromBoundary(int dim, int tag, int val)
                {
                    int ierr;
                    Native.gmshModelMeshSetSizeFromBoundary(dim, tag, val, &ierr);
                    Native.Check(ierr, "model.mesh.setSizeFromBoundary");
                }
                public static void SetCompound(int dim, int[] tags)
                {
                    int ierr;
                    Native.gmshModelMeshSetCompound(dim, tags, Native.N(tags), &ierr);
                    Native.Check(ierr, "model.mesh.setCompound");
                }
                public static void SetTransfiniteCurve(int tag, int numNodes)
                {
                    SetTransfiniteCurve(tag, numNodes, "Progression", 1.0);
                }
                public static void SetTransfiniteCurve(int tag, int numNodes, string meshType, double coef)
                {
                    int ierr;
                    Native.gmshModelMeshSetTransfiniteCurve(tag, numNodes, Native.S(meshType), coef, &ierr);
                    Native.Check(ierr, "model.mesh.setTransfiniteCurve");
                }
                public static void SetTransfiniteSurface(int tag)
                {
                    SetTransfiniteSurface(tag, "Left", new int[0]);
                }
                public static void SetTransfiniteSurface(int tag, string arrangement)
                {
                    SetTransfiniteSurface(tag, arrangement, new int[0]);
                }
                public static void SetTransfiniteSurface(int tag, string arrangement, int[] cornerTags)
                {
                    int ierr;
                    Native.gmshModelMeshSetTransfiniteSurface(tag, Native.S(arrangement), cornerTags, Native.N(cornerTags), &ierr);
                    Native.Check(ierr, "model.mesh.setTransfiniteSurface");
                }
                public static void SetTransfiniteVolume(int tag)
                {
                    SetTransfiniteVolume(tag, new int[0]);
                }
                public static void SetTransfiniteVolume(int tag, int[] cornerTags)
                {
                    int ierr;
                    Native.gmshModelMeshSetTransfiniteVolume(tag, cornerTags, Native.N(cornerTags), &ierr);
                    Native.Check(ierr, "model.mesh.setTransfiniteVolume");
                }
                public static void SetTransfiniteAutomatic()
                {
                    SetTransfiniteAutomatic(new Tuple<int, int>[0], 2.35, true);
                }
                public static void SetTransfiniteAutomatic(double cornerAngle)
                {
                    SetTransfiniteAutomatic(new Tuple<int, int>[0], cornerAngle, true);
                }
                public static void SetTransfiniteAutomatic(double cornerAngle, bool recombine)
                {
                    SetTransfiniteAutomatic(new Tuple<int, int>[0], cornerAngle, recombine);
                }
                public static void SetTransfiniteAutomatic(Tuple<int, int>[] dimTags, double cornerAngle, bool recombine)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelMeshSetTransfiniteAutomatic(flat, Native.N(flat), cornerAngle, recombine ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.mesh.setTransfiniteAutomatic");
                }
                public static void SetSmoothing(int dim, int tag, int val)
                {
                    int ierr;
                    Native.gmshModelMeshSetSmoothing(dim, tag, val, &ierr);
                    Native.Check(ierr, "model.mesh.setSmoothing");
                }
                public static void SetOutwardOrientation(int tag)
                {
                    int ierr;
                    Native.gmshModelMeshSetOutwardOrientation(tag, &ierr);
                    Native.Check(ierr, "model.mesh.setOutwardOrientation");
                }
                public static void SetOrder(int order)
                {
                    int ierr;
                    Native.gmshModelMeshSetOrder(order, &ierr);
                    Native.Check(ierr, "model.mesh.setOrder");
                }
                public static void RenumberElements()
                {
                    int ierr;
                    Native.gmshModelMeshRenumberElements(new IntPtr[0], 0, new IntPtr[0], 0, &ierr);
                    Native.Check(ierr, "model.mesh.renumberElements");
                }
                public static void GetSizes(Tuple<int, int>[] dimTags, out double[] sizes)
                {
                    int ierr;
                    double* p;
                    nuint n;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelMeshGetSizes(flat, Native.N(flat), &p, &n, &ierr);
                    Native.Check(ierr, "model.mesh.getSizes");
                    sizes = Native.Take(p, n);
                }
                public static class Field { }
            }

            public static class OCC
            {
                public static void Extrude(Tuple<int, int>[] dimTags, double dx, double dy, double dz,
                                           out Tuple<int, int>[] outDimTags, int[] numElements, double[] heights,
                                           bool recombine)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccExtrude(flat, Native.N(flat), dx, dy, dz, &p, &n, numElements, Native.N(numElements),
                                               heights, Native.N(heights), recombine ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.extrude");
                    outDimTags = Native.TakeDimTags(p, n);
                }
                public static void Revolve(Tuple<int, int>[] dimTags, double x, double y, double z, double ax, double ay,
                                           double az, double angle, out Tuple<int, int>[] outDimTags, int[] numElements,
                                           double[] heights, bool recombine)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccRevolve(flat, Native.N(flat), x, y, z, ax, ay, az, angle, &p, &n, numElements,
                                               Native.N(numElements), heights, Native.N(heights), recombine ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.revolve");
                    outDimTags = Native.TakeDimTags(p, n);
                }
                public static void Synchronize()
                {
                    int ierr;
                    Native.gmshModelOccSynchronize(&ierr);
                    Native.Check(ierr, "model.occ.synchronize");
                }
                public static void ImportShapes(string fileName, out Tuple<int, int>[] dimTags, bool highestDimOnly,
                                                string format)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    Native.gmshModelOccImportShapes(Native.S(fileName), &p, &n, highestDimOnly ? 1 : 0, Native.S(format), &ierr);
                    Native.Check(ierr, "model.occ.importShapes");
                    dimTags = Native.TakeDimTags(p, n);
                }
                public static Tuple<int, int>[] GetEntities(int dim)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    Native.gmshModelOccGetEntities(&p, &n, dim, &ierr);
                    Native.Check(ierr, "model.occ.getEntities");
                    return Native.TakeDimTags(p, n);
                }
                public static void Remove(Tuple<int, int>[] dimTags)
                {
                    Remove(dimTags, false);
                }
                public static void Remove(Tuple<int, int>[] dimTags, bool recursive)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccRemove(flat, Native.N(flat), recursive ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.remove");
                }
                public static void RemoveAllDuplicates()
                {
                    int ierr;
                    Native.gmshModelOccRemoveAllDuplicates(&ierr);
                    Native.Check(ierr, "model.occ.removeAllDuplicates");
                }
                public static void Copy(Tuple<int, int>[] dimTags, out Tuple<int, int>[] outDimTags)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccCopy(flat, Native.N(flat), &p, &n, &ierr);
                    Native.Check(ierr, "model.occ.copy");
                    outDimTags = Native.TakeDimTags(p, n);
                }
                public static void Translate(Tuple<int, int>[] dimTags, double dx, double dy, double dz)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccTranslate(flat, Native.N(flat), dx, dy, dz, &ierr);
                    Native.Check(ierr, "model.occ.translate");
                }
                public static int AddCircleArc(int startTag, int middleTag, int endTag, bool center)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddCircleArc(startTag, middleTag, endTag, -1, center ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.addCircleArc");
                    return value;
                }
                public static void Defeature(int[] volumeTags, int[] surfaceTags, out Tuple<int, int>[] outDimTags,
                                             bool removeVolume)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    Native.gmshModelOccDefeature(volumeTags, Native.N(volumeTags), surfaceTags, Native.N(surfaceTags), &p, &n,
                                                 removeVolume ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.defeature");
                    outDimTags = Native.TakeDimTags(p, n);
                }
                public static void GetDistance(int dim1, int tag1, int dim2, int tag2, out double distance)
                {
                    GetDistance(dim1, tag1, dim2, tag2, out distance, out _, out _, out _, out _, out _, out _);
                }
                public static void GetDistance(int dim1, int tag1, int dim2, int tag2, out double distance, out double x1,
                                               out double y1, out double z1, out double x2, out double y2, out double z2)
                {
                    int ierr;
                    double d = 0, a = 0, b = 0, c = 0, e = 0, f = 0, g = 0;
                    Native.gmshModelOccGetDistance(dim1, tag1, dim2, tag2, &d, &a, &b, &c, &e, &f, &g, &ierr);
                    Native.Check(ierr, "model.occ.getDistance");
                    distance = d; x1 = a; y1 = b; z1 = c; x2 = e; y2 = f; z2 = g;
                }
                public static void Fragment(Tuple<int, int>[] objectDimTags, Tuple<int, int>[] toolDimTags,
                                            out Tuple<int, int>[] outDimTags, out Tuple<int, int>[][] outDimTagsMap,
                                            bool removeObject, bool removeTool)
                {
                    Fragment(objectDimTags, toolDimTags, out outDimTags, out outDimTagsMap, -1, removeObject, removeTool);
                }
                public static void Fragment(Tuple<int, int>[] objectDimTags, Tuple<int, int>[] toolDimTags,
                                            out Tuple<int, int>[] outDimTags, out Tuple<int, int>[][] outDimTagsMap, int tag,
                                            bool removeObject, bool removeTool)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    int** map;
                    nuint* mapN;
                    nuint mapNN;
                    int[] objects = Native.Flatten(objectDimTags);
                    int[] tools = Native.Flatten(toolDimTags);
                    Native.gmshModelOccFragment(objects, Native.N(objects), tools, Native.N(tools), &p, &n, &map, &mapN, &mapNN,
                                                tag, removeObject ? 1 : 0, removeTool ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.fragment");
                    outDimTags = Native.TakeDimTags(p, n);
                    outDimTagsMap = Native.TakeDimTagsMap(map, mapN, mapNN);
                }
                public static void HealShapes(out Tuple<int, int>[] outDimTags, Tuple<int, int>[] dimTags, double tolerance,
                                              bool fixDegenerate, bool fixSmallEdges, bool fixSmallFaces, bool sewFaces,
                                              bool makeSolids)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccHealShapes(&p, &n, flat, Native.N(flat), tolerance, fixDegenerate ? 1 : 0,
                                                  fixSmallEdges ? 1 : 0, fixSmallFaces ? 1 : 0, sewFaces ? 1 : 0,
                                                  makeSolids ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.healShapes");
                    outDimTags = Native.TakeDimTags(p, n);
                }
                public static void Intersect(Tuple<int, int>[] objectDimTags, Tuple<int, int>[] toolDimTags,
                                             out Tuple<int, int>[] outDimTags, out Tuple<int, int>[][] outDimTagsMap, int tag,
                                             bool removeObject, bool removeTool)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    int** map;
                    nuint* mapN;
                    nuint mapNN;
                    int[] objects = Native.Flatten(objectDimTags);
                    int[] tools = Native.Flatten(toolDimTags);
                    Native.gmshModelOccIntersect(objects, Native.N(objects), tools, Native.N(tools), &p, &n, &map, &mapN,
                                                 &mapNN, tag, removeObject ? 1 : 0, removeTool ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.intersect");
                    outDimTags = Native.TakeDimTags(p, n);
                    outDimTagsMap = Native.TakeDimTagsMap(map, mapN, mapNN);
                }
                public static void GetMass(int dim, int tag, out double mass)
                {
                    int ierr;
                    double value = 0;
                    Native.gmshModelOccGetMass(dim, tag, &value, &ierr);
                    Native.Check(ierr, "model.occ.getMass");
                    mass = value;
                }
                public static void GetCenterOfMass(int dim, int tag, out double x, out double y, out double z)
                {
                    int ierr;
                    double a = 0, b = 0, c = 0;
                    Native.gmshModelOccGetCenterOfMass(dim, tag, &a, &b, &c, &ierr);
                    Native.Check(ierr, "model.occ.getCenterOfMass");
                    x = a; y = b; z = c;
                }
                public static void GetEntitiesInBoundingBox(double xMin, double yMin, double zMin, double xMax, double yMax,
                                                            double zMax, out Tuple<int, int>[] outDimTags, int dim)
                {
                    int ierr;
                    int* p;
                    nuint n;
                    Native.gmshModelOccGetEntitiesInBoundingBox(xMin, yMin, zMin, xMax, yMax, zMax, &p, &n, dim, &ierr);
                    Native.Check(ierr, "model.occ.getEntitiesInBoundingBox");
                    outDimTags = Native.TakeDimTags(p, n);
                }
                public static int AddPoint(double x, double y, double z, int tag)
                {
                    return AddPoint(x, y, z, 0.0, tag);
                }
                public static int AddPoint(double x, double y, double z, double meshSize)
                {
                    return AddPoint(x, y, z, meshSize, -1);
                }
                public static int AddPoint(double x, double y, double z)
                {
                    return AddPoint(x, y, z, 0, -1);
                }
                public static int AddPoint(double x, double y, double z, double meshSize, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddPoint(x, y, z, meshSize, tag, &ierr);
                    Native.Check(ierr, "model.occ.addPoint");
                    return value;
                }
                public static int AddLine(int startTag, int endTag)
                {
                    return AddLine(startTag, endTag, -1);
                }
                public static int AddLine(int startTag, int endTag, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddLine(startTag, endTag, tag, &ierr);
                    Native.Check(ierr, "model.occ.addLine");
                    return value;
                }
                public static int AddBSpline(int[] pointTags, int tag, int degree, double[] weights, double[] knots,
                                             int[] multiplicities)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddBSpline(pointTags, Native.N(pointTags), tag, degree, weights,
                                                              Native.N(weights), knots, Native.N(knots), multiplicities,
                                                              Native.N(multiplicities), &ierr);
                    Native.Check(ierr, "model.occ.addBSpline");
                    return value;
                }
                public static int AddBSpline(int[] pointTags, int tag, int degree, double[] weights)
                {
                    return AddBSpline(pointTags, tag, degree, weights, new double[0], new int[0]);
                }
                public static int AddCurveLoop(int[] curveTags)
                {
                    return AddCurveLoop(curveTags, -1);
                }
                public static int AddCurveLoop(int[] curveTags, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddCurveLoop(curveTags, Native.N(curveTags), tag, &ierr);
                    Native.Check(ierr, "model.occ.addCurveLoop");
                    return value;
                }
                public static int AddPlaneSurface(int[] wireTags)
                {
                    return AddPlaneSurface(wireTags, -1);
                }
                public static int AddPlaneSurface(int[] wireTags, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddPlaneSurface(wireTags, Native.N(wireTags), tag, &ierr);
                    Native.Check(ierr, "model.occ.addPlaneSurface");
                    return value;
                }
                public static int AddBSplineSurface(int[] pointTags, int numPointsU, int tag, int degreeU, int degreeV,
                                                    double[] weights, double[] knotsU, double[] knotsV, int[] multiplicitiesU,
                                                    int[] multiplicitiesV, int[] wireTags, bool wire3d)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddBSplineSurface(pointTags, Native.N(pointTags), numPointsU, tag, degreeU,
                                                                     degreeV, weights, Native.N(weights), knotsU,
                                                                     Native.N(knotsU), knotsV, Native.N(knotsV),
                                                                     multiplicitiesU, Native.N(multiplicitiesU),
                                                                     multiplicitiesV, Native.N(multiplicitiesV), wireTags,
                                                                     Native.N(wireTags), wire3d ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.addBSplineSurface");
                    return value;
                }
                public static int AddBSplineSurface(int[] pointTags, int numPointsU, int tag, int degreeU, int degreeV,
                                                    double[] weights, int[] wireTags, bool wire3d)
                {
                    return AddBSplineSurface(pointTags, numPointsU, tag, degreeU, degreeV, weights, new double[0],
                                             new double[0], new int[0], new int[0], wireTags, wire3d);
                }
                public static int AddBSplineSurface(int[] pointTags, int numPointsU, int tag, int degreeU, int degreeV,
                                                    double[] weights)
                {
                    return AddBSplineSurface(pointTags, numPointsU, tag, degreeU, degreeV, weights, new double[0],
                                             new double[0], new int[0], new int[0], new int[0], false);
                }
                public static int AddWire(int[] curveTags, int tag, bool checkClosed)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddWire(curveTags, Native.N(curveTags), tag, checkClosed ? 1 : 0, &ierr);
                    Native.Check(ierr, "model.occ.addWire");
                    return value;
                }
                public static int AddSurfaceLoop(int[] surfaceTags)
                {
                    return AddSurfaceLoop(surfaceTags, -1);
                }
                public static int AddSurfaceLoop(int[] surfaceTags, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddSurfaceLoop(surfaceTags, Native.N(surfaceTags), tag, 0, &ierr);
                    Native.Check(ierr, "model.occ.addSurfaceLoop");
                    return value;
                }
                public static int AddVolume(int[] shellTags)
                {
                    return AddVolume(shellTags, -1);
                }
                public static int AddVolume(int[] shellTags, int tag)
                {
                    int ierr;
                    int value = Native.gmshModelOccAddVolume(shellTags, Native.N(shellTags), tag, &ierr);
                    Native.Check(ierr, "model.occ.addVolume");
                    return value;
                }
                public static void SetSize(Tuple<int, int>[] dimTags, double size)
                {
                    int ierr;
                    int[] flat = Native.Flatten(dimTags);
                    Native.gmshModelOccMeshSetSize(flat, Native.N(flat), size, &ierr);
                    Native.Check(ierr, "model.occ.mesh.setSize");
                }
                public static class Mesh { }
            }
        }

        public static class Logger
        {
            public static void Write(string message, string level)
            {
                int ierr;
                Native.gmshLoggerWrite(Native.S(message), Native.S(level), &ierr);
                Native.Check(ierr, "logger.write");
            }
            public static void Start()
            {
                int ierr;
                Native.gmshLoggerStart(&ierr);
                Native.Check(ierr, "logger.start");
            }
            public static string[] Get()
            {
                int ierr;
                IntPtr* p;
                nuint n;
                Native.gmshLoggerGet(&p, &n, &ierr);
                Native.Check(ierr, "logger.get");
                string[] log = new string[(int)n];
                for (int i = 0; i < log.Length; i++) log[i] = Native.TakeString(p[i]);
                if (p != null) Native.gmshFree(p);
                return log;
            }
            public static void Stop()
            {
                int ierr;
                Native.gmshLoggerStop(&ierr);
                Native.Check(ierr, "logger.stop");
            }
            public static string GetLastError()
            {
                int ierr;
                IntPtr error;
                Native.gmshLoggerGetLastError(&error, &ierr);
                return Native.TakeString(error);
            }
        }
    }
}
