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
        public void Merge(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.MergeModelParts(partNames);
        }
        public void FindEdgesByAngle(string[] partNames, object edgeAngle)
        {
            double angle = ToEdgeAngle(edgeAngle);
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
        protected override void TranslateParts(string[] partNames, double[] translateVector, int numberOfCopies)
        {
            MeshGateway.TranslateModelParts(partNames, translateVector, numberOfCopies);
        }
        protected override void RotateParts(string[] partNames, double[] rotateCenter, double[] rotateAxis,
                                            double rotateAngle, int numberOfCopies)
        {
            MeshGateway.RotateModelParts(partNames, rotateCenter, rotateAxis, rotateAngle, numberOfCopies);
        }
        protected override void MirrorParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection,
                                            bool copy)
        {
            MeshGateway.MirrorModelParts(partNames, mirrorPoint, mirrorDirection, copy);
        }
        protected override void ScaleParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            MeshGateway.ScaleModelParts(partNames, scaleCenter, scaleFactors, copy);
        }
    }
}
