using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;
using CaeMesh;

namespace TestExtractShellMesh
{
    // Tests FeMesh.ExtractShellMeshFromSolidPart: the order of the extracted shell must follow the
    // secondOrder flag of the dialog, whatever the order of the source solid, and the extraction
    // must never throw.
    internal class Program
    {
        private static int _passed;
        private static int _failed;

        static int Main(string[] args)
        {
            Console.WriteLine("=== Extract shell mesh test ===");
            Console.WriteLine();
            //
            TestParabolicHexaSource();
            TestLinearHexaSource();
            TestParabolicTetraSource();
            TestExtractRollbackOnFailure();
            TestExtrudeRollbackOnFailure();
            TestDeleteExtractedShellPart();
            TestDeleteSourceSolidPart();
            TestDeleteExtrudedSolidAndSourceShell();
            TestCreatePartsFromElementSetsRollback();
            //
            Console.WriteLine();
            Console.WriteLine("=== " + _passed + " passed, " + _failed + " failed ===");
            return _failed == 0 ? 0 : 1;
        }


        //
        // Tests
        //
        private static void TestParabolicHexaSource()
        {
            Console.WriteLine("--- C3D20 source ---");
            // Second order requested: the face of a parabolic solid is already parabolic, so no
            // conversion must be attempted (this used to throw NotSupportedException)
            Extract(BuildParabolicHexaMesh(), 1, FeFaceName.S2, true, "C3D20 + second order",
                    typeof(ParabolicQuadrilateralElement), 8);
            // Linear requested: the midside nodes of the face must be dropped
            Extract(BuildParabolicHexaMesh(), 1, FeFaceName.S2, false, "C3D20 + linear",
                    typeof(LinearQuadrilateralElement), 4);
            Console.WriteLine();
        }

        private static void TestLinearHexaSource()
        {
            Console.WriteLine("--- C3D8 source ---");
            Extract(BuildLinearHexaMesh(), 1, FeFaceName.S2, false, "C3D8 + linear",
                    typeof(LinearQuadrilateralElement), 4);
            Extract(BuildLinearHexaMesh(), 1, FeFaceName.S2, true, "C3D8 + second order",
                    typeof(ParabolicQuadrilateralElement), 8);
            Console.WriteLine();
        }

        private static void TestParabolicTetraSource()
        {
            Console.WriteLine("--- C3D10 source ---");
            Extract(BuildParabolicTetraMesh(), 1, FeFaceName.S1, true, "C3D10 + second order",
                    typeof(ParabolicTriangleElement), 6);
            Extract(BuildParabolicTetraMesh(), 1, FeFaceName.S1, false, "C3D10 + linear",
                    typeof(LinearTriangleElement), 3);
            Console.WriteLine();
        }

        // An extraction that throws after it has already inserted shell elements must leave the mesh
        // exactly as it was. Without the rollback the inserted elements keep their default PartId of
        // -1, belong to no part, and can never be seen, selected or deleted again - they only show up
        // as "elements are missing a section assignment" on every analysis run.
        private static void TestExtractRollbackOnFailure()
        {
            Console.WriteLine("--- rollback of a failed extraction ---");
            FeMesh mesh = BuildTwoLinearHexaMesh();
            int elementsBefore = mesh.Elements.Count;
            int nodesBefore = mesh.Nodes.Count;
            int partsBefore = mesh.Parts.Count;
            int maxElementIdBefore = mesh.MaxElementId;
            int maxNodeIdBefore = mesh.MaxNodeId;
            HashSet<int> elementIdsBefore = new HashSet<int>(mesh.Elements.Keys);
            // A valid face of element 1 followed by an out of range face of element 2: the first shell
            // element is created and inserted, then GetVtkCellFromFaceName throws
            int validFaceId = 1 * 10 + ((int)FeFaceName.S2 - 1);
            int brokenFaceId = 2 * 10 + 6;
            //
            bool threw = false;
            try
            {
                mesh.ExtractShellMeshFromSolidPart(new int[] { validFaceId, brokenFaceId }, false, false,
                                                   "Shell", out _);
            }
            catch (Exception) { threw = true; }
            //
            Check("failed extraction: throws", threw, threw ? "ok" : "no exception");
            Check("failed extraction: no element added", mesh.Elements.Count == elementsBefore,
                  mesh.Elements.Count + " (expected " + elementsBefore + ")");
            Check("failed extraction: no node added", mesh.Nodes.Count == nodesBefore,
                  mesh.Nodes.Count + " (expected " + nodesBefore + ")");
            Check("failed extraction: no part added", mesh.Parts.Count == partsBefore,
                  mesh.Parts.Count + " (expected " + partsBefore + ")");
            bool sameElements = mesh.Elements.Keys.All(id => elementIdsBefore.Contains(id));
            Check("failed extraction: no orphan element left", sameElements, sameElements ? "ok" : "orphan found");
            Check("failed extraction: MaxElementId restored", mesh.MaxElementId == maxElementIdBefore,
                  mesh.MaxElementId + " (expected " + maxElementIdBefore + ")");
            Check("failed extraction: MaxNodeId restored", mesh.MaxNodeId == maxNodeIdBefore,
                  mesh.MaxNodeId + " (expected " + maxNodeIdBefore + ")");
            // The mesh must still be usable: the next extraction reuses the freed ids
            MeshPart newPart = null;
            bool retryOk = true;
            try { mesh.ExtractShellMeshFromSolidPart(new int[] { validFaceId }, false, false, "Shell", out newPart); }
            catch (Exception) { retryOk = false; }
            Check("failed extraction: retry succeeds", retryOk && newPart != null && newPart.Labels.Length == 1,
                  retryOk ? (newPart == null ? "no part" : newPart.Labels.Length + "") : "threw");
            if (retryOk && newPart != null && newPart.Labels.Length == 1)
                Check("failed extraction: retry reuses the freed element id",
                      newPart.Labels[0] == maxElementIdBefore + 1,
                      newPart.Labels[0] + " (expected " + (maxElementIdBefore + 1) + ")");
            Console.WriteLine();
        }

        // The extrusion adds one node per layer boundary before it knows whether any solid element can
        // be built, so ply regions that match nothing used to leave those nodes behind.
        private static void TestExtrudeRollbackOnFailure()
        {
            Console.WriteLine("--- rollback of a failed extrusion ---");
            FeMesh mesh = BuildSingleQuadShellMesh();
            string shellPartName = mesh.Parts.Keys.First();
            int elementsBefore = mesh.Elements.Count;
            int nodesBefore = mesh.Nodes.Count;
            int elementSetsBefore = mesh.ElementSets.Count;
            int maxNodeIdBefore = mesh.MaxNodeId;
            //
            bool threw = false;
            try
            {
                mesh.ExtrudeShellMeshFromSections(shellPartName, new string[] { "No_Such_Element_Set" },
                                                  new double[] { 1.0 }, 0, "Solid", false, 0, out _, out _);
            }
            catch (Exception) { threw = true; }
            //
            Check("failed extrusion: throws", threw, threw ? "ok" : "no exception");
            Check("failed extrusion: no node left behind", mesh.Nodes.Count == nodesBefore,
                  mesh.Nodes.Count + " (expected " + nodesBefore + ")");
            Check("failed extrusion: no element added", mesh.Elements.Count == elementsBefore,
                  mesh.Elements.Count + " (expected " + elementsBefore + ")");
            Check("failed extrusion: no element set added", mesh.ElementSets.Count == elementSetsBefore,
                  mesh.ElementSets.Count + " (expected " + elementSetsBefore + ")");
            Check("failed extrusion: MaxNodeId restored", mesh.MaxNodeId == maxNodeIdBefore,
                  mesh.MaxNodeId + " (expected " + maxNodeIdBefore + ")");
            Console.WriteLine();
        }

        // Deleting a part from the model tree calls FeMesh.RemoveParts, which deletes exactly the
        // elements listed in part.Labels. Every element the extraction created must therefore be in
        // the new part, otherwise deleting it leaves the rest behind as orphans.
        private static void TestDeleteExtractedShellPart()
        {
            Console.WriteLine("--- delete an extracted shell part ---");
            FeMesh mesh = BuildTwoLinearHexaMesh();
            int elementsBefore = mesh.Elements.Count;
            int nodesBefore = mesh.Nodes.Count;
            int faceId = 1 * 10 + ((int)FeFaceName.S2 - 1);
            //
            mesh.ExtractShellMeshFromSolidPart(new int[] { faceId }, false, true, "Shell", out MeshPart shellPart);
            Check("extract: no orphan after extraction", mesh.GetOrphanElementIds().Length == 0,
                  mesh.GetOrphanElementIds().Length + " orphan(s)");
            //
            mesh.RemoveParts(new string[] { shellPart.Name }, out _, false);
            int[] orphans = mesh.GetOrphanElementIds();
            Check("extract: no orphan after deleting the shell part", orphans.Length == 0,
                  orphans.Length + " orphan(s)");
            Check("extract: all shell elements deleted", mesh.Elements.Count == elementsBefore,
                  mesh.Elements.Count + " (expected " + elementsBefore + ")");
            Check("extract: all shell nodes deleted", mesh.Nodes.Count == nodesBefore,
                  mesh.Nodes.Count + " (expected " + nodesBefore + ")");
            Console.WriteLine();
        }

        // Deleting the solid the shell was extracted from must not touch the shell, and must not
        // strand any element. Run with condenseNodes = true so the shell reuses the solid nodes.
        private static void TestDeleteSourceSolidPart()
        {
            Console.WriteLine("--- delete the source solid part ---");
            FeMesh mesh = BuildTwoLinearHexaMesh();
            string solidPartName = mesh.Parts.Keys.First();
            int faceId = 1 * 10 + ((int)FeFaceName.S2 - 1);
            //
            mesh.ExtractShellMeshFromSolidPart(new int[] { faceId }, true, false, "Shell", out MeshPart shellPart);
            int shellElementCount = shellPart.Labels.Length;
            //
            mesh.RemoveParts(new string[] { solidPartName }, out _, false);
            int[] orphans = mesh.GetOrphanElementIds();
            Check("solid deleted: no orphan left", orphans.Length == 0, orphans.Length + " orphan(s)");
            Check("solid deleted: shell part survives", mesh.Parts.ContainsKey(shellPart.Name),
                  mesh.Parts.ContainsKey(shellPart.Name) ? "ok" : "gone");
            Check("solid deleted: shell elements survive", mesh.Elements.Count == shellElementCount,
                  mesh.Elements.Count + " (expected " + shellElementCount + ")");
            bool shellNodesAlive = shellPart.Labels.All(id => mesh.Elements[id].NodeIds.All(n => mesh.Nodes.ContainsKey(n)));
            Check("solid deleted: shared shell nodes survive", shellNodesAlive, shellNodesAlive ? "ok" : "node missing");
            Console.WriteLine();
        }

        // The extrusion leaves the source shell part in the model (deactivated). Deleting either the
        // extruded solid or the source shell afterwards must not strand elements of the other.
        private static void TestDeleteExtrudedSolidAndSourceShell()
        {
            Console.WriteLine("--- delete an extruded solid and its source shell ---");
            FeMesh mesh = BuildSingleQuadShellMesh();
            string shellPartName = mesh.Parts.Keys.First();
            int shellElementsBefore = mesh.Elements.Count;
            //
            MeshPart solidPart = mesh.ExtrudeShellMeshFromSections(shellPartName, new string[] { null },
                                                                   new double[] { 1.0 }, 0, "Solid", false, 0,
                                                                   out _, out _);
            Check("extrude: no orphan after extrusion", mesh.GetOrphanElementIds().Length == 0,
                  mesh.GetOrphanElementIds().Length + " orphan(s)");
            //
            mesh.RemoveParts(new string[] { solidPart.Name }, out _, false);
            int[] orphans = mesh.GetOrphanElementIds();
            Check("extrude: no orphan after deleting the solid", orphans.Length == 0, orphans.Length + " orphan(s)");
            Check("extrude: source shell survives", mesh.Elements.Count == shellElementsBefore,
                  mesh.Elements.Count + " (expected " + shellElementsBefore + ")");
            // Now delete the source shell too - the mesh must end up completely empty
            mesh.RemoveParts(new string[] { shellPartName }, out _, false);
            orphans = mesh.GetOrphanElementIds();
            Check("extrude: no orphan after deleting the shell", orphans.Length == 0, orphans.Length + " orphan(s)");
            Check("extrude: mesh is empty", mesh.Elements.Count == 0, mesh.Elements.Count + " element(s) left");
            Console.WriteLine();
        }

        // CreatePartsFromElementSets reassigns the part id of every element of the set and strips those
        // elements from the labels of the part they came from before it creates the part that receives
        // them. A throw in between used to leave them in no part at all. The element set is injected
        // straight into the collection because AddElementSet drops ids of non existing elements.
        private static void TestCreatePartsFromElementSetsRollback()
        {
            Console.WriteLine("--- rollback of a failed CreatePartsFromElementSets ---");
            FeMesh mesh = BuildTwoLinearHexaMesh();
            string partName = mesh.Parts.Keys.First();
            int partIdBefore = mesh.Parts[partName].PartId;
            int[] labelsBefore = (int[])mesh.Parts[partName].Labels.Clone();
            int partsBefore = mesh.Parts.Count;
            int element1PartIdBefore = mesh.Elements[1].PartId;
            int element2PartIdBefore = mesh.Elements[2].PartId;
            // A valid element followed by an id that no element uses: the first element is reassigned,
            // then the lookup of the dangling id throws
            mesh.ElementSets.Add("Broken", new FeElementSet("Broken", new int[] { 1, 2, 999 }));
            //
            bool threw = false;
            try { mesh.CreatePartsFromElementSets(new string[] { "Broken" }, out _, out _); }
            catch (Exception) { threw = true; }
            //
            Check("failed part creation: throws", threw, threw ? "ok" : "no exception");
            int[] orphans = mesh.GetOrphanElementIds();
            Check("failed part creation: no orphan element left", orphans.Length == 0, orphans.Length + " orphan(s)");
            Check("failed part creation: no part added", mesh.Parts.Count == partsBefore,
                  mesh.Parts.Count + " (expected " + partsBefore + ")");
            Check("failed part creation: source part survives", mesh.Parts.ContainsKey(partName),
                  mesh.Parts.ContainsKey(partName) ? "ok" : "gone");
            bool labelsOk = mesh.Parts.ContainsKey(partName) &&
                            mesh.Parts[partName].Labels.SequenceEqual(labelsBefore);
            Check("failed part creation: part labels restored", labelsOk,
                  labelsOk ? "ok" : "[" + string.Join(",", mesh.Parts[partName].Labels) + "] expected [" +
                  string.Join(",", labelsBefore) + "]");
            Check("failed part creation: part id unchanged",
                  mesh.Parts.ContainsKey(partName) && mesh.Parts[partName].PartId == partIdBefore,
                  mesh.Parts.ContainsKey(partName) ? mesh.Parts[partName].PartId.ToString() : "gone");
            Check("failed part creation: element part ids restored",
                  mesh.Elements[1].PartId == element1PartIdBefore && mesh.Elements[2].PartId == element2PartIdBefore,
                  mesh.Elements[1].PartId + "/" + mesh.Elements[2].PartId + " (expected " +
                  element1PartIdBefore + "/" + element2PartIdBefore + ")");
            Check("failed part creation: element set restored", mesh.ElementSets.ContainsKey("Broken"),
                  mesh.ElementSets.ContainsKey("Broken") ? "ok" : "gone");
            // The mesh must still be usable: a valid split of the same part must now succeed
            mesh.ElementSets.Remove("Broken");
            mesh.ElementSets.Add("Good", new FeElementSet("Good", new int[] { 2 }));
            bool retryOk = true;
            BasePart[] createdParts = null;
            try { mesh.CreatePartsFromElementSets(new string[] { "Good" }, out _, out createdParts); }
            catch (Exception) { retryOk = false; }
            Check("failed part creation: retry succeeds", retryOk && createdParts != null && createdParts.Length == 1,
                  retryOk ? (createdParts == null ? "no part" : createdParts.Length + "") : "threw");
            Check("failed part creation: no orphan after the retry", mesh.GetOrphanElementIds().Length == 0,
                  mesh.GetOrphanElementIds().Length + " orphan(s)");
            Console.WriteLine();
        }

        private static void Extract(FeMesh mesh, int elementId, FeFaceName faceName, bool secondOrder,
                                    string what, Type expectedType, int expectedNodeCount)
        {
            int faceId = elementId * 10 + ((int)faceName - 1);
            MeshPart newPart;
            try
            {
                mesh.ExtractShellMeshFromSolidPart(new int[] { faceId }, false, secondOrder, "Shell", out newPart);
            }
            catch (Exception ex)
            {
                Check(what + ": no exception", false, ex.GetType().Name + ": " + ex.Message);
                return;
            }
            Check(what + ": no exception", true, "ok");
            //
            if (newPart == null || newPart.Labels.Length != 1)
            {
                Check(what + ": one shell element", false, newPart == null ? "no part" : newPart.Labels.Length + "");
                return;
            }
            Check(what + ": one shell element", true, "1");
            //
            FeElement shell = mesh.Elements[newPart.Labels[0]];
            Check(what + ": element type", shell.GetType() == expectedType,
                  shell.GetType().Name + " (expected " + expectedType.Name + ")");
            Check(what + ": node count", shell.NodeIds.Length == expectedNodeCount,
                  shell.NodeIds.Length + " (expected " + expectedNodeCount + ")");
            // Every node of the shell must exist in the mesh
            bool allNodesExist = shell.NodeIds.All(nid => mesh.Nodes.ContainsKey(nid));
            Check(what + ": all shell nodes exist", allNodesExist, allNodesExist ? "ok" : "missing node");
            // The shell must be independent of the solid (condenseNodes = false)
            HashSet<int> solidNodeIds = new HashSet<int>(mesh.Elements[elementId].NodeIds);
            bool independent = !shell.NodeIds.Any(nid => solidNodeIds.Contains(nid));
            Check(what + ": nodes are independent of the solid", independent,
                  independent ? "ok" : "shares nodes");
            Check(what + ": part type is shell", newPart.PartType == PartType.Shell,
                  newPart.PartType.ToString());
        }


        //
        // Meshes
        //
        // Unit cube as a single C3D20: corners 1-8, midside nodes 9-20 in CalculiX order
        private static FeMesh BuildParabolicHexaMesh()
        {
            double[][] corners = new double[][]
            {
                new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 },
                new double[] { 1, 1, 0 }, new double[] { 0, 1, 0 },
                new double[] { 0, 0, 1 }, new double[] { 1, 0, 1 },
                new double[] { 1, 1, 1 }, new double[] { 0, 1, 1 }
            };
            // Edges carrying the midside nodes 9..20
            int[][] edges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 2, 3 }, new int[] { 3, 0 },
                new int[] { 4, 5 }, new int[] { 5, 6 }, new int[] { 6, 7 }, new int[] { 7, 4 },
                new int[] { 0, 4 }, new int[] { 1, 5 }, new int[] { 2, 6 }, new int[] { 3, 7 }
            };
            //
            var nodes = new Dictionary<int, FeNode>();
            for (int i = 0; i < corners.Length; i++)
                nodes.Add(i + 1, new FeNode(i + 1, corners[i][0], corners[i][1], corners[i][2]));
            for (int i = 0; i < edges.Length; i++)
            {
                int id = 9 + i;
                double[] a = corners[edges[i][0]];
                double[] b = corners[edges[i][1]];
                nodes.Add(id, new FeNode(id, 0.5 * (a[0] + b[0]), 0.5 * (a[1] + b[1]), 0.5 * (a[2] + b[2])));
            }
            //
            int[] nodeIds = new int[20];
            for (int i = 0; i < 20; i++) nodeIds[i] = i + 1;
            var elements = new Dictionary<int, FeElement>
            {
                { 1, new ParabolicHexaElement(1, nodeIds) }
            };
            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("C3D20", new HashSet<string> { "C3D20" }, new HashSet<int> { 1 })
            };
            return new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);
        }

        private static FeMesh BuildLinearHexaMesh()
        {
            double[][] corners = new double[][]
            {
                new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 },
                new double[] { 1, 1, 0 }, new double[] { 0, 1, 0 },
                new double[] { 0, 0, 1 }, new double[] { 1, 0, 1 },
                new double[] { 1, 1, 1 }, new double[] { 0, 1, 1 }
            };
            var nodes = new Dictionary<int, FeNode>();
            for (int i = 0; i < corners.Length; i++)
                nodes.Add(i + 1, new FeNode(i + 1, corners[i][0], corners[i][1], corners[i][2]));
            //
            var elements = new Dictionary<int, FeElement>
            {
                { 1, new LinearHexaElement(1, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 }) }
            };
            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("C3D8", new HashSet<string> { "C3D8" }, new HashSet<int> { 1 })
            };
            return new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);
        }

        // Two stacked C3D8 sharing their common face, elements 1 and 2
        private static FeMesh BuildTwoLinearHexaMesh()
        {
            var nodes = new Dictionary<int, FeNode>();
            int id = 1;
            for (int level = 0; level < 3; level++)
            {
                nodes.Add(id++, new FeNode(id - 1, 0, 0, level));
                nodes.Add(id++, new FeNode(id - 1, 1, 0, level));
                nodes.Add(id++, new FeNode(id - 1, 1, 1, level));
                nodes.Add(id++, new FeNode(id - 1, 0, 1, level));
            }
            var elements = new Dictionary<int, FeElement>
            {
                { 1, new LinearHexaElement(1, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 }) },
                { 2, new LinearHexaElement(2, new int[] { 5, 6, 7, 8, 9, 10, 11, 12 }) }
            };
            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("C3D8", new HashSet<string> { "C3D8" }, new HashSet<int> { 1, 2 })
            };
            return new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);
        }

        // Single S4 shell in the z = 0 plane
        private static FeMesh BuildSingleQuadShellMesh()
        {
            var nodes = new Dictionary<int, FeNode>
            {
                { 1, new FeNode(1, 0, 0, 0) }, { 2, new FeNode(2, 1, 0, 0) },
                { 3, new FeNode(3, 1, 1, 0) }, { 4, new FeNode(4, 0, 1, 0) }
            };
            var elements = new Dictionary<int, FeElement>
            {
                { 1, new LinearQuadrilateralElement(1, new int[] { 1, 2, 3, 4 }) }
            };
            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("S4", new HashSet<string> { "S4" }, new HashSet<int> { 1 })
            };
            return new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);
        }

        // Single C3D10: corners 1-4, midside nodes 5-10 in CalculiX order
        private static FeMesh BuildParabolicTetraMesh()
        {
            double[][] corners = new double[][]
            {
                new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 },
                new double[] { 0, 1, 0 }, new double[] { 0, 0, 1 }
            };
            int[][] edges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 2, 0 },
                new int[] { 0, 3 }, new int[] { 1, 3 }, new int[] { 2, 3 }
            };
            //
            var nodes = new Dictionary<int, FeNode>();
            for (int i = 0; i < corners.Length; i++)
                nodes.Add(i + 1, new FeNode(i + 1, corners[i][0], corners[i][1], corners[i][2]));
            for (int i = 0; i < edges.Length; i++)
            {
                int id = 5 + i;
                double[] a = corners[edges[i][0]];
                double[] b = corners[edges[i][1]];
                nodes.Add(id, new FeNode(id, 0.5 * (a[0] + b[0]), 0.5 * (a[1] + b[1]), 0.5 * (a[2] + b[2])));
            }
            //
            int[] nodeIds = new int[10];
            for (int i = 0; i < 10; i++) nodeIds[i] = i + 1;
            var elements = new Dictionary<int, FeElement>
            {
                { 1, new ParabolicTetraElement(1, nodeIds) }
            };
            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("C3D10", new HashSet<string> { "C3D10" }, new HashSet<int> { 1 })
            };
            return new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);
        }

        private static void Check(string what, bool condition, string detail)
        {
            if (condition) { _passed++; Console.WriteLine("  PASS  " + what + "  [" + detail + "]"); }
            else { _failed++; Console.WriteLine("  FAIL  " + what + "  [" + detail + "]"); }
        }
    }
}
