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
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // Constraints, contact pairs and surface interactions. A constraint carries either one region or a
    // master and a slave pair, and each kind is rebuilt from the selection it was created with.
    public static class PythonInteractionEmitter
    {
        // Methods                                                                                                                  
        public static void Constraint(PythonJournalWriter writer, string oldName, Constraint constraint)
        {
            const string collection = "pmx.model.constraints";
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(constraint.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method = verb + "_" + AddConstraintArguments(writer, arguments, constraint);
            // A replace keeps the expansion region it does not name, so one the model would discard is cleared
            if (oldName != null && constraint is CyclicSymmetry cyclicSymmetry && cyclicSymmetry.NumberOfGraphSectors < 2)
                arguments.Add("clear_expansion_region=" + PythonValueFormatter.Format(true));
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, constraint);
            writer.CountEmittedCommand();
        }
        public static void ContactPair(PythonJournalWriter writer, string oldName, ContactPair contactPair)
        {
            const string collection = "pmx.model.contact_pairs";
            string master = MasterRegion(writer, contactPair.MasterRegionName, contactPair.MasterRegionType,
                                         contactPair.MasterCreationData);
            string slave = PythonRegionEmitter.Emit(writer, contactPair.SlaveRegionName,
                                                    contactPair.SlaveRegionType, contactPair.SlaveCreationData);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(contactPair.Name));
            arguments.Add(PythonValueFormatter.Text(contactPair.SurfaceInteractionName));
            arguments.Add(master);
            arguments.Add(slave);
            arguments.Add("method=" + PythonValueFormatter.Text(contactPair.Method.ToString()));
            // SurfaceToSurface clears small sliding, so it is emitted only where it survives
            if (contactPair.Method != ContactPairMethod.SurfaceToSurface)
                arguments.Add("small_sliding=" + PythonValueFormatter.Format(contactPair.SmallSliding));
            arguments.Add("adjust=" + PythonValueFormatter.Format(contactPair.Adjust));
            if (contactPair.Adjust)
                arguments.Add("adjustment_size=" + PythonValueFormatter.Number(contactPair.AdjustmentSize));
            //
            writer.Statement(writer.Call(collection, oldName == null ? "add" : "replace", arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, contactPair);
            writer.CountEmittedCommand();
        }
        public static void SurfaceInteraction(PythonJournalWriter writer, string oldName,
                                              SurfaceInteraction interaction)
        {
            const string collection = "pmx.model.surface_interactions";
            List<string> makers = new List<string>();
            foreach (SurfaceInteractionProperty property in interaction.Properties)
                makers.Add(GetPropertyMaker(property));
            //
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(interaction.Name));
            arguments.Add("[" + string.Join(", ", makers) + "]");
            //
            writer.Statement(writer.Call(collection, oldName == null ? "add" : "replace", arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, interaction);
            writer.CountEmittedCommand();
        }
        private static string AddConstraintArguments(PythonJournalWriter writer, List<string> arguments,
                                                     Constraint constraint)
        {
            if (constraint is CyclicSymmetry cyclicSymmetry)
            {
                AddMasterSlave(writer, arguments, constraint);
                // The expansion region is discarded below two graph sectors, so it is emitted only when
                // the model kept it, whether it is a selection or a named element set
                bool hasExpansionRegion = cyclicSymmetry.CreationData != null ||
                                          (cyclicSymmetry.RegionType != CaeGlobals.RegionTypeEnum.Selection &&
                                           cyclicSymmetry.RegionType != CaeGlobals.RegionTypeEnum.None &&
                                           !string.IsNullOrEmpty(cyclicSymmetry.RegionName));
                if (cyclicSymmetry.NumberOfGraphSectors >= 2 && hasExpansionRegion)
                    arguments.Add("region=" + PythonRegionEmitter.Emit(writer, cyclicSymmetry.RegionName,
                                                                       cyclicSymmetry.RegionType,
                                                                       cyclicSymmetry.CreationData));
                arguments.Add("axis_point=" + Point(cyclicSymmetry.X, cyclicSymmetry.Y, cyclicSymmetry.Z));
                arguments.Add("axis_direction=" + Point(cyclicSymmetry.N1, cyclicSymmetry.N2,
                                                        cyclicSymmetry.N3));
                arguments.Add("number_of_sectors=" + cyclicSymmetry.NumberOfSectors);
                arguments.Add("number_of_graph_sectors=" + cyclicSymmetry.NumberOfGraphSectors);
                arguments.Add("check_sector_angle=" +
                              PythonValueFormatter.Format(cyclicSymmetry.CheckSectorAngle));
                AddTieValues(arguments, cyclicSymmetry);
                return "cyclic_symmetry";
            }
            if (constraint is Tie tie)
            {
                AddMasterSlave(writer, arguments, constraint);
                AddTieValues(arguments, tie);
                return "tie";
            }
            if (constraint is Joint joint)
            {
                AddMasterSlave(writer, arguments, constraint);
                arguments.Add("joint_type=" + PythonValueFormatter.Text(joint.JointType.ToString()));
                // A centre picked in the viewport is emitted as the coordinates it resolved to, which is what
                // the picking produced and what a script can reproduce
                bool centerFromMasterSurface = joint.CenterDefinition == JointCenterDefinitionEnum.MasterSurface;
                arguments.Add("center_defined_by=" +
                              PythonValueFormatter.Text(centerFromMasterSurface ?
                                                        JointCenterDefinitionEnum.MasterSurface.ToString() :
                                                        JointCenterDefinitionEnum.Coordinates.ToString()));
                if (!centerFromMasterSurface) arguments.Add("center=" + Point(joint.X, joint.Y, joint.Z));
                arguments.Add("axis_defined_by=" + PythonValueFormatter.Text(joint.AxisDefinition.ToString()));
                if (joint.AxisDefinition == JointDefinitionEnum.UserDefined)
                    arguments.Add("axis_direction=" + Point(joint.N1, joint.N2, joint.N3));
                arguments.Add("formulation=" + PythonValueFormatter.Text(joint.Formulation.ToString()));
                if (joint.HasSprings)
                {
                    arguments.Add("stiffness_defined_by=" + PythonValueFormatter.Text(joint.StiffnessDefinition.ToString()));
                    if (joint.HasUserDefinedStiffness)
                        arguments.Add("stiffness=" + PythonValueFormatter.Equation(joint.Stiffness));
                }
                return "joint";
            }
            if (constraint is DistributingCoupling distributing)
            {
                AddCouplingArguments(writer, arguments, distributing);
                AddFlag(arguments, "u1", distributing.U1);
                AddFlag(arguments, "u2", distributing.U2);
                AddFlag(arguments, "u3", distributing.U3);
                AddFlag(arguments, "ur1", distributing.UR1);
                AddFlag(arguments, "ur2", distributing.UR2);
                AddFlag(arguments, "ur3", distributing.UR3);
                return "distributing_coupling";
            }
            if (constraint is KinematicCoupling kinematic)
            {
                AddCouplingArguments(writer, arguments, kinematic);
                AddFlag(arguments, "u1", kinematic.U1);
                AddFlag(arguments, "u2", kinematic.U2);
                AddFlag(arguments, "u3", kinematic.U3);
                return "kinematic_coupling";
            }
            if (constraint is RigidBody rigidBody)
            {
                arguments.Add(PythonValueFormatter.Text(rigidBody.ReferencePointName));
                arguments.Add(PythonRegionEmitter.Emit(writer, rigidBody.RegionName, rigidBody.RegionType,
                                                       rigidBody.CreationData));
                return "rigid_body";
            }
            if (constraint is CompressionOnly compressionOnly)
            {
                arguments.Add(PythonRegionEmitter.Emit(writer, compressionOnly.RegionName,
                                                       compressionOnly.RegionType,
                                                       compressionOnly.CreationData));
                arguments.Add("clearance=" + PythonValueFormatter.Equation(compressionOnly.Clearance));
                arguments.Add("spring_stiffness=" +
                              PythonValueFormatter.Equation(compressionOnly.SpringStiffness));
                arguments.Add("tensile_force_at_negative_infinity=" +
                              PythonValueFormatter.Equation(compressionOnly.TensileForceAtNegativeInfinity));
                arguments.Add("offset=" + PythonValueFormatter.Equation(compressionOnly.Offset));
                arguments.Add("non_linear=" + PythonValueFormatter.Format(compressionOnly.NonLinear));
                return "compression_only";
            }
            if (constraint is SurfaceSpring surfaceSpring)
            {
                AddSpringArguments(writer, arguments, surfaceSpring);
                arguments.Add("stiffness_per_area=" +
                              PythonValueFormatter.Format(surfaceSpring.StiffnessPerArea));
                return "surface_spring";
            }
            if (constraint is PointSpring pointSpring)
            {
                AddSpringArguments(writer, arguments, pointSpring);
                return "point_spring";
            }
            //
            throw new NotSupportedException("the constraint type " + constraint.GetType().Name +
                                            " has no API form");
        }
        private static void AddMasterSlave(PythonJournalWriter writer, List<string> arguments,
                                           Constraint constraint)
        {
            arguments.Add(MasterRegion(writer, constraint.MasterRegionName, constraint.MasterRegionType,
                                       constraint.MasterCreationData));
            arguments.Add(PythonRegionEmitter.Emit(writer, constraint.SlaveRegionName,
                                                   constraint.SlaveRegionType, constraint.SlaveCreationData));
        }
        private static void AddTieValues(List<string> arguments, Tie tie)
        {
            arguments.Add("position_tolerance=" + PythonValueFormatter.Equation(tie.PositionTolerance));
            arguments.Add("adjust=" + PythonValueFormatter.Format(tie.Adjust));
        }
        private static void AddCouplingArguments(PythonJournalWriter writer, List<string> arguments,
                                                 Coupling coupling)
        {
            arguments.Add(PythonValueFormatter.Text(coupling.ReferencePointName));
            arguments.Add(PythonRegionEmitter.Emit(writer, coupling.RegionName, coupling.RegionType,
                                                   coupling.CreationData));
        }
        private static void AddSpringArguments(PythonJournalWriter writer, List<string> arguments,
                                               SpringConstraint spring)
        {
            arguments.Add(PythonRegionEmitter.Emit(writer, spring.RegionName, spring.RegionType,
                                                   spring.CreationData));
            AddStiffness(arguments, "k1", spring.K1);
            AddStiffness(arguments, "k2", spring.K2);
            AddStiffness(arguments, "k3", spring.K3);
        }
        private static void AddStiffness(List<string> arguments, string name, CaeGlobals.EquationContainer value)
        {
            if (value == null || double.IsNaN(value.Value)) return;
            //
            arguments.Add(name + "=" + PythonValueFormatter.Equation(value));
        }
        private static void AddFlag(List<string> arguments, string name, bool value)
        {
            arguments.Add(name + "=" + PythonValueFormatter.Format(value));
        }
        private static string MasterRegion(PythonJournalWriter writer, string regionName,
                                           CaeGlobals.RegionTypeEnum regionType, CaeGlobals.Selection creationData)
        {
            return PythonRegionEmitter.Emit(writer, regionName, regionType, creationData);
        }
        private static string GetPropertyMaker(SurfaceInteractionProperty property)
        {
            if (property is Friction friction)
                return PythonJournalWriter.Join("pmx.create.friction",
                                                PythonValueFormatter.Number(friction.Coefficient),
                                                "stick_slope=" + PythonValueFormatter.Number(friction.StickSlope));
            if (property is GapConductance gapConductance)
                return PythonJournalWriter.Join("pmx.create.gap_conductance",
                                                "conductance_table=" +
                                                Table(gapConductance.ConductnancePressureTemp));
            if (property is SurfaceBehavior surfaceBehavior)
                return PythonJournalWriter.Join("pmx.create.surface_behavior",
                                                PythonValueFormatter.Text(
                                                    surfaceBehavior.PressureOverclosureType.ToString()),
                                                "c0=" + PythonValueFormatter.Number(surfaceBehavior.C0),
                                                "p0=" + PythonValueFormatter.Number(surfaceBehavior.P0),
                                                "k=" + PythonValueFormatter.Number(surfaceBehavior.K),
                                                "s_inf=" + PythonValueFormatter.Number(surfaceBehavior.Sinf),
                                                "pressure_overclosure=" +
                                                Table(surfaceBehavior.PressureOverclosure));
            //
            throw new NotSupportedException("the surface interaction property " + property.GetType().Name +
                                            " has no pmx.create maker");
        }
        private static string Point(CaeGlobals.EquationContainer x, CaeGlobals.EquationContainer y,
                                    CaeGlobals.EquationContainer z)
        {
            return "[" + PythonValueFormatter.Equation(x) + ", " + PythonValueFormatter.Equation(y) + ", " +
                   PythonValueFormatter.Equation(z) + "]";
        }
        private static string Table(double[][] table)
        {
            if (table == null) return "None";
            //
            List<string> rows = new List<string>();
            foreach (double[] row in table) rows.Add(PythonValueFormatter.List(row));
            //
            return "[" + string.Join(", ", rows) + "]";
        }
    }
}
