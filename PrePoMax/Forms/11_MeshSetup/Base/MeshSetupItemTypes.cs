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
                DisplayName = "Shell Mmg", DefaultNameRoot = "Shell_Mmg", AvailableInTwoD = true,
                ItemType = typeof(ShellMmg), ViewType = typeof(ViewShellMmg),
                CreateDefaultView = (controller, name) => new ViewShellMmg(new ShellMmg(name)),
                CreateView = (controller, item) => new ViewShellMmg(((ShellMmg)item).DeepClone())
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
                DisplayName = "Shell QuadWild", DefaultNameRoot = "Shell_QuadWild", AvailableInTwoD = true,
                ItemType = typeof(ShellQuadWild), ViewType = typeof(ViewShellQuadWild),
                CreateDefaultView = (controller, name) => new ViewShellQuadWild(new ShellQuadWild(name)),
                CreateView = (controller, item) => new ViewShellQuadWild(((ShellQuadWild)item).DeepClone())
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
                DisplayName = "Netgen Mesh", DefaultNameRoot = "Netgen_Mesh", AvailableInTwoD = true,
                ItemType = typeof(NetgenMesh), ViewType = typeof(ViewNetgenMesh),
                CreateDefaultView = (controller, name) => new ViewNetgenMesh(new NetgenMesh(name)),
                CreateView = (controller, item) => new ViewNetgenMesh(((NetgenMesh)item).DeepClone())
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
            return GetIndex(GetAvailable(twoD), entry => entry.ItemType, meshSetupItem);
        }
        public static int GetIndex(bool twoD, ViewMeshSetupItem viewMeshSetupItem)
        {
            return GetIndex(GetAvailable(twoD), entry => entry.ViewType, viewMeshSetupItem);
        }
        public static ViewMeshSetupItem CreateView(Controller controller, MeshSetupItem meshSetupItem)
        {
            return _all[GetIndex(_all, entry => entry.ItemType, meshSetupItem)].CreateView(controller, meshSetupItem);
        }
        // The entry of the type itself, else of a type it derives from: a revolve item is a sweep item too
        private static int GetIndex(MeshSetupItemType[] entries, Func<MeshSetupItemType, Type> getType, object item)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (getType(entries[i]) == item.GetType()) return i;
            }
            for (int i = 0; i < entries.Length; i++)
            {
                if (getType(entries[i]).IsInstanceOfType(item)) return i;
            }
            throw new NotSupportedException("MeshSetupItemTypeException");
        }
    }
}
