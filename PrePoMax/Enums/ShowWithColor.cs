using System;

namespace PrePoMax
{
    [Serializable]
    [Flags]
    public enum AnnotateWithColorEnum
    {
        None = 0,
        FaceOrientation = 1,
        Parts = 2,
        Materials = 4,
        Sections = 8,
        SectionThicknesses = 16,
        ReferencePoints = 32,
        Constraints = 64,
        ContactPairs = 128,
        InitialConditions = 256,
        BoundaryConditions = 512,
        Loads = 1024,
        DefinedFields = 2048
    }
}
