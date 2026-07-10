using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    public class BaseAPI
    {
        public string __str__()
        {
            return ToString();
        }
        public string __repr__()
        {
            return ToString();
        }
        public override string ToString()
        {
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance |
                                                        BindingFlags.Public |
                                                        BindingFlags.DeclaredOnly);
            //
            StringBuilder sb = new StringBuilder();
            sb.Append("Methods:");
            //
            foreach (MethodInfo method in methods.OrderBy(m => m.Name))
            {
                sb.AppendLine("");
                //
                if (method.IsSpecialName) continue;
                if (method.Name == nameof(ToString)) continue;
                //
                sb.Append("    ");
                sb.Append(method.Name);
                sb.Append("(");
                //
                ParameterInfo[] parameters = method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) sb.Append(", ");

                    sb.Append(parameters[i].ParameterType.Name);
                    sb.Append(" ");
                    sb.Append(parameters[i].Name);
                }
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}
