// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System.Drawing;

namespace APIs.Python.Model
{
    internal class ModelPartService : PartService
    {
        // Properties
        protected override string ViewName { get { return "femodel"; } }


        // Constructors
        public ModelPartService(ModelResolver modelResolver, SelectionService selectionService,
                                IPythonMeshGateway meshGateway, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, meshGateway, modelGateway)
        {
        }


        // Methods
        public void Translate(string[] partNames, object translateVector, int numberOfCopies)
        {
            double[] vector = ToVector(translateVector, "translate_vector");
            ValidateNumberOfCopies(numberOfCopies);
            EnsureCommandsAvailable();
            MeshGateway.TranslateModelParts(partNames, vector, numberOfCopies);
        }
        public void Rotate(string[] partNames, object rotateCenter, object rotateAxis, object rotateAngle,
                           int numberOfCopies)
        {
            double[] center = ToVector(rotateCenter, "rotate_center");
            double[] axis = ToVector(rotateAxis, "rotate_axis");
            double angle = PythonValueConverter.ToDouble(rotateAngle, "rotate_angle");
            ValidateNumberOfCopies(numberOfCopies);
            EnsureCommandsAvailable();
            MeshGateway.RotateModelParts(partNames, center, axis, angle, numberOfCopies);
        }
        public void Mirror(string[] partNames, object mirrorPoint, object mirrorDirection, bool copy)
        {
            double[] point = ToVector(mirrorPoint, "mirror_point");
            double[] direction = ToVector(mirrorDirection, "mirror_direction");
            EnsureCommandsAvailable();
            MeshGateway.MirrorModelParts(partNames, point, direction, copy);
        }
        public void Merge(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.MergeModelParts(partNames);
        }
        public void FindEdgesByAngle(string[] partNames, object edgeAngle)
        {
            double angle = PythonValueConverter.ToDouble(edgeAngle, "edge_angle");
            EnsureCommandsAvailable();
            MeshGateway.FindEdgesByAngleForModelParts(partNames, angle);
        }
        //
        protected override PartProperties GetPartProperties(string partName)
        {
            return MeshGateway.GetModelPartProperties(partName);
        }
        protected override FeMesh GetMeshOrNull()
        {
            return ModelResolver.GetModel().Mesh;
        }
        protected override void ReplacePartProperties(string oldPartName, PartProperties properties)
        {
            MeshGateway.ReplaceModelPartProperties(oldPartName, properties);
        }
        protected override void SetColorForParts(string[] partNames, Color color)
        {
            MeshGateway.SetColorForModelParts(partNames, color);
        }
        protected override void ResetColorForParts(string[] partNames)
        {
            MeshGateway.ResetColorForModelParts(partNames);
        }
        protected override void SetTransparencyForParts(string[] partNames, byte alpha)
        {
            MeshGateway.SetTransparencyForModelParts(partNames, alpha);
        }
        protected override void HideParts(string[] partNames)
        {
            MeshGateway.HideModelParts(partNames);
        }
        protected override void ShowParts(string[] partNames)
        {
            MeshGateway.ShowModelParts(partNames);
        }
        protected override void RemoveParts(string[] partNames)
        {
            MeshGateway.RemoveModelParts(partNames);
        }
        protected override void ScaleParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            MeshGateway.ScaleModelParts(partNames, scaleCenter, scaleFactors, copy);
        }
        //
        private static void ValidateNumberOfCopies(int numberOfCopies)
        {
            if (numberOfCopies < 0)
                throw new PythonAPIException("The argument 'number_of_copies' must not be negative.");
        }
    }
}
