using System;
using System.Collections.Generic;
using CaeMesh;

namespace CaeModel
{
    public static class BladeAeroAlgorithm
    {
        // Methods

        // Position along the spine that the first table column is measured from.
        // Absolute: the origin is taken as entered (0 → the table holds model coordinates).
        // Relative: ratio 0 is the mesh start unless the origin lies inside the mesh.
        public static double GetOriginAlongAxis(double[] origin, int axis, double minCoord,
                                                BladeAeroDistanceTypeEnum distanceType)
        {
            if (distanceType == BladeAeroDistanceTypeEnum.Absolute)
                return origin != null ? origin[axis] : 0.0;
            //
            return (origin != null && origin[axis] >= minCoord - 1e-9) ? origin[axis] : minCoord;
        }

        // Converts the first table column into absolute positions along the spine axis
        public static double[] ComputePositions(double[] distances, BladeAeroDistanceTypeEnum distanceType,
                                                double originAlongAxis, double spineLength)
        {
            double[] positions = new double[distances.Length];
            if (distanceType == BladeAeroDistanceTypeEnum.Absolute)
            {
                for (int i = 0; i < distances.Length; i++)
                    positions[i] = originAlongAxis + distances[i];
            }
            else
            {
                // Ratios: if any value > 1.0, the column is read as a percentage → divide by 100
                double maxRatio = 0;
                foreach (double d in distances)
                    if (d > maxRatio) maxRatio = d;
                double divisor = maxRatio > 1.0 ? 100.0 : 1.0;
                //
                for (int i = 0; i < distances.Length; i++)
                    positions[i] = originAlongAxis + distances[i] / divisor * spineLength;
            }
            return positions;
        }

        // axis: 0=X  1=Y  2=Z (spine direction);  forceAxis: force direction, must differ from axis;
        // origin: 3D origin point — origin[axis] defines the distance-0 position along the spine
        // distanceType: meaning of the first table column (absolute distance or span ratio)
        public static BladeAeroSection[] ComputeSections(FeMesh mesh,
                                                         (double distance, double force)[] csvRows,
                                                         int axis = 2,
                                                         int forceAxis = 0,
                                                         double[] origin = null,
                                                         BladeAeroDistanceTypeEnum distanceType =
                                                             BladeAeroDistanceTypeEnum.Absolute)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (csvRows == null || csvRows.Length == 0)
                throw new ArgumentException("No CSV rows provided.");
            //
            // Prefer shell surface nodes; fall back to all solid element nodes for fully solid meshes
            HashSet<int> sectionNodeIds = CollectShellNodeIds(mesh);
            if (sectionNodeIds.Count == 0)
                sectionNodeIds = CollectSolidNodeIds(mesh);
            if (sectionNodeIds.Count == 0)
                throw new Exception("No shell (2D) or solid (3D) elements found in the mesh. " +
                    "Blade aerodynamic loads require a shell or solid mesh.");
            //
            // Spine extent along chosen axis
            double minCoord = double.MaxValue, maxCoord = double.MinValue;
            foreach (int id in sectionNodeIds)
            {
                if (!mesh.Nodes.ContainsKey(id)) continue;
                double c = GetCoord(mesh.Nodes[id], axis);
                if (c < minCoord) minCoord = c;
                if (c > maxCoord) maxCoord = c;
            }
            if (maxCoord - minCoord < 1e-12)
                throw new Exception("Mesh has no extent along the chosen axis direction.");
            //
            double originAlongAxis = GetOriginAlongAxis(origin, axis, minCoord, distanceType);
            double spineLength = maxCoord - originAlongAxis;
            //
            // Convert the first column to absolute positions along the spine
            double[] distances = new double[csvRows.Length];
            for (int i = 0; i < csvRows.Length; i++) distances[i] = csvRows[i].distance;
            double[] positionValues = ComputePositions(distances, distanceType, originAlongAxis, spineLength);
            //
            var positions = new (double position, double force)[csvRows.Length];
            for (int i = 0; i < csvRows.Length; i++)
                positions[i] = (positionValues[i], csvRows[i].force);
            //
            double tolerance = ComputeTolerance(positions);
            //
            if (distanceType == BladeAeroDistanceTypeEnum.Absolute)
            {
                // A section may sit slightly beyond the last mesh layer and still catch its nodes
                for (int i = 0; i < positions.Length; i++)
                {
                    if (positions[i].position < minCoord - tolerance || positions[i].position > maxCoord + tolerance)
                        throw new Exception(string.Format(
                            "Row {0}: the distance {1:G6} is measured from the origin at {2}={3:G6} and places " +
                            "the section at {2}={4:G6}, outside the mesh, which spans {2}={5:G6} to {2}={6:G6}. " +
                            "Set the origin along {2} to the point the distances start from (0 if the table " +
                            "holds model coordinates, {5:G6} if they start at the mesh end), check the units of " +
                            "the first column, or set the distance type to 'Span ratio' if the values are " +
                            "ratios or percentages.",
                            i + 1, distances[i], AxisName(axis), originAlongAxis, positions[i].position,
                            minCoord, maxCoord));
                }
            }
            else
            {
                // Validate that the last span position reaches the mesh tip (ratio 1.0 = 100 %)
                double maxRatio = 0;
                foreach (double d in distances) if (d > maxRatio) maxRatio = d;
                double lastRatio = distances[distances.Length - 1] / (maxRatio > 1.0 ? 100.0 : 1.0);
                if (lastRatio < 1.0 - 1e-6)
                    throw new Exception(string.Format(
                        "The last CSV row is at {0:P1} of the blade span — the tip will have no section load. " +
                        "Add a row at 100 (or 1.0) so the tip section is included.",
                        lastRatio));
            }
            //
            var sections = new BladeAeroSection[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                double pos = positions[i].position;
                double force = positions[i].force;
                //
                int[] nodeIds = FindSectionNodes(sectionNodeIds, mesh, pos, tolerance, axis);
                if (nodeIds.Length < 3)
                    throw new Exception(string.Format(
                        "Section at {0}={1:G6}: only {2} node(s) found within tolerance {3:G4}. " +
                        "Try re-meshing so the plane coincides with a mesh layer.",
                        AxisName(axis), pos, nodeIds.Length, tolerance));
                //
                double[] bary = ComputeBarycenter(mesh, nodeIds);
                double[] dir = AxisVector(forceAxis);
                //
                sections[i] = new BladeAeroSection(i + 1, pos, force, nodeIds, bary, dir);
            }
            return sections;
        }

        private static double ComputeTolerance((double position, double force)[] positions)
        {
            if (positions.Length > 1)
            {
                double minGap = double.MaxValue;
                for (int i = 1; i < positions.Length; i++)
                {
                    double gap = Math.Abs(positions[i].position - positions[i - 1].position);
                    if (gap > 1e-12 && gap < minGap) minGap = gap;
                }
                if (minGap < double.MaxValue)
                    return Math.Max(minGap / 4.0, 0.01);
            }
            return 0.5;
        }

        public static HashSet<int> CollectShellNodeIds(FeMesh mesh)
        {
            var ids = new HashSet<int>();
            foreach (var kvp in mesh.Elements)
            {
                if (kvp.Value is FeElement2D)
                {
                    foreach (int id in kvp.Value.NodeIds)
                        ids.Add(id);
                }
            }
            return ids;
        }

        public static HashSet<int> CollectSolidNodeIds(FeMesh mesh)
        {
            var ids = new HashSet<int>();
            foreach (var kvp in mesh.Elements)
            {
                if (kvp.Value is FeElement3D)
                {
                    foreach (int id in kvp.Value.NodeIds)
                        ids.Add(id);
                }
            }
            return ids;
        }

        private static int[] FindSectionNodes(HashSet<int> candidateNodeIds, FeMesh mesh,
                                              double target, double tolerance, int axis)
        {
            var ids = new List<int>();
            foreach (int id in candidateNodeIds)
            {
                if (!mesh.Nodes.ContainsKey(id)) continue;
                if (Math.Abs(GetCoord(mesh.Nodes[id], axis) - target) <= tolerance)
                    ids.Add(id);
            }
            return ids.ToArray();
        }

        private static double[] ComputeBarycenter(FeMesh mesh, int[] nodeIds)
        {
            double sx = 0, sy = 0, sz = 0;
            foreach (int id in nodeIds)
            {
                FeNode n = mesh.Nodes[id];
                sx += n.X; sy += n.Y; sz += n.Z;
            }
            int cnt = nodeIds.Length;
            return new double[] { sx / cnt, sy / cnt, sz / cnt };
        }

        private static double[] AxisVector(int axis)
        {
            double[] v = new double[3];
            v[axis] = 1.0;
            return v;
        }

        private static double GetCoord(FeNode node, int axis)
        {
            if (axis == 0) return node.X;
            if (axis == 1) return node.Y;
            return node.Z;
        }

        private static double GetCoord(double[] coords, int axis)
        {
            return coords[axis];
        }

        private static string AxisName(int axis)
        {
            if (axis == 0) return "X";
            if (axis == 1) return "Y";
            return "Z";
        }

        // axis: 0=X  1=Y  2=Z;  origin[axis] defines the distance-0 position (default null → face-centroid min)
        // csvRows second column is force magnitude (N); pressure = force / zone_area is computed per zone
        public static BladeAeroPressureSection[] ComputePressureSections(
            FeMesh mesh,
            (double distance, double force)[] csvRows,
            string sourceSurfaceName,
            int axis = 2,
            double[] origin = null,
            BladeAeroDistanceTypeEnum distanceType = BladeAeroDistanceTypeEnum.Absolute)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (csvRows == null || csvRows.Length == 0)
                throw new ArgumentException("No CSV rows provided.");
            if (string.IsNullOrEmpty(sourceSurfaceName))
                throw new ArgumentException("A face surface name is required for pressure mode.");
            if (!mesh.Surfaces.ContainsKey(sourceSurfaceName))
                throw new Exception("Surface '" + sourceSurfaceName + "' not found in the mesh.");
            //
            FeSurface surf = mesh.Surfaces[sourceSurfaceName];
            if (surf.Type != FeSurfaceType.Element)
                throw new Exception("Surface '" + sourceSurfaceName + "' is a node-type surface. " +
                    "Select an element-type surface (created from solid element faces).");
            if (surf.ElementFaces == null || surf.ElementFaces.Count == 0)
                throw new Exception("Surface '" + sourceSurfaceName + "' has no element face data. " +
                    "Re-create the surface by selecting faces of solid elements in the model.");
            //
            // Enumerate all (elemId, faceName) pairs with face centroid and face area
            var faceEntries = new List<(int elemId, FeFaceName faceName, double axisCoord, double faceArea)>();
            foreach (var kvp in surf.ElementFaces)
            {
                FeFaceName faceName = kvp.Key;
                string elemSetName = kvp.Value;
                if (!mesh.ElementSets.ContainsKey(elemSetName)) continue;
                FeElementSet elSet = mesh.ElementSets[elemSetName];
                foreach (int elemId in elSet.Labels)
                {
                    if (!mesh.Elements.ContainsKey(elemId)) continue;
                    FeElement elem = mesh.Elements[elemId];
                    double area;
                    double[] cg = elem.GetFaceCG(faceName, mesh.Nodes, out area);
                    if (cg == null) continue;
                    faceEntries.Add((elemId, faceName, GetCoord(cg, axis), area));
                }
            }
            //
            if (faceEntries.Count == 0)
                throw new Exception("No element faces could be evaluated from surface '" +
                    sourceSurfaceName + "'.");
            //
            // Spine extent from face centroids
            double minCoord = double.MaxValue, maxCoord = double.MinValue;
            foreach (var fe in faceEntries)
            {
                if (fe.axisCoord < minCoord) minCoord = fe.axisCoord;
                if (fe.axisCoord > maxCoord) maxCoord = fe.axisCoord;
            }
            if (maxCoord - minCoord < 1e-12)
                throw new Exception("Surface has no extent along the chosen axis direction.");
            //
            double originAlongAxis = GetOriginAlongAxis(origin, axis, minCoord, distanceType);
            double spineLength = maxCoord - originAlongAxis;
            //
            // Convert the first column to absolute positions, keeping the force value per zone
            double[] distances = new double[csvRows.Length];
            for (int i = 0; i < csvRows.Length; i++) distances[i] = csvRows[i].distance;
            double[] positionValues = ComputePositions(distances, distanceType, originAlongAxis, spineLength);
            //
            var positions = new (double position, double force)[csvRows.Length];
            for (int i = 0; i < csvRows.Length; i++)
                positions[i] = (positionValues[i], csvRows[i].force);
            //
            if (distanceType == BladeAeroDistanceTypeEnum.Absolute)
            {
                double slack = ComputeTolerance(positions);
                for (int i = 0; i < positions.Length; i++)
                {
                    if (positions[i].position < minCoord - slack || positions[i].position > maxCoord + slack)
                        throw new Exception(string.Format(
                            "Row {0}: the distance {1:G6} is measured from the origin at {2}={3:G6} and places " +
                            "the zone at {2}={4:G6}, outside the surface, which spans {2}={5:G6} to {2}={6:G6}. " +
                            "In absolute mode the table must hold {2} coordinates of the surface. Check the " +
                            "units of the first column, or set the distance type to 'Span ratio' if the values " +
                            "are ratios or percentages.",
                            i + 1, distances[i], AxisName(axis), originAlongAxis, positions[i].position,
                            minCoord, maxCoord));
                }
            }
            else
            {
                // Validate last row reaches tip
                double maxRatio = 0;
                foreach (double d in distances) if (d > maxRatio) maxRatio = d;
                double lastRatio = distances[distances.Length - 1] / (maxRatio > 1.0 ? 100.0 : 1.0);
                if (lastRatio < 1.0 - 1e-6)
                    throw new Exception(string.Format(
                        "The last CSV row is at {0:P1} of the span — the tip will have no pressure zone. " +
                        "Add a row at 100 (or 1.0) so all faces are covered.",
                        lastRatio));
            }
            //
            // Voronoi zone assignment: each face → nearest section position; accumulate zone area
            var zones = new Dictionary<FeFaceName, List<int>>[positions.Length];
            var zoneAreas = new double[positions.Length];
            for (int i = 0; i < zones.Length; i++)
                zones[i] = new Dictionary<FeFaceName, List<int>>();
            //
            foreach (var fe in faceEntries)
            {
                int closest = 0;
                double minDist = Math.Abs(fe.axisCoord - positions[0].position);
                for (int i = 1; i < positions.Length; i++)
                {
                    double d = Math.Abs(fe.axisCoord - positions[i].position);
                    if (d < minDist) { minDist = d; closest = i; }
                }
                if (!zones[closest].ContainsKey(fe.faceName))
                    zones[closest][fe.faceName] = new List<int>();
                zones[closest][fe.faceName].Add(fe.elemId);
                zoneAreas[closest] += fe.faceArea;
            }
            //
            // Build result: pressure = force / zone_area
            var sections = new BladeAeroPressureSection[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                double pressure = (zoneAreas[i] > 1e-20) ? positions[i].force / zoneAreas[i] : 0;
                var faceGroups = new Dictionary<FeFaceName, int[]>();
                foreach (var kv in zones[i])
                    faceGroups[kv.Key] = kv.Value.ToArray();
                sections[i] = new BladeAeroPressureSection(i + 1, positions[i].position,
                                                            pressure, zoneAreas[i], faceGroups);
            }
            return sections;
        }
    }
}
