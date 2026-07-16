// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeResults;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.API.Python
{
    internal class ResultDataService
    {
        // Variables
        private readonly ResultResolver _resolver;


        // Constructors
        public ResultDataService(ResultResolver resolver)
        {
            _resolver = resolver;
        }


        // Methods
        public int[] GetStepIds(string resultName)
        {
            return _resolver.ResolveResult(resultName).GetAllStepIds();
        }
        public int[] GetIncrementIds(string resultName, int stepId)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateStep(resultName, result, stepId);
            return result.GetStepIncrementIds(stepId);
        }
        public float GetIncrementTime(string resultName, int stepId, int incrementId)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateIncrement(resultName, result, stepId, incrementId);
            return result.GetIncrementTime(stepId, incrementId);
        }
        public string[] GetFieldNames(string resultName, int stepId, int incrementId)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateIncrement(resultName, result, stepId, incrementId);
            List<string> names = new List<string>();
            foreach (FieldData fieldData in result.GetAllFieldData())
            {
                if (fieldData.StepId == stepId && fieldData.StepIncrementId == incrementId &&
                    FOFieldNames.IsVisible(fieldData.Name) && !names.Contains(fieldData.Name))
                {
                    names.Add(fieldData.Name);
                }
            }
            // PrePoMax exposes a synthetic zero increment for the first static step.
            if (stepId == 1 && incrementId == 0 && names.Count == 0)
            {
                foreach (string name in result.GetStepFieldNames(stepId))
                {
                    if (FOFieldNames.IsVisible(name) && !names.Contains(name)) names.Add(name);
                }
            }
            return names.ToArray();
        }
        public string[] GetComponentNames(string resultName, int stepId, int incrementId, string fieldName)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateField(resultName, result, stepId, incrementId, fieldName);
            if (stepId == 1 && incrementId == 0)
            {
                string[] zeroNames = result.GetFieldComponentNames(fieldName);
                return zeroNames != null ? zeroNames : new string[0];
            }
            FieldData fieldData = result.GetFieldData(fieldName, null, stepId, incrementId);
            Field field = result.GetField(fieldData);
            if (field == null)
                throw new PythonAPIException("The field '" + fieldName + "' does not exist for step " + stepId +
                                             ", increment " + incrementId + ".");
            return field.GetComponentNames();
        }
        public FieldDataAPI GetComponentData(string resultName, int stepId, int incrementId,
                                                   string fieldName, string componentName)
        {
            ComponentArrays arrays = ExtractComponentArrays(resultName, stepId, incrementId, fieldName, componentName);
            return new FieldDataAPI(arrays.Ids, arrays.Values, "node", arrays.Unit);
        }
        public float GetNodalValue(string resultName, int stepId, int incrementId,
                                   string fieldName, string componentName, int nodeId)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateComponent(resultName, result, stepId, incrementId, fieldName, componentName);
            if (result.Mesh == null)
                throw new PythonAPIException("The result '" + resultName + "' does not contain a mesh.");
            if (!result.Mesh.Nodes.ContainsKey(nodeId))
                throw new PythonAPIException("The node ID " + nodeId + " does not exist in result '" + resultName + "'.");
            FieldData fieldData = result.GetFieldData(fieldName, componentName, stepId, incrementId, true);
            if (!fieldData.Valid)
                throw new PythonAPIException("The field '" + fieldName + "' does not exist for step " + stepId +
                                             ", increment " + incrementId + ".");
            float[] values = result.GetValues(fieldData, new int[] { nodeId });
            if (values == null || values.Length == 0)
                throw new PythonAPIException("The component '" + componentName + "' could not be extracted from field '" +
                                             fieldName + "' for node ID " + nodeId + ".");
            return values[0];
        }
        public object GetDeformedNodalCoordinates(string resultName, int stepId, int incrementId, int nodeId)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateComponent(resultName, result, stepId, incrementId, FOFieldNames.Disp, FOComponentNames.U1);
            ValidateComponent(resultName, result, stepId, incrementId, FOFieldNames.Disp, FOComponentNames.U2);
            ValidateComponent(resultName, result, stepId, incrementId, FOFieldNames.Disp, FOComponentNames.U3);
            if (result.UndeformedNodes == null || !result.UndeformedNodes.ContainsKey(nodeId))
                throw new PythonAPIException("The node ID " + nodeId + " does not exist in result '" + resultName + "'.");
            double[][] coordinates = new double[][] { result.UndeformedNodes[nodeId].Coor };
            result.ScaleNodeCoordinates(FOFieldNames.Disp, 1, stepId, incrementId,
                                        new int[] { nodeId }, ref coordinates);
            return NumPyConverter.ToPythonArray(coordinates[0]);
        }
        public FieldMultiComponentDataAPI GetFieldData(string resultName, int stepId, int incrementId, string fieldName)
        {
            string[] componentNames = GetComponentNames(resultName, stepId, incrementId, fieldName);
            if (componentNames.Length == 0)
                throw new PythonAPIException("The field '" + fieldName + "' does not contain any components.");
            ComponentArrays first = ExtractComponentArrays(resultName, stepId, incrementId, fieldName, componentNames[0]);
            float[,] values = new float[first.Values.Length, componentNames.Length];
            for (int i = 0; i < first.Values.Length; i++) values[i, 0] = first.Values[i];
            for (int j = 1; j < componentNames.Length; j++)
            {
                ComponentArrays current = ExtractComponentArrays(resultName, stepId, incrementId, fieldName, componentNames[j]);
                if (current.Values.Length != first.Values.Length)
                    throw new PythonAPIException("The components of field '" + fieldName + "' do not share the same length.");
                for (int i = 0; i < current.Values.Length; i++) values[i, j] = current.Values[i];
            }
            return new FieldMultiComponentDataAPI(first.Ids, values, componentNames, "node", first.Unit);
        }
        public void ValidateStep(string resultName, FeResults result, int stepId)
        {
            if (!result.GetAllStepIds().Contains(stepId))
                throw new PythonAPIException("The step ID " + stepId + " does not exist in result '" + resultName + "'.");
        }
        public void ValidateIncrement(string resultName, FeResults result, int stepId, int incrementId)
        {
            ValidateStep(resultName, result, stepId);
            if (!result.GetStepIncrementIds(stepId).Contains(incrementId))
                throw new PythonAPIException("The increment ID " + incrementId + " does not exist in step " + stepId +
                                             " of result '" + resultName + "'.");
        }
        public void ValidateField(string resultName, FeResults result, int stepId, int incrementId, string fieldName)
        {
            ValidateIncrement(resultName, result, stepId, incrementId);
            if (!GetFieldNames(resultName, stepId, incrementId).Contains(fieldName))
                throw new PythonAPIException("The field '" + fieldName + "' does not exist for step " + stepId +
                                             ", increment " + incrementId + ".");
        }
        public void ValidateComponent(string resultName, FeResults result, int stepId, int incrementId,
                                      string fieldName, string componentName)
        {
            ValidateField(resultName, result, stepId, incrementId, fieldName);
            if (!GetComponentNames(resultName, stepId, incrementId, fieldName).Contains(componentName))
                throw new PythonAPIException("The component '" + componentName + "' does not exist in field '" + fieldName + "'.");
        }

        private ComponentArrays ExtractComponentArrays(string resultName, int stepId, int incrementId,
                                                       string fieldName, string componentName)
        {
            FeResults result = _resolver.ResolveResult(resultName);
            ValidateComponent(resultName, result, stepId, incrementId, fieldName, componentName);
            FieldData fieldData = result.GetFieldData(fieldName, componentName, stepId, incrementId, true);
            if (!fieldData.Valid)
                throw new PythonAPIException("The field '" + fieldName + "' does not exist for step " + stepId +
                                             ", increment " + incrementId + ".");
            int[] ids = result.Mesh.Nodes.Keys.ToArray();
            float[] values = result.GetValues(fieldData, ids);
            if (values == null)
                throw new PythonAPIException("The component '" + componentName + "' could not be extracted from field '" +
                                             fieldName + "'.");
            string unit = null;
            try
            {
                unit = result.GetFieldUnitAbbreviation(fieldName, componentName, stepId, incrementId);
            }
            catch { }
            return new ComponentArrays(ids, values, unit);
        }


        // Private classes
        private class ComponentArrays
        {
            public readonly int[] Ids;
            public readonly float[] Values;
            public readonly string Unit;

            public ComponentArrays(int[] ids, float[] values, string unit)
            {
                Ids = ids;
                Values = values;
                Unit = unit;
            }
        }
    }
}
