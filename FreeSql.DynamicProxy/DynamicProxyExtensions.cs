using System;
using System.Collections.Generic;
using System.Linq;
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
            if (that == typeof(void)) return "void";
            var sb = new StringBuilder();
            var nestedType = that;
            while (nestedType.IsNested)
            {
                sb.Insert(0, ".").Insert(0, nestedType.DeclaringType.Name);
                nestedType = nestedType.DeclaringType;
            }
            if (string.IsNullOrEmpty(nestedType.Namespace) == false)
                sb.Insert(0, ".").Insert(0, nestedType.Namespace);
            if (that.IsGenericType == false)
                return sb.Append(that.Name).ToString();
            sb.Append(that.Name.Remove(that.Name.IndexOf('`'))).Append("<");
            var genericTypeIndex = 0;
            foreach (var genericType in that.GetGenericArguments())
            {
                if (genericTypeIndex++ > 0) sb.Append(", ");
                sb.Append(genericType.CSharpFullName());
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
