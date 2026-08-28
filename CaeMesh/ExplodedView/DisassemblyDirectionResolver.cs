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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The second stage of the disassembly direction exploded view: it turns the faces of every detected
    /// interface into the direction along which the second cluster of that interface separates from the
    /// first one.
    ///
    /// A planar face proposes its own area weighted normal and a curved face proposes the axis of the
    /// recognized analytic surface - the axis of a bore is the only direction along which a shaft can
    /// leave it. A sphere proposes nothing. In both cases the sign is the one that needs the shorter
    /// travel to clear the neighbour, which does not rely on the face normals pointing out of the
    /// material and therefore also holds for the shell parts.
    ///
    /// The candidates are merged by direction and weighted by the face area. An axis candidate always wins
    /// over a plane candidate, however much area the planes carry, because a bore constrains two degrees of
    /// freedom and a plane only one. Of two axes the one the most bores agree on wins, and of as many bores
    /// either way the wider one, because that is the one the part is located by. The confidence is the share of the interface area that agrees with the winner, counting
    /// only the axes once an axis has won. A winning plane below the limit is thrown away and the layout
    /// stage falls back to the radial direction, while a winning axis is always kept.
    /// </summary>
    public static class DisassemblyDirectionResolver
    {
        // Variables                                                                                                                
        public static double MergeAngleDeg = 15;
        public static double MaxAxisDeviationAngleDeg = 30;
        public static double MaxPenetrationAngleDeg = 15;
        public static double MinDirectionConfidence = 0.6;
        //
        private const double _tiny = 1E-12;


        // Methods                                                                                                                  
        public static void Resolve(FeMesh mesh, ExplodedViewCluster[] clusters, ExplodedViewInterface[] interfaces)
        {
            foreach (ExplodedViewInterface contactInterface in interfaces)
            {
                Resolve(mesh, clusters[contactInterface.FirstClusterId], clusters[contactInterface.SecondClusterId],
                        contactInterface);
            }
            //
            ApplySharedShaftDirections(clusters, interfaces);
        }
        /// <summary>
        /// Two parts pinned together by the same bolt can only be taken apart along that bolt, however their
        /// mating faces happen to be oriented. An interface that is not itself a shaft in a hole therefore
        /// takes over the axis of a shaft that runs through both of its clusters when there is one.
        ///
        /// Only the direction is taken over. The two parts are still mounted on each other over their own
        /// contact and neither of them becomes a part threaded onto the other, which is what the bolt they
        /// share is for.
        /// </summary>
        private static void ApplySharedShaftDirections(ExplodedViewCluster[] clusters,
                                                       ExplodedViewInterface[] interfaces)
        {
            List<ExplodedViewInterface>[] axialMountsByCluster = new List<ExplodedViewInterface>[clusters.Length];
            for (int i = 0; i < clusters.Length; i++) axialMountsByCluster[i] = new List<ExplodedViewInterface>();
            //
            foreach (ExplodedViewInterface contactInterface in interfaces)
            {
                if (!contactInterface.IsAxialMount) continue;
                //
                axialMountsByCluster[contactInterface.GetOtherClusterId(contactInterface.ShaftClusterId)]
                    .Add(contactInterface);
            }
            //
            foreach (ExplodedViewInterface contactInterface in interfaces)
            {
                if (contactInterface.IsAxialMount) continue;
                //
                ExplodedViewInterface sharedShaftInterface =
                    GetSharedShaftInterface(contactInterface, axialMountsByCluster);
                if (sharedShaftInterface == null) continue;
                //
                Vec3D direction = GetOrientedDirection(new Vec3D(sharedShaftInterface.Direction),
                                                       clusters[contactInterface.FirstClusterId],
                                                       clusters[contactInterface.SecondClusterId]);
                //
                contactInterface.SetDirection(direction, sharedShaftInterface.DirectionConfidence, false,
                                              sharedShaftInterface.ShaftClusterId);
            }
        }
        /// <summary>
        /// The mount of the largest shaft that runs through both clusters of the interface
        /// </summary>
        private static ExplodedViewInterface GetSharedShaftInterface(ExplodedViewInterface contactInterface,
                                                                     List<ExplodedViewInterface>[] axialMountsByCluster)
        {
            ExplodedViewInterface sharedShaftInterface = null;
            //
            foreach (ExplodedViewInterface firstMount in axialMountsByCluster[contactInterface.FirstClusterId])
            {
                foreach (ExplodedViewInterface secondMount in axialMountsByCluster[contactInterface.SecondClusterId])
                {
                    if (firstMount.ShaftClusterId != secondMount.ShaftClusterId) continue;
                    //
                    if (sharedShaftInterface == null || firstMount.Area > sharedShaftInterface.Area)
                        sharedShaftInterface = firstMount;
                }
            }
            //
            return sharedShaftInterface;
        }
        private static void Resolve(FeMesh mesh, ExplodedViewCluster firstCluster, ExplodedViewCluster secondCluster,
                                    ExplodedViewInterface contactInterface)
        {
            List<ExplodedViewDirectionCandidate> candidates = new List<ExplodedViewDirectionCandidate>();
            //
            double firstShaftArea = 0;
            double secondShaftArea = 0;
            foreach (ExplodedViewFace face in contactInterface.FirstFaces)
                AddCandidate(mesh, face, firstCluster, secondCluster, candidates, ref firstShaftArea);
            foreach (ExplodedViewFace face in contactInterface.SecondFaces)
                AddCandidate(mesh, face, firstCluster, secondCluster, candidates, ref secondShaftArea);
            //
            List<ExplodedViewDirectionCandidate> mergedCandidates = MergeCandidates(candidates);
            if (mergedCandidates.Count == 0) return;
            //
            ExplodedViewDirectionCandidate winner = GetWinner(mergedCandidates);
            if (winner == null) return;
            //
            double totalWeight = 0;
            double agreeingWeight = 0;
            foreach (ExplodedViewDirectionCandidate candidate in mergedCandidates)
            {
                // A plane does not vote against a winning axis: the bore already decides the direction and
                // only another bore pulling the other way makes the interface ambiguous
                if (winner.IsAxis && !candidate.IsAxis) continue;
                //
                totalWeight += candidate.Weight;
                if (Agrees(candidate, winner.Direction)) agreeingWeight += candidate.Weight;
            }
            if (totalWeight <= _tiny) return;
            //
            // A winning axis is kept whatever the confidence: when a part sits in two bores at an angle the
            // wider of them locates it and the narrower one must not throw the direction away
            double confidence = agreeingWeight / totalWeight;
            if (!winner.IsAxis && confidence < MinDirectionConfidence) return;
            //
            int shaftClusterId = -1;
            if (firstShaftArea > secondShaftArea) shaftClusterId = firstCluster.Id;
            else if (secondShaftArea > firstShaftArea) shaftClusterId = secondCluster.Id;
            //
            contactInterface.SetDirection(winner.Direction, confidence, winner.IsAxis, shaftClusterId);
        }
        private static void AddCandidate(FeMesh mesh, ExplodedViewFace face, ExplodedViewCluster firstCluster,
                                         ExplodedViewCluster secondCluster,
                                         List<ExplodedViewDirectionCandidate> candidates, ref double shaftArea)
        {
            Vec3D direction = null;
            bool isAxis = false;
            double radius = 0;
            GeomFaceType faceType = GetFaceType(face);
            //
            if (faceType == GeomFaceType.Sphere) return;
            else if (faceType == GeomFaceType.Plane) direction = new Vec3D(face.Normal);
            else
            {
                GeomFaceData faceData = FaceTypeRecognition.GetFaceData(face.Part.Visualization, mesh.Nodes, face.FaceId);
                //
                if (faceData != null && faceData.Type == GeomFaceType.Sphere) return;
                else if (faceData != null && faceData.Type == GeomFaceType.Plane) direction = new Vec3D(face.Normal);
                else if (faceData != null && faceData.Axis != null)
                {
                    direction = new Vec3D(faceData.Axis);
                    isAxis = true;
                    radius = faceData.Radius;
                    // A face that turns its back to the axis is the outside of a shaft, one that faces the
                    // axis is the inside of a hole
                    if (faceData.Convex) shaftArea += face.Area;
                }
                else if (face.NormalCoherence >= DisassemblyContactGraph.MinNormalCoherence)
                    direction = new Vec3D(face.Normal);
            }
            //
            if (direction == null || direction.Len2 <= _tiny) return;
            direction.Normalize();
            //
            candidates.Add(new ExplodedViewDirectionCandidate(GetOrientedDirection(direction, firstCluster, secondCluster),
                                                              face.Area, isAxis, radius));
        }
        /// <summary>
        /// A candidate only contradicts the winning direction when moving along it would violate the
        /// candidate. An axis has to be followed, so it must stay nearly parallel. A plane only blocks the
        /// motion that goes into its material, so a plane whose normal is perpendicular to the winner -
        /// the flank of a keyway next to a bore - agrees with it and must not lower the confidence.
        /// </summary>
        private static bool Agrees(ExplodedViewDirectionCandidate candidate, Vec3D direction)
        {
            double dotProduct = Vec3D.DotProduct(candidate.Direction, direction);
            //
            if (candidate.IsAxis) return dotProduct >= Math.Cos(MaxAxisDeviationAngleDeg * Math.PI / 180);
            else return dotProduct >= -Math.Sin(MaxPenetrationAngleDeg * Math.PI / 180);
        }
        private static GeomFaceType GetFaceType(ExplodedViewFace face)
        {
            GeomFaceType[] faceTypes = face.Part.Visualization.FaceTypes;
            if (faceTypes != null && face.FaceId < faceTypes.Length) return faceTypes[face.FaceId];
            else return GeomFaceType.Unknown;
        }
        /// <summary>
        /// Returns the sign along which the second cluster needs the shorter travel to clear the first
        /// one. The sign is taken from the travel and not from the orientation of the face normal, so a
        /// shell part, whose normal may point either way, is treated the same as a solid one.
        /// </summary>
        private static Vec3D GetOrientedDirection(Vec3D direction, ExplodedViewCluster firstCluster,
                                                  ExplodedViewCluster secondCluster)
        {
            double positiveTravel = GetTravelToClear(secondCluster.BoundingBox, firstCluster.BoundingBox, direction);
            double negativeTravel = GetTravelToClear(secondCluster.BoundingBox, firstCluster.BoundingBox, -1 * direction);
            //
            if (negativeTravel < positiveTravel) return -1 * direction;
            else if (positiveTravel < negativeTravel) return direction;
            // Both ways are equally long - keep the deterministic choice
            Vec3D separation = new Vec3D(secondCluster.Center) - new Vec3D(firstCluster.Center);
            if (Vec3D.DotProduct(separation, direction) < 0) return -1 * direction;
            else return direction;
        }
        private static double GetTravelToClear(BoundingBox movingBox, BoundingBox staticBox, Vec3D direction)
        {
            double movingMin;
            double movingMax;
            double staticMin;
            double staticMax;
            movingBox.GetProjection(direction.Coor, out movingMin, out movingMax);
            staticBox.GetProjection(direction.Coor, out staticMin, out staticMax);
            //
            double travel = staticMax - movingMin;
            if (travel < 0) travel = 0;
            return travel;
        }
        private static List<ExplodedViewDirectionCandidate> MergeCandidates(List<ExplodedViewDirectionCandidate> candidates)
        {
            double minDotProduct = Math.Cos(MergeAngleDeg * Math.PI / 180);
            List<ExplodedViewDirectionCandidate> mergedCandidates = new List<ExplodedViewDirectionCandidate>();
            //
            foreach (ExplodedViewDirectionCandidate candidate in candidates)
            {
                bool merged = false;
                foreach (ExplodedViewDirectionCandidate mergedCandidate in mergedCandidates)
                {
                    if (mergedCandidate.IsAxis != candidate.IsAxis) continue;
                    if (Vec3D.DotProduct(mergedCandidate.Direction, candidate.Direction) < minDotProduct) continue;
                    //
                    mergedCandidate.Weight += candidate.Weight;
                    mergedCandidate.FaceCount += candidate.FaceCount;
                    if (candidate.Radius > mergedCandidate.Radius) mergedCandidate.Radius = candidate.Radius;
                    merged = true;
                    break;
                }
                if (!merged)
                    mergedCandidates.Add(new ExplodedViewDirectionCandidate(candidate.Direction, candidate.Weight,
                                                                            candidate.IsAxis, candidate.Radius));
            }
            //
            return mergedCandidates;
        }
        /// <summary>
        /// The heaviest axis candidate whenever the interface has one, and the heaviest plane candidate
        /// otherwise. A bore leaves the shaft inside it only one way out, while a mating plane leaves it a
        /// whole plane of them, so an axis decides the direction even when the planes of the interface
        /// carry more area than it does.
        /// </summary>
        private static ExplodedViewDirectionCandidate GetWinner(List<ExplodedViewDirectionCandidate> candidates)
        {
            ExplodedViewDirectionCandidate bestAxis = null;
            ExplodedViewDirectionCandidate bestPlane = null;
            //
            foreach (ExplodedViewDirectionCandidate candidate in candidates)
            {
                if (candidate.IsAxis)
                {
                    if (bestAxis == null || IsStrongerAxis(candidate, bestAxis)) bestAxis = candidate;
                }
                else
                {
                    if (bestPlane == null || candidate.Weight > bestPlane.Weight) bestPlane = candidate;
                }
            }
            //
            if (bestAxis != null) return bestAxis;
            else return bestPlane;
        }
        /// <summary>
        /// The direction that the most bores agree on wins, since a row of holes holds the part more firmly
        /// than a single bore across them. Of as many bores either way the wider one wins, since that is the
        /// one the part is located by, and only when neither of them has a recognized radius does the larger
        /// contact area decide.
        /// </summary>
        private static bool IsStrongerAxis(ExplodedViewDirectionCandidate candidate,
                                           ExplodedViewDirectionCandidate bestCandidate)
        {
            if (candidate.FaceCount != bestCandidate.FaceCount)
                return candidate.FaceCount > bestCandidate.FaceCount;
            //
            if (candidate.Radius > 0 && bestCandidate.Radius > 0 && candidate.Radius != bestCandidate.Radius)
                return candidate.Radius > bestCandidate.Radius;
            //
            return candidate.Weight > bestCandidate.Weight;
        }
    }
}
