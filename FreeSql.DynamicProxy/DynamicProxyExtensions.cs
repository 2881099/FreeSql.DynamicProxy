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
        /// <param name="that"></param>
        /// <returns></returns>
        internal static string CSharpFullName(this Type that)
        {
            return CSharpName(that, true);
        }
        static string CSharpName(this Type that, bool isNameSpace)
        {
            if (that == typeof(void)) return "void";
            if (that.IsGenericParameter) return that.Name;
            var sb = new StringBuilder();
            var nestedType = that;
            while (nestedType.IsNested)
            {
                sb.Insert(0, ".").Insert(0, CSharpName(nestedType.DeclaringType, false));
                nestedType = nestedType.DeclaringType;
            }
            if (isNameSpace && string.IsNullOrEmpty(nestedType.Namespace) == false)
                sb.Insert(0, ".").Insert(0, nestedType.Namespace);

            if (that.IsGenericType == false)
                return sb.Append(that.Name).ToString();

            var genericParameters = that.GetGenericArguments();
            if (that.IsNested && that.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in that.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any() == false)
                return sb.Append(that.Name).ToString();

            sb.Append(that.Name.Remove(that.Name.IndexOf('`'))).Append("<");
            var genericTypeIndex = 0;
            foreach (var genericType in genericParameters)
            {
                if (genericTypeIndex++ > 0) sb.Append(", ");
                sb.Append(CSharpName(genericType, true));
            }
            return sb.Append(">").ToString();
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
