using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;

namespace FreeSql
{
    /// <summary>
    /// DynamicProxy 快速使用类
    /// </summary>
    public static partial class DynamicProxy
    {

        static ConcurrentDictionary<Type, DynamicProxyMeta> _metaCache = new ConcurrentDictionary<Type, DynamicProxyMeta>();

        /// <summary>
        /// 创建 对象 source 的 FreeSql.DynamicProxy 代理对象<para></para>
        /// 有可能返回 source 本身（当不需要动态代理的时候）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T ToDynamicProxy<T>(this T source) where T : class
        {
            if (source == null) return null;
            var meta = _metaCache.GetOrAdd(source.GetType(), tp => CreateDynamicProxyMeta(tp, true, true));
            return meta?.CreateProxyInstance(source) as T;
        }

        /// <summary>
        /// 获取 动态接口实现 对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>() where T : class {
            var meta = _metaCache.GetOrAdd(typeof(T), tp => CreateDynamicProxyMeta(tp, true, true));
            if (meta == null || meta.ProxyType == null) return null;
            return DynamicProxyMeta.CreateInstanceDefault(meta.ProxyType) as T;
        }

        static readonly string _metaName = typeof(DynamicProxyMeta).DisplayCsharp();
        static readonly string _beforeAgumentsName = typeof(DynamicProxyBeforeArguments).DisplayCsharp();
        static readonly string _afterAgumentsName = typeof(DynamicProxyAfterArguments).DisplayCsharp();
        static readonly string _injectorTypeName = typeof(DynamicProxyInjectorType).DisplayCsharp();
        static readonly string _idynamicProxyName = typeof(DynamicProxyAttribute).DisplayCsharp();
        static readonly string _dynamicProxyExceptionName = typeof(DynamicProxyException).DisplayCsharp();

        public static DynamicProxyMeta GetAvailableMeta(Type type)
        {
            if (_metaCache.TryGetValue(type, out var meta) == false)
                meta = _metaCache.GetOrAdd(type, tp => CreateDynamicProxyMeta(tp, true, false));
            return meta?.ProxyType != null ? meta : null;
        }
        public static DynamicProxyMeta CreateDynamicProxyMeta(Type type, bool isCompile, bool isThrow)
        {
            if (type == null) return null;
            var typeCSharpName = type.DisplayCsharp();

            if (type.IsNotPublic)
            {
                if (isThrow) throw new ArgumentException($"FreeSql.DynamicProxy 失败提示：{typeCSharpName} 需要使用 public 标记");
                return null;
            }

            var matchedMemberInfos = new List<MemberInfo>();
            var matchedAttributes = new List<DynamicProxyAttribute>();
            var matchedAttributesFromServices = new List<FieldInfo[]>();
            var className = $"AopProxyClass___{Guid.NewGuid().ToString("N")}";
            var methodOverrideSb = new StringBuilder();
            var sb = methodOverrideSb;

            #region Common Code

            Func<Type, DynamicProxyInjectorType, bool, int, string, string> getMatchedAttributesCode = (returnType, injectorType, isAsync, attrsIndex, proxyMethodName) =>
            {
                var sbt = new StringBuilder();
                for (var a = attrsIndex; a < matchedAttributes.Count; a++)
                {
                    sbt.Append($@"{(proxyMethodName == "Before" ? $@"
        var __DP_ARG___attribute{a} = __DP_Meta.{nameof(DynamicProxyMeta.CreateDynamicProxyAttribute)}({a});
        __DP_ARG___attribute{a}_FromServicesCopyTo(__DP_ARG___attribute{a});" : "")}
        var __DP_ARG___{proxyMethodName}{a} = new {(proxyMethodName == "Before" ? _beforeAgumentsName : _afterAgumentsName)}(this, {_injectorTypeName}.{injectorType.ToString()}, __DP_Meta.MatchedMemberInfos[{a}], __DP_ARG___parameters, {(proxyMethodName == "Before" ? "null" : "__DP_ARG___return_value, __DP_ARG___exception")});
        {(isAsync ? "await " : "")}__DP_ARG___attribute{a}.{proxyMethodName}(__DP_ARG___{proxyMethodName}{a});
        {(proxyMethodName == "Before" ? 
        $@"if (__DP_ARG___is_return == false)
        {{
            __DP_ARG___is_return = __DP_ARG___{proxyMethodName}{a}.Returned;{(returnType != typeof(void) ? $@"
            if (__DP_ARG___is_return) __DP_ARG___return_value = __DP_ARG___{proxyMethodName}{a}.ReturnValue;" : "")}
        }}" : 
        $"if (__DP_ARG___{proxyMethodName}{a}.Exception != null && __DP_ARG___{proxyMethodName}{a}.ExceptionHandled == false) throw __DP_ARG___{proxyMethodName}{a}.Exception;")}");
                }
                return sbt.ToString();
            };
            Func<Type, DynamicProxyInjectorType, bool, string, string> getMatchedAttributesCodeReturn = (returnType, injectorType, isAsync, basePropertyValueTpl) =>
            {
                var sbt = new StringBuilder();
                var taskType = returnType.ReturnTypeWithoutTask();
                sbt.Append($@"
        {(returnType == typeof(void) ? "return;" : (isAsync == false && returnType.IsTask() ?
                (taskType.IsValueType || taskType.IsGenericParameter ?
                    $"return __DP_ARG___return_value == null ? null : (__DP_ARG___return_value.GetType() == typeof({taskType.DisplayCsharp()}) ? System.Threading.Tasks.Task.FromResult(({taskType.DisplayCsharp()})__DP_ARG___return_value) : ({returnType.DisplayCsharp()})__DP_ARG___return_value);" :
                    $"return __DP_ARG___return_value == null ? null : (__DP_ARG___return_value.GetType() == typeof({taskType.DisplayCsharp()}) ? System.Threading.Tasks.Task.FromResult(__DP_ARG___return_value as {taskType.DisplayCsharp()}) : ({returnType.DisplayCsharp()})__DP_ARG___return_value);"
                ) :
                (returnType.IsValueType || returnType.IsGenericParameter ? $"return ({returnType.DisplayCsharp()})__DP_ARG___return_value;" : $"return __DP_ARG___return_value as {returnType.DisplayCsharp()};")))}");
                return sbt.ToString();
            };
            Func<string, Type, string> getMatchedAttributesCodeAuditParameter = (methodParameterName, methodParameterType) =>
            {
                return $@"
            if (!object.ReferenceEquals({methodParameterName}, __DP_ARG___parameters[""{methodParameterName}""])) {methodParameterName} = {(methodParameterType.IsValueType ? $@"({methodParameterType.DisplayCsharp()})__DP_ARG___parameters[""{methodParameterName}""]" : $@"__DP_ARG___parameters[""{methodParameterName}""] as {methodParameterType.DisplayCsharp()}")};";
            };
            #endregion

            #region Methods
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(a => a.IsStatic == false).ToArray();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                    if (type.GetProperty(method.Name.Substring(4), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) != null) continue;
                var attrs = method.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).ToArray();
                if (attrs.Any() == false) continue;
                var attrsIndex = matchedAttributes.Count;
                matchedMemberInfos.AddRange(attrs.Select(a => method));
                matchedAttributes.AddRange(attrs);
#if net50 || ns21 || ns20
                matchedAttributesFromServices.AddRange(attrs.Select(af => af.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(gf => gf.GetCustomAttribute(typeof(DynamicProxyFromServicesAttribute)) != null).ToArray()));
#else
                matchedAttributesFromServices.AddRange(attrs.Select(af => new FieldInfo[0]));
#endif
                if (method.IsVirtual == false || method.IsFinal)
                {
                    if (isThrow) throw new ArgumentException($"FreeSql.DynamicProxy 失败提示：{typeCSharpName} 方法 {method.Name} 需要使用 virtual 标记");
                    continue;
                }

#if net40
                var returnType = method.ReturnType;
                var methodIsAsync = false;
#else
                var returnType = method.ReturnType.ReturnTypeWithoutTask();
                var methodIsAsync = method.ReturnType.IsTask();

                //if (attrs.Where(a => a.GetType().GetMethod("BeforeAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) != null).Any() ||
                //    attrs.Where(a => a.GetType().GetMethod("AfterAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) != null).Any())
                //{

                //}
#endif

                var baseInvoke = type.IsInterface == false ? $@"

        try
        {{
            if (__DP_ARG___is_return == false)
            {{{string.Join("", method.GetParameters().Select(a => getMatchedAttributesCodeAuditParameter(a.Name, a.ParameterType)))}
                {(returnType != typeof(void) ? "__DP_ARG___return_value = " : "")}{(methodIsAsync ? "await " : "")}base.{method.Name}({(string.Join(", ", method.GetParameters().Select(a => a.Name)))});
            }}
        }}
        catch (Exception __DP_ARG___ex)
        {{
            __DP_ARG___exception = __DP_ARG___ex;
        }}" : "";

                sb.Append($@"

    {(methodIsAsync ? "async " : "")}{method.DisplayCsharp(true)}
    {{
        Exception __DP_ARG___exception = null;
        var __DP_ARG___is_return = false;
        object __DP_ARG___return_value = null;
        var __DP_ARG___parameters = new Dictionary<string, object>();{string.Join("\r\n        ", method.GetParameters().Select(a => $"__DP_ARG___parameters.Add(\"{a.Name}\", {a.Name});"))}
        {getMatchedAttributesCode(returnType, DynamicProxyInjectorType.Method, methodIsAsync, attrsIndex, "Before")}{baseInvoke}
        {getMatchedAttributesCode(returnType, DynamicProxyInjectorType.Method, methodIsAsync, attrsIndex, "After")}
        {getMatchedAttributesCodeReturn(returnType, DynamicProxyInjectorType.Method, methodIsAsync, null)}
    }}");
            }
            #endregion

            var propertyOverrideSb = new StringBuilder();
            sb = propertyOverrideSb;
            #region Property
            var props = type.IsInterface == false ? type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) : new PropertyInfo[0];
            foreach (var prop2 in props)
            {
                var getMethod = prop2.GetGetMethod(false);
                var setMethod = prop2.GetSetMethod(false);
                if (getMethod?.IsFinal == true || setMethod?.IsFinal == true || (getMethod?.IsVirtual == false && setMethod?.IsVirtual == false))
                {
                    if (getMethod?.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).Any() == true ||
                        setMethod?.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).Any() == true)
                    {
                        if (isThrow) throw new ArgumentException($"FreeSql.DynamicProxy 失败提示：{typeCSharpName} 属性 {prop2.Name} 需要使用 virtual 标记");
                        continue;
                    }
                }

                var attrs = prop2.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).ToArray();
                var prop2AttributeAny = attrs.Any();
                var getMethodAttributeAny = prop2AttributeAny;
                var setMethodAttributeAny = prop2AttributeAny;
                if (attrs.Any() == false && getMethod?.IsVirtual == true)
                {
                    attrs = getMethod.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).ToArray();
                    getMethodAttributeAny = attrs.Any();
                }
                if (attrs.Any() == false && setMethod?.IsVirtual == true)
                {
                    attrs = setMethod.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).ToArray();
                    setMethodAttributeAny = attrs.Any();
                }
                if (attrs.Any() == false) continue;

                var attrsIndex = matchedAttributes.Count;
                matchedMemberInfos.AddRange(attrs.Select(a => prop2));
                matchedAttributes.AddRange(attrs);
#if net50 || ns21 || ns20
                matchedAttributesFromServices.AddRange(attrs.Select(af => af.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(gf => gf.GetCustomAttribute(typeof(DynamicProxyFromServicesAttribute)) != null).ToArray()));
#else
                matchedAttributesFromServices.AddRange(attrs.Select(af => new FieldInfo[0]));
#endif

                var returnTypeCSharpName = prop2.PropertyType.DisplayCsharp();

                var propModification = (getMethod?.IsPublic == true || setMethod?.IsPublic == true ? "public " : (getMethod?.IsAssembly == true || setMethod?.IsAssembly == true ? "internal " : (getMethod?.IsFamily == true || setMethod?.IsFamily == true ? "protected " : (getMethod?.IsPrivate == true || setMethod?.IsPrivate == true ? "private " : ""))));
                var propSetModification = (setMethod?.IsPublic == true ? "public " : (setMethod?.IsAssembly == true ? "internal " : (setMethod?.IsFamily == true ? "protected " : (setMethod?.IsPrivate == true ? "private " : ""))));
                var propGetModification = (getMethod?.IsPublic == true ? "public " : (getMethod?.IsAssembly == true ? "internal " : (getMethod?.IsFamily == true ? "protected " : (getMethod?.IsPrivate == true ? "private " : ""))));
                if (propSetModification == propModification) propSetModification = "";
                if (propGetModification == propModification) propGetModification = "";

                //if (getMethod.IsAbstract) sb.Append("abstract ");
                sb.Append($@"

    {propModification}{(getMethod?.IsStatic == true ? "static " : "")}{(getMethod?.IsVirtual == true ? "override " : "")}{returnTypeCSharpName} {prop2.Name}
    {{");

                if (getMethod != null)
                {
                    if (getMethodAttributeAny == false) sb.Append($@"
        {propGetModification} get
        {{
            return base.{prop2.Name}
        }}");
                    else sb.Append($@"
        {propGetModification} get
        {{
            Exception __DP_ARG___exception = null;
            var __DP_ARG___is_return = false;
            object __DP_ARG___return_value = null;
            var __DP_ARG___parameters = new Dictionary<string, object>();
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertyGet, false, attrsIndex, "Before")}

            try
            {{
                if (__DP_ARG___is_return == false) __DP_ARG___return_value = base.{prop2.Name};
            }}
            catch (Exception __DP_ARG___ex)
            {{
                __DP_ARG___exception = __DP_ARG___ex;
            }}
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertyGet, false, attrsIndex, "After")}
            {getMatchedAttributesCodeReturn(prop2.PropertyType, DynamicProxyInjectorType.Method, false, null)}
        }}");
                }

                if (setMethod != null)
                {
                    if (setMethodAttributeAny == false) sb.Append($@"
        {propSetModification} set
        {{
            base.{prop2.Name} = value;
        }}");
                    else sb.Append($@"
        {propSetModification} set
        {{
            Exception __DP_ARG___exception = null;
            var __DP_ARG___is_return = false;
            object __DP_ARG___return_value = null;
            var __DP_ARG___parameters = new Dictionary<string, object>();
            __DP_ARG___parameters.Add(""value"", value);
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertySet, false, attrsIndex, "Before")}

            try
            {{
                if (__DP_ARG___is_return == false)
                {{{getMatchedAttributesCodeAuditParameter("value", prop2.PropertyType)}
                    base.{prop2.Name} = value;
                }}
            }}
            catch (Exception __DP_ARG___ex)
            {{
                __DP_ARG___exception = __DP_ARG___ex;
            }}
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertySet, false, attrsIndex, "After")}
        }}");
                }


                sb.Append($@"
    }}");
            }
            #endregion

            string proxyCscode = "";
            Assembly proxyAssembly = null;
            Type proxyType = null;

            if (matchedMemberInfos.Any())
            {
                #region Constructors
                sb = new StringBuilder();
                var fromServicesTypes = matchedAttributesFromServices.SelectMany(fs => fs).GroupBy(a => a.FieldType).Select((a, b) => new KeyValuePair<Type, string>(a.Key, $"__DP_ARG___FromServices_{b}")).ToDictionary(a => a.Key, a => a.Value);
                sb.Append(string.Join("", fromServicesTypes.Select(serviceType => $@"
    private {serviceType.Key.DisplayCsharp()} {serviceType.Value};")));
                foreach (var ctor in ctors)
                {
                    var ctorParams = ctor.GetParameters();
                    sb.Append($@"

    {(ctor.IsPrivate ? "private " : "")}{(ctor.IsFamily ? "protected " : "")}{(ctor.IsAssembly ? "internal " : "")}{(ctor.IsPublic ? "public " : "")}{className}({string.Join(", ", ctorParams.Select(a => $"{a.ParameterType.DisplayCsharp()} {a.Name}"))}{
                        (ctorParams.Any() && fromServicesTypes.Any() ? ", " : "")}{
                        string.Join(", ", fromServicesTypes.Select(serviceType => $@"{serviceType.Key.DisplayCsharp()} parameter{serviceType.Value}"))})
        : base({(string.Join(", ", ctorParams.Select(a => a.Name)))})
    {{{string.Join("", fromServicesTypes.Select(serviceType => $@"
        {serviceType.Value} = parameter{serviceType.Value};"))}
    }}");
                }
                for (var a = 0; a < matchedAttributesFromServices.Count; a++)
                {
                    sb.Append($@"

    private void __DP_ARG___attribute{a}_FromServicesCopyTo({_idynamicProxyName} attr)
    {{{string.Join("", matchedAttributesFromServices[a].Select(fs => $@"
        __DP_Meta.{nameof(DynamicProxyMeta.SetDynamicProxyAttributePropertyValue)}({a}, attr, ""{fs.Name}"", {fromServicesTypes[fs.FieldType]});"))}
    }}");
                }
                #endregion

                proxyCscode = $@"using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class {className} : {typeCSharpName}
{{
    private {_metaName} __DP_Meta = {typeof(DynamicProxy).DisplayCsharp()}.{nameof(GetAvailableMeta)}(typeof({typeCSharpName}));

    //这里要注释掉，如果重写的基类没有无参构造函数，会报错
    //public {className}({_metaName} meta)
    //{{
    //    __DP_Meta = meta;
    //}}
    {sb.ToString()}
    {methodOverrideSb.ToString()}

    {propertyOverrideSb.ToString()}
}}";
                proxyAssembly = isCompile == false ? null : CompileCode(proxyCscode);
                proxyType = isCompile == false ? null : proxyAssembly.GetExportedTypes()/*.DefinedTypes*/.Where(a => a.FullName.EndsWith(className)).FirstOrDefault();
            }
            methodOverrideSb.Clear();
            propertyOverrideSb.Clear();
            sb.Clear();
            return new DynamicProxyMeta(
                type, ctors,
                matchedMemberInfos.ToArray(), matchedAttributes.ToArray(),
                isCompile == false ? proxyCscode : null, className, proxyAssembly, proxyType);
        }

#if net50 || ns21 || ns20

        static Lazy<CSScriptLib.RoslynEvaluator> _compiler = new Lazy<CSScriptLib.RoslynEvaluator>(() =>
        {
            var compiler = new CSScriptLib.RoslynEvaluator();
            compiler.DisableReferencingFromCode = false;
            compiler
                .ReferenceAssemblyOf<DynamicProxyAttribute>()
                .ReferenceDomainAssemblies();
            return compiler;
        });

        static Assembly CompileCode(string cscode)
        {
            try
            {
                return _compiler.Value.CompileCode(cscode);
            }
            catch (Exception ex)
            {
                throw new DynamicProxyException($"FreeSql.DynamicProxy 失败提示：{ex.Message} {cscode}", ex);
            }
        }
        //static Assembly CompileCode(string cscode)
        //{
        //    Natasha.AssemblyComplier complier = new Natasha.AssemblyComplier();
        //    //complier.Domain = DomainManagment.Random;
        //    complier.Add(cscode);
        //    return complier.GetAssembly();
        //}

#else

    static Assembly CompileCode(string cscode)
    {

        var files = Directory.GetFiles(Directory.GetParent(Type.GetType("FreeSql.DynamicProxy, FreeSql.DynamicProxy").Assembly.Location).FullName);
        using (var compiler = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("cs"))
        {
            var objCompilerParameters = new System.CodeDom.Compiler.CompilerParameters();
            objCompilerParameters.ReferencedAssemblies.Add("System.dll");
            objCompilerParameters.ReferencedAssemblies.Add("System.Core.dll");
            objCompilerParameters.ReferencedAssemblies.Add("FreeSql.DynamicProxy.dll");
            foreach (var dll in files)
            {
                if (!dll.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !dll.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                Console.WriteLine(dll);
                var dllName = string.Empty;
                var idx = dll.LastIndexOf('/');
                if (idx != -1) dllName = dll.Substring(idx + 1);
                else
                {
                    idx = dll.LastIndexOf('\\');
                    if (idx != -1) dllName = dll.Substring(idx + 1);
                }
                if (string.IsNullOrEmpty(dllName)) continue;
                try
                {
                    var ass = Assembly.LoadFile(dll);
                    objCompilerParameters.ReferencedAssemblies.Add(dllName);
                }
                catch
                {

                }
            }
            objCompilerParameters.GenerateExecutable = false;
            objCompilerParameters.GenerateInMemory = true;

            var cr = compiler.CompileAssemblyFromSource(objCompilerParameters, cscode);

            if (cr.Errors.Count > 0)
                throw new DynamicProxyException($"FreeSql.DynamicProxy 失败提示：{cr.Errors[0].ErrorText} {cscode}", null);

            return cr.CompiledAssembly;
        }
    }

#endif

    }
}