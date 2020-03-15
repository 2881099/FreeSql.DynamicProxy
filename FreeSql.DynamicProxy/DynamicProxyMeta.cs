using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;

namespace FreeSql
{
    public class DynamicProxyMeta
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

        internal DynamicProxyMeta(
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

        readonly static ConcurrentDictionary<Type, Action<object, object>> _copyDataFunc = new ConcurrentDictionary<Type, Action<object, object>>();
        readonly static Action<object, object> _copyDataFuncEmpty = new Action<object, object>((item1, item2) => { });
        public object CreateProxyInstance(object source)
        {
            if (this.ProxyType == null) return source;
            var proxy = Activator.CreateInstance(this.ProxyType, new object[] { this });

            // copy data
            _copyDataFunc.GetOrAdd(this.ProxyType, _ =>
            {
                var sourceParamExp = Expression.Parameter(typeof(object), "sourceObject");
                var proxyParamExp = Expression.Parameter(typeof(object), "proxyObject");
                var sourceExp = Expression.Variable(this.SourceType, "source");
                var proxyExp = Expression.Variable(this.ProxyType, "proxy");
                var copyExps = new List<Expression>();
                //Expression.IfThen(Expression.Equal(sourceExp, Expression.Constant(null)),
                var sourceFields = this.SourceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                //var proxyFields = this.ProxyType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var sourceField in sourceFields)
                {
                    //var proxyField = proxyFields.Where(a => a.FieldType == sourceField.FieldType && a.Name == sourceField.Name).FirstOrDefault();
                    //if (proxyField == null) continue;
                    var proxyField = sourceField;

                    copyExps.Add(Expression.Assign(Expression.MakeMemberAccess(proxyExp, proxyField), Expression.MakeMemberAccess(sourceExp, sourceField)));
                }
                if (copyExps.Any() == false) return _copyDataFuncEmpty;
                var bodyExp = Expression.Block(
                    new[] {
                            sourceExp, proxyExp
                    },
                    new[] {
                            Expression.IfThen(
                                Expression.NotEqual(sourceParamExp, Expression.Constant(null)),
                                Expression.IfThen(
                                    Expression.NotEqual(proxyParamExp, Expression.Constant(null)),
                                    Expression.Block(
                                        Expression.Assign(sourceExp, Expression.TypeAs(sourceParamExp, this.SourceType)),
                                        Expression.Assign(proxyExp, Expression.TypeAs(proxyParamExp, this.ProxyType)),
                                        Expression.IfThen(
                                            Expression.NotEqual(sourceExp, Expression.Constant(null)),
                                            Expression.IfThen(
                                                Expression.NotEqual(sourceExp, Expression.Constant(null)),
                                                Expression.Block(
                                                    copyExps.ToArray()
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                    }
                );
                return Expression.Lambda<Action<object, object>>(bodyExp, sourceParamExp, proxyParamExp).Compile();
            })(source, proxy);

            return proxy;
        }
    }
}