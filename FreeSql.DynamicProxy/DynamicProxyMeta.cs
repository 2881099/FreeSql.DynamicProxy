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
        private Type[] _matchedAttributesTypes;

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
            _matchedAttributesTypes = matchedAttributes?.Select(a => a?.GetType()).ToArray();

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
                throw new ArgumentException($"{this.SourceType.DisplayCsharp()} 没有定义长度 {parameters.Length} 的构造函数");

            return Activator.CreateInstance(this.SourceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters);
        }

        public object CreateProxyInstance(object source)
        {
            if (this.ProxyType == null) return source;
            var proxy = CreateInstanceDefault(this.ProxyType);
            CopyData(this.SourceType, source, proxy);
            return proxy;
        }

        public DynamicProxyAttribute CreateDynamicProxyAttribute(int index)
        {
            if (index < 0 || index > this.MatchedAttributes.Length) 
                throw new ArgumentException($"{nameof(index)} 参数错误，值范围 0 至 {this.MatchedAttributes.Length}");
            var attribute = CreateInstanceDefault(_matchedAttributesTypes[index]) as DynamicProxyAttribute;
            CopyData(_matchedAttributesTypes[index], this.MatchedAttributes[index], attribute);
            return attribute;
        }
        public void SetDynamicProxyAttributePropertyValue(int index, object source, string propertyOrField, object value)
        {
            if (source == null) return;
            if (index < 0 || index > this.MatchedAttributes.Length)
                throw new ArgumentException($"{nameof(index)} 参数错误，值范围 0 至 {this.MatchedAttributes.Length}");
            SetPropertyValue(_matchedAttributesTypes[index], source, propertyOrField, value);
        }


        public static object CreateInstanceDefault(Type type)
        {
            if (type == null) return null;
            if (type == typeof(string)) return default(string);
            if (type.IsArray) return Array.CreateInstance(type, 0);
            var ctorParms = InternalGetTypeConstructor0OrFirst(type, true)?.GetParameters();
            if (ctorParms == null || ctorParms.Any() == false) return Activator.CreateInstance(type, null);
            return Activator.CreateInstance(type, ctorParms.Select(a => a.ParameterType.IsInterface || a.ParameterType.IsAbstract || a.ParameterType == typeof(string) ? null : Activator.CreateInstance(a.ParameterType, null)).ToArray());
        }
        internal static NewExpression InternalNewExpression(Type that)
        {
            var ctor = InternalGetTypeConstructor0OrFirst(that);
            return Expression.New(ctor, ctor.GetParameters().Select(a => Expression.Constant(CreateInstanceDefault(a.ParameterType), a.ParameterType)));
        }

        static ConcurrentDictionary<Type, ConstructorInfo> _dicInternalGetTypeConstructor0OrFirst = new ConcurrentDictionary<Type, ConstructorInfo>();
        internal static ConstructorInfo InternalGetTypeConstructor0OrFirst(Type that, bool isThrow = true)
        {
            var ret = _dicInternalGetTypeConstructor0OrFirst.GetOrAdd(that, tp =>
                tp.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null) ??
                tp.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault());
            if (ret == null && isThrow) throw new ArgumentException($"{that.FullName} 类型无方法访问构造函数");
            return ret;
        }

        readonly static ConcurrentDictionary<Type, Action<object, object>> _copyDataFunc = new ConcurrentDictionary<Type, Action<object, object>>();
        readonly static Action<object, object> _copyDataFuncEmpty = new Action<object, object>((item1, item2) => { });
        public static void CopyData(Type sourceType, object source, object target)
        {
            if (source == null) return;
            if (target == null) return;
            _copyDataFunc.GetOrAdd(sourceType, type =>
            {
                var sourceParamExp = Expression.Parameter(typeof(object), "sourceObject");
                var targetParamExp = Expression.Parameter(typeof(object), "targetObject");
                var sourceExp = Expression.Variable(type, "source");
                var targetExp = Expression.Variable(type, "target");
                var copyExps = new List<Expression>();
                var sourceFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in sourceFields)
                    copyExps.Add(Expression.Assign(Expression.MakeMemberAccess(targetExp, field), Expression.MakeMemberAccess(sourceExp, field)));

                if (copyExps.Any() == false) return _copyDataFuncEmpty;
                var bodyExp = Expression.Block(
                    new[] {
                            sourceExp, targetExp
                    },
                    new[] {
                            Expression.IfThen(
                                Expression.NotEqual(sourceParamExp, Expression.Constant(null)),
                                Expression.IfThen(
                                    Expression.NotEqual(targetParamExp, Expression.Constant(null)),
                                    Expression.Block(
                                        Expression.Assign(sourceExp, Expression.TypeAs(sourceParamExp, sourceType)),
                                        Expression.Assign(targetExp, Expression.TypeAs(targetParamExp, sourceType)),
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
                return Expression.Lambda<Action<object, object>>(bodyExp, sourceParamExp, targetParamExp).Compile();
            })(source, target);
        }

        static ConcurrentDictionary<Type, ConcurrentDictionary<string, Action<object, string, object>>> _dicSetEntityValueWithPropertyName = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Action<object, string, object>>>();
        public static void SetPropertyValue(Type sourceType, object source, string propertyOrField, object value)
        {
            if (source == null) return;
            if (sourceType == null) sourceType = source.GetType();
            _dicSetEntityValueWithPropertyName
                .GetOrAdd(sourceType, et => new ConcurrentDictionary<string, Action<object, string, object>>())
                .GetOrAdd(propertyOrField, pf =>
                {
                    var t = sourceType;
                    var parm1 = Expression.Parameter(typeof(object));
                    var parm2 = Expression.Parameter(typeof(string));
                    var parm3 = Expression.Parameter(typeof(object));
                    var var1Parm = Expression.Variable(t);
                    var exps = new List<Expression>(new Expression[] {
                        Expression.Assign(var1Parm, Expression.TypeAs(parm1, t))
                    });
                    var memberInfos = t.GetMember(pf, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(a => a.MemberType == MemberTypes.Field || a.MemberType == MemberTypes.Property);
                    foreach (var memberInfo in memberInfos) {
                        exps.Add(
                            Expression.Assign(
                                Expression.MakeMemberAccess(var1Parm, memberInfo),
                                Expression.Convert(
                                    parm3,
                                    memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo)?.FieldType : (memberInfo as PropertyInfo)?.PropertyType
                                )
                            )
                        );
                    }
                    return Expression.Lambda<Action<object, string, object>>(Expression.Block(new[] { var1Parm }, exps), new[] { parm1, parm2, parm3 }).Compile();
                })(source, propertyOrField, value);
        }
    }
}