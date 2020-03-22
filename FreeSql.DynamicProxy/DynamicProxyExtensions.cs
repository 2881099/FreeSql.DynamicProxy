using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql
{
    static class DynamicProxyExtensions
    {

        /// <summary>
        /// 获取 Type 的原始 c# 文本表示
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static string DisplayCsharp(this Type type, bool isNameSpace = true)
        {
            if (type == null) return null;
            if (type == typeof(void)) return "void";
            if (type.IsGenericParameter) return type.Name;
            var sb = new StringBuilder();
            var nestedType = type;
            while (nestedType.IsNested)
            {
                sb.Insert(0, ".").Insert(0, DisplayCsharp(nestedType.DeclaringType, false));
                nestedType = nestedType.DeclaringType;
            }
            if (isNameSpace && string.IsNullOrEmpty(nestedType.Namespace) == false)
                sb.Insert(0, ".").Insert(0, nestedType.Namespace);

            if (type.IsGenericType == false)
                return sb.Append(type.Name).ToString();

            var genericParameters = type.GetGenericArguments();
            if (type.IsNested && type.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in type.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any() == false)
                return sb.Append(type.Name).ToString();

            sb.Append(type.Name.Remove(type.Name.IndexOf('`'))).Append("<");
            var genericTypeIndex = 0;
            foreach (var genericType in genericParameters)
            {
                if (genericTypeIndex++ > 0) sb.Append(", ");
                sb.Append(DisplayCsharp(genericType, true));
            }
            return sb.Append(">").ToString();
        }
        internal static string DisplayCsharp(this MethodInfo method, bool isOverride)
        {
            if (method == null) return null;
            var sb = new StringBuilder();
            if (method.IsPublic) sb.Append("public ");
            if (method.IsAssembly) sb.Append("internal ");
            if (method.IsFamily) sb.Append("protected ");
            if (method.IsPrivate) sb.Append("private ");
            if (method.IsPrivate) sb.Append("private ");
            if (method.IsStatic) sb.Append("static ");
            if (method.IsAbstract && method.DeclaringType.IsInterface == false) sb.Append("abstract ");
            if (method.IsVirtual && method.DeclaringType.IsInterface == false) sb.Append(isOverride ? "override " : "virtual ");
            sb.Append(method.ReturnType.DisplayCsharp()).Append(" ").Append(method.Name);

            var genericParameters = method.GetGenericArguments();
            if (method.DeclaringType.IsNested && method.DeclaringType.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in method.DeclaringType.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any())
                sb.Append("<")
                    .Append(string.Join(", ", genericParameters.Select(a => a.DisplayCsharp())))
                    .Append(">");

            sb.Append("(").Append(string.Join(", ", method.GetParameters().Select(a => $"{a.ParameterType.DisplayCsharp()} {a.Name}"))).Append(")");
            return sb.ToString();
        }

        internal static bool IsTask(this Type that)
        {
            if (that == typeof(void)) return false;
            if (that == typeof(Task)) return true;
#if ns21
            if (that == typeof(ValueTask)) return true;
#endif

#if ns20 || ns21
            if (that.IsGenericType && that.Namespace == "System.Threading.Tasks" && (that.Name == typeof(Task<object>).Name || that.Name == typeof(ValueTask<object>).Name)) return true;
#else
            if (that.IsGenericType && that.Namespace == "System.Threading.Tasks" && that.Name == typeof(Task<object>).Name) return true;
#endif
            return false;
        }

        internal static Type ReturnTypeWithoutTask(this Type that)
        {
            if (that.IsTask() == false) return that;
            return that.GetGenericArguments().FirstOrDefault();
        }

    }

}
