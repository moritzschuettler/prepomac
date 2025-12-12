using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using System.IO;
using FileInOut.Output.Calculix;
using CaeGlobals;
using Microsoft.SqlServer.Server;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Diagnostics.Eventing.Reader;
using System.Security;

namespace FileInOut.Output
{
    [Serializable]
    public static class AbaqusFileWriter
    {
        // Methods                                                                                                                  
        static public void Write(string fileName, FeModel model, ConvertPyramidsToEnum convertPyramidsTo,
                                 Dictionary<int, double[]> deformations = null)
        {
            // Copy model
            FeModel copy = model.DeepCopy();
            string[] inactivePartNames = copy.Mesh.GetInactivePartNames();
            copy.Mesh.RemoveParts(inactivePartNames, out _, false);
            //
            CalculixKeyword keywordToChange;
            List<CalculixKeyword> keywords = CalculixFileWriter.GetAllKeywords(copy, convertPyramidsTo, deformations);
            for (int i = 0; i < keywords.Count; i++)
            {
                keywordToChange = keywords[i];
                ConvertKeywordsToAbaqus(copy, ref keywordToChange);
                keywords[i] = keywordToChange;
            }
            // Write file
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in keywords)
            {
                WriteKeywordRecursively(sb, keyword);
            }
            // Write to file in multiple steps
            int step = 10_000_000;
            int upper;
            using (StreamWriter sw = new StreamWriter(fileName, false))
            {
                for (int i = 0; i < sb.Length; i+=step)
                {
                    upper = Math.Min(sb.Length - i, step);
                    sw.Write(sb.ToString(i, upper));
                }
            }
        }
        static private void WriteKeywordRecursively(StringBuilder sb, CalculixKeyword keyword)
        {
            sb.Append(keyword.GetKeywordString());
            sb.Append(keyword.GetDataString());
            //
            foreach (var childKeyword in keyword.Keywords)
            {
                WriteKeywordRecursively(sb, childKeyword);
            }
        }
        static private void ConvertKeywordsToAbaqus(FeModel model, ref CalculixKeyword keyword)
        {
            CalculixKeyword additionalKeyword;
            CalculixKeyword childKeyword;
            //
            HashSet<string> rpRotNames = new HashSet<string>();
            foreach (var entry in model.Mesh.ReferencePoints) rpRotNames.Add(entry.Value.RotNodeSetName);
            // Element
            if (keyword is CalElement ce) keyword = new AbqElement(ce);
            // Surface
            else if (keyword is CalSurface cs) keyword = new AbqSurface(cs);
            // Rigid body
            else if (keyword is CalRigidBody crb) keyword = new AbqRigidBody(crb);
            // Step
            else if (keyword is CalStep cst)
            {
                cst.OutputSolver = false;
                cst.OutputNoAnalysis = false;
                if (keyword is CalBuckleStep cbs) cbs.OutputAccuracy = false;   // turn off accuracy output
            }
            // Output
            else if (keyword is CalOutputFrequency) keyword = new CalTitle("", "");
            // History output
            else if (keyword is CalNodePrint cnp)
            {
                if (rpRotNames.Contains(cnp.RegionName)) keyword = new CalTitle("", "");
                else cnp.OutputFrequency = true;
            }
            else if (keyword is CalElPrint cep)
            {
                cep.OutputFrequency = true;
                cep.OutputGlobal = false;
            }
            else if (keyword is CalContactPrint ccp) keyword = new AbqContactPrint(ccp);
            // Field output
            else if (keyword is CalNodeFile cnf)
            {
                keyword = new AbqFieldNodeOutput(cnf);
            }
            else if (keyword is CalElFile cef)
            {
                keyword = new AbqFieldElementOutput(cef);
            }
            else if (keyword is CalContactFile ccf) keyword = new AbqFieldContactOutput(ccf);
            // Boundary condition
            else if (keyword is CalBC cbc)
            {
                if (cbc is CalDisplacementRotation cdr) cdr.MaxNumberNodeDOFs = 6;
                else if (cbc is CalFixedBC cfb) cfb.MaxNumberNodeDOFs = 6;
                cbc.OpType = OpTypeEnum.New;
            }
            // Load
            else if (keyword is CalLoad cl)
            {
                if (cl is CalMomentLoad cml) cml.MaxNumberNodeDOFs = 6;
                else if (cl is CalDLoad cdl) keyword = new AbqDLoad(cdl);
                else if (cl is CalVariablePressureLoad cvpl) keyword = new AbqVariablePressureLoad(cvpl);
                else if (cl is CalImportedPressureLoad cipl) keyword = new AbqImportedPressureLoad(cipl);
                else if (cl is CalShellEdgeLoad csel) keyword = new AbqShellEdgeLoad(csel);
                //
                cl.OpType = OpTypeEnum.New;
            }
            else if (keyword is CalDefinedTemperature cdt)
            {
                cdt.OpType = OpTypeEnum.New;
            }
            // Additional
            else if (keyword is CalAdditional ca)
            {
                for (int i = 0; i < ca.AdditionalKeywords.Count; i++)
                {
                    additionalKeyword = ca.AdditionalKeywords[i];
                    ConvertKeywordsToAbaqus(model, ref additionalKeyword);
                    ca.AdditionalKeywords[i] = additionalKeyword;
                }
            }
            // Child keywords
            for (int i = 0; i < keyword.Keywords.Count; i++)
            {
                childKeyword = keyword.Keywords[i];
                ConvertKeywordsToAbaqus(model, ref childKeyword);
                keyword.Keywords[i] = childKeyword;
            }
        }

    }
}
