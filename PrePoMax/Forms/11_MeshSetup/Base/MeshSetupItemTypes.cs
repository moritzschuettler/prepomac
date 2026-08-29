// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.Forms
{
    internal class MeshSetupItemType
    {
        public string DisplayName;
        public string DefaultNameRoot;
        public bool AvailableInTwoD;
        public Type ItemType;
        public Type ViewType;
        public Func<Controller, string, ViewMeshSetupItem> CreateDefaultView;
        public Func<Controller, MeshSetupItem, ViewMeshSetupItem> CreateView;
    }

    internal static class MeshSetupItemTypes
    {
        // Variables                                                                                                                
        private static readonly MeshSetupItemType[] _all = new MeshSetupItemType[]
        {
            new MeshSetupItemType()
            {
                DisplayName = "Meshing Parameters", DefaultNameRoot = "Meshing_Parameters", AvailableInTwoD = true,
                ItemType = typeof(MeshingParameters), ViewType = typeof(ViewMeshingParameters),
                CreateDefaultView = (controller, name) =>
                {
                    MeshingParameters meshingParameters = controller.GetDefaultMeshingParameters(name);
                    return new ViewMeshingParameters(meshingParameters, meshingParameters.RelativeSize);
                },
                CreateView = (controller, item) =>
                    new ViewMeshingParameters(((MeshingParameters)item).DeepClone(),
                                              controller.Settings.Meshing.MeshingParameters.RelativeSize)
            },
            new MeshSetupItemType()
            {
                DisplayName = "Local Mesh Size", DefaultNameRoot = "Local_Mesh_Size", AvailableInTwoD = true,
                ItemType = typeof(LocalMeshSize), ViewType = typeof(ViewLocalMeshSize),
                CreateDefaultView = (controller, name) => new ViewLocalMeshSize(new LocalMeshSize(name)),
                CreateView = (controller, item) => new ViewLocalMeshSize(((LocalMeshSize)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Shell Gmsh", DefaultNameRoot = "Shell_Gmsh", AvailableInTwoD = true,
                ItemType = typeof(ShellGmsh), ViewType = typeof(ViewShellGmsh),
                CreateDefaultView = (controller, name) => new ViewShellGmsh(new ShellGmsh(name)),
                CreateView = (controller, item) => new ViewShellGmsh(((ShellGmsh)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Thicken Shell Mesh", DefaultNameRoot = "Thicken_Shell_Mesh", AvailableInTwoD = true,
                ItemType = typeof(ThickenShellMesh), ViewType = typeof(ViewThickenShellMesh),
                CreateDefaultView = (controller, name) => new ViewThickenShellMesh(new ThickenShellMesh(name)),
                CreateView = (controller, item) => new ViewThickenShellMesh(((ThickenShellMesh)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Tetrahedral Gmsh", DefaultNameRoot = "Tetrahedral_Gmsh", AvailableInTwoD = true,
                ItemType = typeof(TetrahedralGmsh), ViewType = typeof(ViewTetrahedralGmsh),
                CreateDefaultView = (controller, name) => new ViewTetrahedralGmsh(new TetrahedralGmsh(name)),
                CreateView = (controller, item) => new ViewTetrahedralGmsh(((TetrahedralGmsh)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Transfinite Mesh", DefaultNameRoot = "Transfinite_Mesh", AvailableInTwoD = true,
                ItemType = typeof(TransfiniteMesh), ViewType = typeof(ViewTransfiniteMesh),
                CreateDefaultView = (controller, name) => new ViewTransfiniteMesh(new TransfiniteMesh(name)),
                CreateView = (controller, item) => new ViewTransfiniteMesh(((TransfiniteMesh)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Extrude Mesh", DefaultNameRoot = "Extrude_Mesh", AvailableInTwoD = false,
                ItemType = typeof(ExtrudeMesh), ViewType = typeof(ViewExtrudeMesh),
                CreateDefaultView = (controller, name) => new ViewExtrudeMesh(new ExtrudeMesh(name)),
                CreateView = (controller, item) => new ViewExtrudeMesh(((ExtrudeMesh)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Sweep Mesh", DefaultNameRoot = "Sweep_Mesh", AvailableInTwoD = false,
                ItemType = typeof(SweepMesh), ViewType = typeof(ViewSweepMesh),
                CreateDefaultView = (controller, name) => new ViewSweepMesh(new SweepMesh(name)),
                CreateView = (controller, item) => new ViewSweepMesh(((SweepMesh)item).DeepClone())
            },
            new MeshSetupItemType()
            {
                DisplayName = "Revolve Mesh", DefaultNameRoot = "Revolve_Mesh", AvailableInTwoD = false,
                ItemType = typeof(RevolveMesh), ViewType = typeof(ViewRevolveMesh),
                CreateDefaultView = (controller, name) => new ViewRevolveMesh(new RevolveMesh(name)),
                CreateView = (controller, item) => new ViewRevolveMesh(((RevolveMesh)item).DeepClone())
            }
        };


        // Methods                                                                                                                  
        public static MeshSetupItemType[] GetAvailable(bool twoD)
        {
            if (twoD) return _all.Where(entry => entry.AvailableInTwoD).ToArray();
            else return _all;
        }
        public static int GetIndex(bool twoD, MeshSetupItem meshSetupItem)
        {
            MeshSetupItemType[] available = GetAvailable(twoD);
            for (int i = 0; i < available.Length; i++)
            {
                if (available[i].ItemType.IsInstanceOfType(meshSetupItem)) return i;
            }
            throw new NotSupportedException("MeshSetupItemTypeException");
        }
        public static int GetIndex(bool twoD, ViewMeshSetupItem viewMeshSetupItem)
        {
            MeshSetupItemType[] available = GetAvailable(twoD);
            for (int i = 0; i < available.Length; i++)
            {
                if (available[i].ViewType.IsInstanceOfType(viewMeshSetupItem)) return i;
            }
            throw new NotSupportedException("MeshSetupItemTypeException");
        }
        public static ViewMeshSetupItem CreateView(Controller controller, MeshSetupItem meshSetupItem)
        {
            foreach (var entry in _all)
            {
                if (entry.ItemType.IsInstanceOfType(meshSetupItem)) return entry.CreateView(controller, meshSetupItem);
            }
            throw new NotSupportedException("MeshSetupItemTypeException");
        }
    }
}
