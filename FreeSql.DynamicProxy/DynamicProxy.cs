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

        static ConcurrentDictionary<Type, Meta> _metaCache = new ConcurrentDictionary<Type, Meta>();

        /// <summary>
        /// 创建 FreeSql.DynamicProxy 代理对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateInstanse<T>() where T : class => CreateInstanse(typeof(T), null) as T;
        public static T CreateInstanse<T>(object[] parameters) where T : class => CreateInstanse(typeof(T), parameters) as T;

        /// <summary>
        /// 创建 FreeSql.DynamicProxy 代理对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object CreateInstanse(Type type, object[] parameters)
        {
            var meta = _metaCache.GetOrAdd(type, tp => Test(tp));
            if (meta == null) return null;
            object source = meta.CreateSourceInstance(parameters);
            return Activator.CreateInstance(meta.ProxyType, new object[] { source, meta });
        }

        static readonly string _metaName = typeof(Meta).CSharpFullName();
        static readonly string _argumentName = typeof(DynamicProxyArguments).CSharpFullName();
        static readonly string _injectorTypeName = typeof(InjectorType).CSharpFullName();
        static readonly string _idynamicProxyName = typeof(DynamicProxyAttribute).CSharpFullName();

        public static Meta Test(Type type, bool isCompile = true, bool isThrow = true)
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
            Func<Type, InjectorType, bool, int, string, string> getMatchedAttributesCode = (returnType, injectorType, isAsync, attrsIndex, proxyMethodName) =>
            {
                var sbt = new StringBuilder();
                for (var a = attrsIndex; a < matchedAttributes.Count; a++)
                {
                    sbt.Append($@"
        //{proxyMethodName}{a}{(proxyMethodName == "Before" ? $"\r\n      var __AE_ARG___bag{a} = new Dictionary<string, object>()" : "")};
        __AE_ARG = new {_argumentName}(__AE_Source, {_injectorTypeName}.{injectorType.ToString()}, __AE_Meta.MatchedMemberInfos[{a}], __AE_ARG__parameters, __AE_Meta.MatchedAttributes[{a}], {(returnType == typeof(void) || proxyMethodName == "Before" ? "null" : "__AE_ARG_source_return")}, __AE_ARG___bag{a});
        __AE_ARG___attribute = __AE_Meta.MatchedAttributes[{a}];
        {(isAsync ? "await " : "")}__AE_ARG___attribute.{proxyMethodName}{(isAsync ? "Async" : "")}(__AE_ARG);");

                    if (injectorType == InjectorType.PropertySet)
                        sbt.Append($@"
        if (__AE_ARG.IsReturn)
        {{
            __base__property__value__ = {(returnType.IsValueType ? $"({returnType.CSharpFullName()})__AE_ARG.ReturnValue;" : $"__AE_ARG.ReturnValue as {returnType.CSharpFullName()};")}
            return;
        }}");
                    else
                        sbt.Append($@"
        if (__AE_ARG.IsReturn) {(returnType == typeof(void) ? "return;" : (returnType.IsValueType ? $"return ({returnType.CSharpFullName()})__AE_ARG.ReturnValue;" : $"return __AE_ARG.ReturnValue as {returnType.CSharpFullName()};"))}");
                }
                return sbt.ToString();
            };
            #endregion

            #region Methods
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
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
                    return null;
                }

#if net40
                var returnType = method.ReturnType;
                var methodIsAsync = false;
#else
                var returnType = method.ReturnType.ReturnTypeWithoutTask();
                var methodIsAsync = method.ReturnType.IsTask();
#endif

                sb.Append($@"

    {(methodIsAsync ? "async " : "")}{(method.IsPrivate ? "private " : "")}{(method.IsFamily ? "protected " : "")}{(method.IsAssembly ? "internal " : "")}{(method.IsPublic ? "public " : "")}{(method.IsStatic ? "static " : "")}{(method.IsAbstract ? "abstract " : "")}{(method.IsVirtual ? "override " : "")}{method.ReturnType.CSharpFullName()} {method.Name}({string.Join(", ", method.GetParameters().Select(a => $"{a.ParameterType.CSharpFullName()} {a.Name}"))})
    {{
        {_argumentName} __AE_ARG = null;
        {_idynamicProxyName} __AE_ARG___attribute = null;
        var __AE_ARG__parameters = new Dictionary<string, object>();{string.Join("\r\n        ", method.GetParameters().Select(a => $"__AE_ARG__parameters.Add(\"{a.Name}\", {a.Name});"))}
        {getMatchedAttributesCode(returnType, FreeSql.DynamicProxy.InjectorType.Method, methodIsAsync, attrsIndex, "Before")}

        {(returnType != typeof(void) ? "var __AE_ARG_source_return = " : "")}{(methodIsAsync ? "await " : "")}__AE_Source.{method.Name}({(string.Join(", ", method.GetParameters().Select(a => a.Name)))});
        {getMatchedAttributesCode(returnType, FreeSql.DynamicProxy.InjectorType.Method, methodIsAsync, attrsIndex, "After")}

        return{(returnType != typeof(void) ? " __AE_ARG_source_return" : "")};
    }}");
            }
#endregion

            var propertyOverrideSb = new StringBuilder();
            sb = propertyOverrideSb;
            #region Property
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop2 in props)
            {
                var getMethod = prop2.GetGetMethod(false);
                var setMethod = prop2.GetSetMethod(false);
                if (getMethod?.IsVirtual == false && setMethod?.IsVirtual == false)
                {
                    if (isThrow) throw new ArgumentException($"FreeSql.DynamicProxy 失败提示：{typeCSharpName} 属性 {prop2.Name} 需要使用 virtual 标记");
                    return null;
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
            return __AE_Source.{prop2.Name}
        }}");
                    else sb.Append($@"
        get
        {{
            {_argumentName} __AE_ARG = null;
            {_idynamicProxyName} __AE_ARG___attribute = null;
            var __AE_ARG__parameters = new Dictionary<string, object>();
            {getMatchedAttributesCode(prop2.PropertyType, InjectorType.PropertyGet, false, attrsIndex, "Before")}

            var __AE_ARG_source_return = __AE_Source.{prop2.Name};
            {getMatchedAttributesCode(prop2.PropertyType, InjectorType.PropertyGet, false, attrsIndex, "After")}

            return __AE_ARG_source_return;
        }}");
                }

                if (setMethod != null)
                {
                    if (setMethodAttributeAny == false) sb.Append($@"
        set
        {{
            __AE_Source.{prop2.Name} = value;
        }}");
                    else sb.Append($@"
        set
        {{
            {_argumentName} __AE_ARG = null;
            {_idynamicProxyName} __AE_ARG___attribute = null;
            var __AE_ARG__parameters = new Dictionary<string, object>();
            __AE_ARG__parameters.Add(""value"", value);
            {getMatchedAttributesCode(prop2.PropertyType, InjectorType.PropertySet, false, attrsIndex, "Before").Replace("__base__property__value__", $"__AE_Source.{prop2.Name}")}

            __AE_Source.{prop2.Name} = value;
            var __AE_ARG_source_return = value;
            {getMatchedAttributesCode(prop2.PropertyType, InjectorType.PropertySet, false, attrsIndex, "After").Replace("__base__property__value__", $"__AE_Source.{prop2.Name}")}
        }}");
                }


                sb.Append($@"
    }}");
            }
#endregion

            if (matchedMemberInfos.Any() == false) return null;

            var proxyCscode = $@"using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class {className} : {typeCSharpName}
{{
    private {typeCSharpName} __AE_Source;
    private {_metaName} __AE_Meta;

    public {className}({typeCSharpName} source, {_metaName} meta)
    {{
        __AE_Source = source;
        __AE_Meta = meta;
    }}
    {methodOverrideSb.ToString()}

    {propertyOverrideSb.ToString()}
}}";
            var proxyAssembly = isCompile == false ? null : CompileCode(proxyCscode);
            var proxyType = isCompile == false ? null : proxyAssembly.GetExportedTypes()/*.DefinedTypes*/.Where(a => a.FullName.EndsWith(className)).FirstOrDefault();

            return new Meta(
                type, type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                matchedMemberInfos.ToArray(), matchedAttributes.ToArray(),
                isCompile == false ? proxyCscode : null, className, proxyAssembly, proxyType);
        }

#if netstandard

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