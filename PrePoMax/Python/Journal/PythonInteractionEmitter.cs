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
using System.Drawing;
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
            string method = verb + "_" + AddConstraintArguments(writer, arguments, constraint, oldName != null);
            AddConstraintColors(arguments, constraint, oldName != null);
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
            // Only NodeToSurface takes small sliding, so an add leaves it out for the other methods; a replace
            // keeps the small sliding it is not given and refuses another method over a kept one, so it always
            // writes it
            bool isNodeToSurface = contactPair.Method == ContactPairMethod.NodeToSurface;
            if (oldName != null || isNodeToSurface)
                arguments.Add("small_sliding=" +
                              PythonValueFormatter.Format(isNodeToSurface && contactPair.SmallSliding));
            arguments.Add("adjust=" + PythonValueFormatter.Format(contactPair.Adjust));
            // The Default size is stored as NaN: an add leaves it out, and a replace, which keeps a value it is not
            // given, writes float('nan'), which the API takes as Default
            if (contactPair.Adjust && !double.IsNaN(contactPair.AdjustmentSize))
                arguments.Add("adjustment_size=" + PythonValueFormatter.Number(contactPair.AdjustmentSize));
            else if (contactPair.Adjust && oldName != null) arguments.Add("adjustment_size=float('nan')");
            Color defaultColor = new ContactPair("Default", null, null, CaeGlobals.RegionTypeEnum.Selection, null,
                                                 CaeGlobals.RegionTypeEnum.Selection).MasterColor;
            AddColor(arguments, "master_color", contactPair.MasterColor, defaultColor, oldName != null);
            AddColor(arguments, "slave_color", contactPair.SlaveColor, defaultColor, oldName != null);
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
                                                     Constraint constraint, bool replace)
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
                {
                    PythonRegionEmitter.RefuseSelectionOtherThanParts(cyclicSymmetry.CreationData,
                                                                      "the expansion region of the cyclic symmetry '" +
                                                                      cyclicSymmetry.Name + "'");
                    arguments.Add("region=" + PythonRegionEmitter.Emit(writer, cyclicSymmetry.RegionName,
                                                                       cyclicSymmetry.RegionType,
                                                                       cyclicSymmetry.CreationData));
                }
                arguments.Add("axis_point=" + Point(cyclicSymmetry.X, cyclicSymmetry.Y, cyclicSymmetry.Z));
                arguments.Add("axis_direction=" + Point(cyclicSymmetry.N1, cyclicSymmetry.N2,
                                                        cyclicSymmetry.N3));
                arguments.Add("number_of_sectors=" + cyclicSymmetry.NumberOfSectors);
                arguments.Add("number_of_graph_sectors=" + cyclicSymmetry.NumberOfGraphSectors);
                arguments.Add("check_sector_angle=" +
                              PythonValueFormatter.Format(cyclicSymmetry.CheckSectorAngle));
                AddTieValues(arguments, cyclicSymmetry, replace);
                return "cyclic_symmetry";
            }
            if (constraint is Tie tie)
            {
                AddMasterSlave(writer, arguments, constraint);
                AddTieValues(arguments, tie, replace);
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
                // A Ball or a Weld joint has no axis, which the dialog then hides and the API refuses
                if (joint.HasAxis)
                    arguments.Add("axis_defined_by=" + PythonValueFormatter.Text(joint.AxisDefinition.ToString()));
                if (joint.HasUserDefinedAxis) arguments.Add("axis_direction=" + Point(joint.N1, joint.N2, joint.N3));
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
                AddDefaultable(arguments, "spring_stiffness=", compressionOnly.SpringStiffness, replace);
                AddDefaultable(arguments, "tensile_force_at_negative_infinity=",
                               compressionOnly.TensileForceAtNegativeInfinity, replace);
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
        // A tie, a cyclic symmetry and a joint color their two surfaces, every other constraint has one color
        private static void AddConstraintColors(List<string> arguments, Constraint constraint, bool replace)
        {
            Color defaultColor = new Tie("Default", null, CaeGlobals.RegionTypeEnum.Selection, null,
                                         CaeGlobals.RegionTypeEnum.Selection, false).MasterColor;
            if (constraint is Tie || constraint is Joint)
            {
                AddColor(arguments, "master_color", constraint.MasterColor, defaultColor, replace);
                AddColor(arguments, "slave_color", constraint.SlaveColor, defaultColor, replace);
            }
            else AddColor(arguments, "color", constraint.MasterColor, defaultColor, replace);
        }
        // Like PythonStepItemEmitter.AddColor, for an argument of another name: only a color other than the default
        // on an add, and always on a replace
        private static void AddColor(List<string> arguments, string argument, Color color, Color defaultColor,
                                     bool replace)
        {
            if (!replace && color.ToArgb() == defaultColor.ToArgb()) return;
            //
            arguments.Add(argument + "=" + PythonValueFormatter.Channels(color));
        }
        private static void AddMasterSlave(PythonJournalWriter writer, List<string> arguments,
                                           Constraint constraint)
        {
            arguments.Add(MasterRegion(writer, constraint.MasterRegionName, constraint.MasterRegionType,
                                       constraint.MasterCreationData));
            arguments.Add(PythonRegionEmitter.Emit(writer, constraint.SlaveRegionName,
                                                   constraint.SlaveRegionType, constraint.SlaveCreationData));
        }
        private static void AddTieValues(List<string> arguments, Tie tie, bool replace)
        {
            AddDefaultable(arguments, "position_tolerance=", tie.PositionTolerance, replace);
            arguments.Add("adjust=" + PythonValueFormatter.Format(tie.Adjust));
        }
        // A value the dialog offers as Default is stored as NaN. An add leaves it out, and a replace, which keeps a
        // value it is not given, writes float('nan'), which the API takes as Default
        private static void AddDefaultable(List<string> arguments, string argument, CaeGlobals.EquationContainer value,
                                           bool replace)
        {
            if (value == null) return;
            //
            if (!value.IsEquation() && double.IsNaN(value.Value))
            {
                if (replace) arguments.Add(argument + "float('nan')");
                return;
            }
            arguments.Add(argument + PythonValueFormatter.Equation(value));
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
            {
                // pmx.create refuses a value the pressure overclosure type does not use
                PressureOverclosureEnum type = surfaceBehavior.PressureOverclosureType;
                List<string> arguments = new List<string>();
                arguments.Add(PythonValueFormatter.Text(type.ToString()));
                if (type == PressureOverclosureEnum.Linear || type == PressureOverclosureEnum.Exponential)
                    arguments.Add("c0=" + PythonValueFormatter.Number(surfaceBehavior.C0));
                if (type == PressureOverclosureEnum.Exponential)
                    arguments.Add("p0=" + PythonValueFormatter.Number(surfaceBehavior.P0));
                if (type == PressureOverclosureEnum.Linear || type == PressureOverclosureEnum.Tied)
                    arguments.Add("k=" + PythonValueFormatter.Number(surfaceBehavior.K));
                if (type == PressureOverclosureEnum.Linear)
                    arguments.Add("s_inf=" + PythonValueFormatter.Number(surfaceBehavior.Sinf));
                if (type == PressureOverclosureEnum.Tabular)
                    arguments.Add("pressure_overclosure=" + Table(surfaceBehavior.PressureOverclosure));
                return PythonJournalWriter.Join("pmx.create.surface_behavior", arguments.ToArray());
            }
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
