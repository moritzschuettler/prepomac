// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    // What the application shows, as opposed to what the model holds. None of it is a model change,
    // so none of it is recorded in the command history: a camera angle is not something Regenerate
    // has to replay, and a script that moved the view has changed nothing the .pmx keeps.
    //
    // Everything here is named with a string rather than with an enumeration, because the view
    // enumerations belong to the application and the API may not reference it.
    public interface IPythonViewGateway
    {
        string GetCurrentView();
        void SetCurrentView(string view);
        //
        void SetStandardView(string orientation);
        void ZoomToFit();
        double[] GetViewParameters();
        void SetViewParameters(double[] parameters);
        void Rotate(double azimuthAngle, double elevationAngle, double rollAngle);
        //
        string GetResultType();
        void SetResultType(string resultType);
        string[] GetResultFieldData();
        void SetResultFieldData(string fieldName, string componentName);
        void SetResultIncrement(int stepId, int incrementId);
        //
        string GetDeformationType();
        void SetDeformationType(string deformationType);
        double GetDeformationFactor();
        void SetDeformationFactor(double deformationFactor);
        string GetUndeformedModelType();
        void SetUndeformedModelType(string undeformedModelType);
        void SetLegendRange(double minValue, double maxValue);
        void ResetLegendRange();
        //
        string[] GetAnnotationNames();
        void AddTextAnnotation(string name, string text, double[] anchorPoint);
        string AddNodeAnnotation(int nodeId);
        string AddElementAnnotation(int elementId);
        void RemoveAnnotations(string[] names);
        bool[] GetMinMaxAnnotations();
        void ShowMinMaxAnnotations(bool showMin, bool showMax);
        //
        string SaveImage(string fileName, bool transparentBackground, int magnification);
    }
}
