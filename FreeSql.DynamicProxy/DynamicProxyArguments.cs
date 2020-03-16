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
        /// 是否拦截，在代理层作为判断条件，设置 ReturnValue 值时生效
        /// </summary>
        public bool Returned { get; private set; }
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
                Returned = true;
            }
        }

        public DynamicProxyBeforeArguments(object sender, DynamicProxyInjectorType injectorType, MemberInfo memberInfo, Dictionary<string, object> parameters, object returnValue)
        {
            this.Sender = sender;
            this.InjectorType = injectorType;
            this.MemberInfo = memberInfo;
            this.Parameters = parameters;
            this._ReturnValue = returnValue;
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
        /// 方法的返回值
        /// </summary>
        public object ReturnValue { get; }
        /// <summary>
        /// 发生的错误
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// 发生异常时，是否处理异常<para></para>
        /// false: 抛出异常 (默认)
        /// true: 忽略异常 (继续执行)
        /// </summary>
        public bool ExceptionHandled { get; set; }

        public DynamicProxyAfterArguments(object sender, DynamicProxyInjectorType injectorType, MemberInfo memberInfo, Dictionary<string, object> parameters, object returnValue, Exception exception)
        {
            this.Sender = sender;
            this.InjectorType = injectorType;
            this.MemberInfo = memberInfo;
            this.Parameters = parameters;
            this.ReturnValue = returnValue;
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