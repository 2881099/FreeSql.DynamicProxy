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

        internal static object CreateInstanceGetDefaultValue(this Type that)
        {
            if (that == null) return null;
            if (that == typeof(string)) return default(string);
            if (that.IsArray) return Array.CreateInstance(that, 0);
            var ctorParms = that.InternalGetTypeConstructor0OrFirst(false)?.GetParameters();
            if (ctorParms == null || ctorParms.Any() == false) return Activator.CreateInstance(that, null);
            return Activator.CreateInstance(that, ctorParms.Select(a => Activator.CreateInstance(a.ParameterType, null)).ToArray());
        }
        internal static NewExpression InternalNewExpression(this Type that)
        {
            var ctor = that.InternalGetTypeConstructor0OrFirst();
            return Expression.New(ctor, ctor.GetParameters().Select(a => Expression.Constant(a.ParameterType.CreateInstanceGetDefaultValue(), a.ParameterType)));
        }

        static ConcurrentDictionary<Type, ConstructorInfo> _dicInternalGetTypeConstructor0OrFirst = new ConcurrentDictionary<Type, ConstructorInfo>();
        internal static ConstructorInfo InternalGetTypeConstructor0OrFirst(this Type that, bool isThrow = true)
        {
            var ret = _dicInternalGetTypeConstructor0OrFirst.GetOrAdd(that, tp =>
                tp.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null) ??
                tp.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault());
            if (ret == null && isThrow) throw new ArgumentException($"{that.FullName} 类型无方法访问构造函数");
            return ret;
        }

    }

}
