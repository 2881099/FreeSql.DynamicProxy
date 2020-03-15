using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FreeSql
{
    public class DynamicProxyBeforeArguments
    {
        /// <summary>
        /// 代理对象
        /// </summary>
        public object Sender { get; }
        /// <summary>
        /// 生效的类型
        /// </summary>
        public DynamicProxyInjectorType InjectorType { get; }
        /// <summary>
        /// 方法或属性反射信息
        /// </summary>
        public MemberInfo MemberInfo { get; }
        /// <summary>
        /// 方法或属性，执行时候的参数值
        /// </summary>
        public Dictionary<string, object> Parameters { get; }
        /// <summary>
        /// 生效的特性
        /// </summary>
        public DynamicProxyAttribute Attribute { get; }

        /// <summary>
        /// 是否拦截，在代理层作为判断条件，设置 ReturnValue 值时生效
        /// </summary>
        public bool IsReturn { get; private set; }
        private object _ReturnValue;
        /// <summary>
        /// 拦截自定义返回值<para></para>
        /// 如果方法返回类型为 void，设置 null 即可
        /// </summary>
        public object ReturnValue
        {
            get => _ReturnValue;
            set
            {
                if (_ReturnValue == value) return;
                _ReturnValue = value;
                IsReturn = true;
            }
        }

        /// <summary>
        /// Before/After 共用包
        /// </summary>
        public Dictionary<string, object> AfterBag { get; }

        public DynamicProxyBeforeArguments(object sender, DynamicProxyInjectorType injectorType, MemberInfo memberInfo, Dictionary<string, object> parameters, DynamicProxyAttribute attribute, object returnValue, Dictionary<string, object> afterBag)
        {
            this.Sender = sender;
            this.InjectorType = injectorType;
            this.MemberInfo = memberInfo;
            this.Parameters = parameters;
            this.Attribute = attribute;
            this._ReturnValue = returnValue;
            this.AfterBag = afterBag ?? new Dictionary<string, object>();
        }
    }

    public class DynamicProxyAfterArguments
    {
        /// <summary>
        /// 代理对象
        /// </summary>
        public object Sender { get; }
        /// <summary>
        /// 生效的类型
        /// </summary>
        public DynamicProxyInjectorType InjectorType { get; }
        /// <summary>
        /// 方法或属性反射信息
        /// </summary>
        public MemberInfo MemberInfo { get; }
        /// <summary>
        /// 方法或属性，执行时候的参数值
        /// </summary>
        public Dictionary<string, object> Parameters { get; }
        /// <summary>
        /// 生效的特性
        /// </summary>
        public DynamicProxyAttribute Attribute { get; }

        /// <summary>
        /// Before/After 共用包
        /// </summary>
        public Dictionary<string, object> BeforeBag { get; }
        /// <summary>
        /// 发生的错误
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// 发生异常时，是否处理异常<para></para>
        /// false: 抛出异常 (默认)
        /// true: 不抛出异常 (继续执行)
        /// </summary>
        public bool ExceptionHandled { get; set; }

        public DynamicProxyAfterArguments(object sender, DynamicProxyInjectorType injectorType, MemberInfo memberInfo, Dictionary<string, object> parameters, DynamicProxyAttribute attribute, Dictionary<string, object> beforeBag, Exception exception)
        {
            this.Sender = sender;
            this.InjectorType = injectorType;
            this.MemberInfo = memberInfo;
            this.Parameters = parameters;
            this.Attribute = attribute;
            this.BeforeBag = beforeBag ?? new Dictionary<string, object>();
            this.Exception = exception;
        }
    }

    public enum DynamicProxyInjectorType
    {
        /// <summary>
        /// 方法代理
        /// </summary>
        Method,
        /// <summary>
        /// 属性Get代理
        /// </summary>
        PropertyGet,
        /// <summary>
        /// 属性Set代理
        /// </summary>
        PropertySet
    }

    public class DynamicProxyException : Exception
    {
        public DynamicProxyException(string message, Exception innerException) : base(message, innerException) { }
    }
}