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

        static readonly string _metaName = typeof(DynamicProxyMeta).CSharpFullName();
        static readonly string _argumentName = typeof(DynamicProxyArguments).CSharpFullName();
        static readonly string _injectorTypeName = typeof(DynamicProxyInjectorType).CSharpFullName();
        static readonly string _idynamicProxyName = typeof(DynamicProxyAttribute).CSharpFullName();

        public static DynamicProxyMeta GetAvailableMeta(Type type)
        {
            if (_metaCache.TryGetValue(type, out var meta) == false)
                meta = _metaCache.GetOrAdd(type, tp => CreateDynamicProxyMeta(tp, true, false));
            return meta?.ProxyType != null ? meta : null;
        }
        public static DynamicProxyMeta CreateDynamicProxyMeta(Type type, bool isCompile, bool isThrow)
        {
            if (type == null) return null;
            var typeCSharpName = type.CSharpFullName();

            if (type.IsNotPublic)
            {
                if (isThrow) throw new ArgumentException($"FreeSql.DynamicProxy 失败提示：{typeCSharpName} 需要使用 public 标记");
                return null;
            }

            var matchedMemberInfos = new List<MemberInfo>();
            var matchedAttributes = new List<DynamicProxyAttribute>();
            var className = $"AopProxyClass___{Guid.NewGuid().ToString("N")}";
            var methodOverrideSb = new StringBuilder();
            var sb = methodOverrideSb;

            #region Common
            Func<Type, DynamicProxyInjectorType, bool, int, string, string> getMatchedAttributesCode = (returnType, injectorType, isAsync, attrsIndex, proxyMethodName) =>
            {
                var sbt = new StringBuilder();
                for (var a = attrsIndex; a < matchedAttributes.Count; a++)
                {
                    sbt.Append($@"
        //{proxyMethodName}{a}{(proxyMethodName == "Before" ? $"\r\n      var __DP_ARG___bag{a} = new Dictionary<string, object>()" : "")};
        __DP_ARG = new {_argumentName}(this, {_injectorTypeName}.{injectorType.ToString()}, __DP_Meta.MatchedMemberInfos[{a}], __DP_ARG__parameters, __DP_Meta.MatchedAttributes[{a}], {(returnType == typeof(void) || proxyMethodName == "Before" ? "null" : "__DP_ARG_source_return")}, __DP_ARG___bag{a});
        __DP_ARG___attribute = __DP_Meta.MatchedAttributes[{a}];
        {(isAsync ? "await " : "")}__DP_ARG___attribute.{proxyMethodName}(__DP_ARG);");

                    if (injectorType == DynamicProxyInjectorType.PropertySet)
                        sbt.Append($@"
        if (__DP_ARG.IsReturn)
        {{
            __base__property__value__ = {(returnType.IsValueType ? $"({returnType.CSharpFullName()})__DP_ARG.ReturnValue;" : $"__DP_ARG.ReturnValue as {returnType.CSharpFullName()};")}
            return;
        }}");
                    else
                        sbt.Append($@"
        if (__DP_ARG.IsReturn) {(returnType == typeof(void) ? "return;" : (isAsync == false && returnType.IsTask() ? 
                        (returnType.ReturnTypeWithoutTask().IsValueType ? 
                            $"return __DP_ARG.ReturnValue == null ? null : (__DP_ARG.ReturnValue.GetType() == typeof({returnType.ReturnTypeWithoutTask().CSharpFullName()}) ? System.Threading.Tasks.Task.FromResult(({returnType.ReturnTypeWithoutTask().CSharpFullName()})__DP_ARG.ReturnValue) : ({returnType.CSharpFullName()})__DP_ARG.ReturnValue);" : 
                            $"return __DP_ARG.ReturnValue == null ? null : (__DP_ARG.ReturnValue.GetType() == typeof({returnType.ReturnTypeWithoutTask().CSharpFullName()}) ? System.Threading.Tasks.Task.FromResult(__DP_ARG.ReturnValue as {returnType.ReturnTypeWithoutTask().CSharpFullName()}) : ({returnType.CSharpFullName()})__DP_ARG.ReturnValue);"
                        )  : 
                        (returnType.IsValueType ? $"return ({returnType.CSharpFullName()})__DP_ARG.ReturnValue;" : $"return __DP_ARG.ReturnValue as {returnType.CSharpFullName()};")))}");
                }
                return sbt.ToString();
            };
            #endregion

            #region Methods
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(a => a.IsStatic == false).ToArray();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                    if (type.GetProperty(method.Name.Substring(4), BindingFlags.Instance | BindingFlags.Public) != null) continue;
                var attrs = method.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).ToArray();
                if (attrs.Any() == false) continue;
                var attrsIndex = matchedAttributes.Count;
                matchedMemberInfos.AddRange(attrs.Select(a => method));
                matchedAttributes.AddRange(attrs);
                if (method.IsVirtual == false)
                {
                    if (isThrow) throw new ArgumentException($"FreeSql.DynamicProxy 失败提示：{typeCSharpName} 方法 {method.Name} 需要使用 virtual 标记");
                    return new DynamicProxyMeta(
                        type, ctors, 
                        new MemberInfo[0], new DynamicProxyAttribute[0],
                        null, className, null, null);
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

                sb.Append($@"

    {(methodIsAsync ? "async " : "")}{(method.IsPrivate ? "private " : "")}{(method.IsFamily ? "protected " : "")}{(method.IsAssembly ? "internal " : "")}{(method.IsPublic ? "public " : "")}{(method.IsStatic ? "static " : "")}{(method.IsAbstract ? "abstract " : "")}{(method.IsVirtual ? "override " : "")}{method.ReturnType.CSharpFullName()} {method.Name}({string.Join(", ", method.GetParameters().Select(a => $"{a.ParameterType.CSharpFullName()} {a.Name}"))})
    {{
        {_argumentName} __DP_ARG = null;
        {_idynamicProxyName} __DP_ARG___attribute = null;
        var __DP_ARG__parameters = new Dictionary<string, object>();{string.Join("\r\n        ", method.GetParameters().Select(a => $"__DP_ARG__parameters.Add(\"{a.Name}\", {a.Name});"))}
        {getMatchedAttributesCode(returnType, DynamicProxyInjectorType.Method, methodIsAsync, attrsIndex, "Before")}

        {(returnType != typeof(void) ? "var __DP_ARG_source_return = " : "")}{(methodIsAsync ? "await " : "")}base.{method.Name}({(string.Join(", ", method.GetParameters().Select(a => a.Name)))});
        {getMatchedAttributesCode(returnType, DynamicProxyInjectorType.Method, methodIsAsync, attrsIndex, "After")}

        return{(returnType != typeof(void) ? " __DP_ARG_source_return" : "")};
    }}");
            }
            #endregion

            var propertyOverrideSb = new StringBuilder();
            sb = propertyOverrideSb;
            #region Property
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var prop2 in props)
            {
                var getMethod = prop2.GetGetMethod(false);
                var setMethod = prop2.GetSetMethod(false);
                if (getMethod?.IsVirtual == false && setMethod?.IsVirtual == false)
                {
                    if (getMethod.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).Any() ||
                        setMethod.GetCustomAttributes(false).Select(a => a as DynamicProxyAttribute).Where(a => a != null).Any())
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

                var returnTypeCSharpName = prop2.PropertyType.CSharpFullName();

                //if (getMethod.IsAbstract) sb.Append("abstract ");
                sb.Append($@"

    {(getMethod?.IsPublic == true || setMethod?.IsPublic == true ? "public " : (getMethod?.IsAssembly == true || setMethod?.IsAssembly == true ? "internal " : (getMethod?.IsFamily == true || setMethod?.IsFamily == true ? "protected " : (getMethod?.IsPrivate == true || setMethod?.IsPrivate == true ? "private " : ""))))}{(getMethod?.IsStatic == true ? "static " : "")}{(getMethod?.IsVirtual == true ? "override " : "")}{returnTypeCSharpName} {prop2.Name}
    {{");

                if (getMethod != null)
                {
                    if (getMethodAttributeAny == false) sb.Append($@"
        get
        {{
            return base.{prop2.Name}
        }}");
                    else sb.Append($@"
        get
        {{
            {_argumentName} __DP_ARG = null;
            {_idynamicProxyName} __DP_ARG___attribute = null;
            var __DP_ARG__parameters = new Dictionary<string, object>();
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertyGet, false, attrsIndex, "Before")}

            var __DP_ARG_source_return = base.{prop2.Name};
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertyGet, false, attrsIndex, "After")}

            return __DP_ARG_source_return;
        }}");
                }

                if (setMethod != null)
                {
                    if (setMethodAttributeAny == false) sb.Append($@"
        set
        {{
            base.{prop2.Name} = value;
        }}");
                    else sb.Append($@"
        set
        {{
            {_argumentName} __DP_ARG = null;
            {_idynamicProxyName} __DP_ARG___attribute = null;
            var __DP_ARG__parameters = new Dictionary<string, object>();
            __DP_ARG__parameters.Add(""value"", value);
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertySet, false, attrsIndex, "Before").Replace("__base__property__value__", $"base.{prop2.Name}")}

            base.{prop2.Name} = value;
            var __DP_ARG_source_return = value;
            {getMatchedAttributesCode(prop2.PropertyType, DynamicProxyInjectorType.PropertySet, false, attrsIndex, "After").Replace("__base__property__value__", $"base.{prop2.Name}")}
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
                foreach (var ctor in ctors)
                {
                    sb.Append($@"

    {(ctor.IsPrivate ? "private " : "")}{(ctor.IsFamily ? "protected " : "")}{(ctor.IsAssembly ? "internal " : "")}{(ctor.IsPublic ? "public " : "")}{className}({string.Join(", ", ctor.GetParameters().Select(a => $"{a.ParameterType.CSharpFullName()} {a.Name}"))})
        : base({(string.Join(", ", ctor.GetParameters().Select(a => a.Name)))})
    {{
    }}");
                }
                #endregion

                proxyCscode = $@"using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class {className} : {typeCSharpName}
{{
    private {_metaName} __DP_Meta = {typeof(DynamicProxy).CSharpFullName()}.{nameof(GetAvailableMeta)}(typeof({typeCSharpName}));

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

#if ns20 || ns21

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
                throw new Exception($"{ex.Message} {cscode}", ex);
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
                throw new Exception($"{cr.Errors[0].ErrorText} {cscode}");

            return cr.CompiledAssembly;
        }
    }

#endif

    }
}