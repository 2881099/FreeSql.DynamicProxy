using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FreeSql
{
    public class DynamicProxyArguments
    {

        /// <summary>
        /// 真实目标实例
        /// </summary>
        public object Sender { get; }
        /// <summary>
        /// 生效的类型
        /// </summary>
        public DynamicProxy.InjectorType InjectorType { get; }
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

        public DynamicProxyArguments(object sender, DynamicProxy.InjectorType injectorType, MemberInfo memberInfo, Dictionary<string, object> parameters, DynamicProxyAttribute attribute, object returnValue, Dictionary<string, object> bag)
        {
            this.Sender = sender;
            this.InjectorType = injectorType;
            this.MemberInfo = memberInfo;
            this.Parameters = parameters;
            this.Attribute = attribute;
            this._ReturnValue = returnValue;
            this.AfterBag = bag ?? new Dictionary<string, object>();
        }
    }

    partial class DynamicProxy
    {
        public enum InjectorType
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
    }
    }