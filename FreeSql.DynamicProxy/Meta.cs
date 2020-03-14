using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FreeSql
{
    partial class DynamicProxy
    {
        public class Meta
        {
            public Type SourceType { get; }
            public ConstructorInfo[] SourceConstructors { get; }
            public Dictionary<int, ConstructorInfo[]> SourceConstructorsMergeParametersLength { get; }

            public MemberInfo[] MatchedMemberInfos { get; }
            public DynamicProxyAttribute[] MatchedAttributes { get; }

            public string ProxyCSharpCode { get; }
            public string ProxyClassName { get; }
            public Assembly ProxyAssembly { get; }
            public Type ProxyType { get; }

            internal Meta(
                Type sourceType, ConstructorInfo[] constructors,
                MemberInfo[] matchedMemberInfos, DynamicProxyAttribute[] matchedAttributes, 
                string proxyCSharpCode, string proxyClassName, Assembly proxyAssembly, Type proxyType)
            {

                this.SourceType = sourceType;
                this.SourceConstructors = constructors;
                this.SourceConstructorsMergeParametersLength = constructors?.GroupBy(a => a.GetParameters().Length)
                    .Select(a => new KeyValuePair<int, ConstructorInfo[]>(a.Key, constructors.Where(b => b.GetParameters().Length == a.Key).ToArray()))
                    .ToDictionary(a => a.Key, a => a.Value);

                this.MatchedMemberInfos = matchedMemberInfos;
                this.MatchedAttributes = matchedAttributes;

                this.ProxyCSharpCode = proxyCSharpCode;
                this.ProxyClassName = proxyClassName;
                this.ProxyAssembly = proxyAssembly;
                this.ProxyType = proxyType;

            }

            public object CreateSourceInstance(object[] parameters)
            {
                if (parameters == null || parameters.Length == 0)
                    return Activator.CreateInstance(this.SourceType, true);

                if (this.SourceConstructorsMergeParametersLength.TryGetValue(parameters.Length, out var ctors) == false)
                    throw new ArgumentException($"{this.SourceType.CSharpFullName()} 没有定义长度 {parameters.Length} 的构造函数");

                return Activator.CreateInstance(this.SourceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters);
            }

        }
    }
}