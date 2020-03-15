using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql
{
    /// <summary>
    /// 实现该特性，标记的方法/属性支持动态代理
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class DynamicProxyAttribute : Attribute
    {

#if net40
        /// <summary>
        /// 方法执行之前
        /// </summary>
        /// <param name="args"></param>
        public virtual void Before(DynamicProxyBeforeArguments args) { }

        /// <summary>
        /// 方法执行之后
        /// </summary>
        /// <param name="args"></param>
        public virtual void After(DynamicProxyAfterArguments args) { }
#else
        /// <summary>
        /// 方法执行之前
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task Before(DynamicProxyBeforeArguments args) => Task.FromResult(false);
        /// <summary>
        /// 方法执行之前
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task After(DynamicProxyAfterArguments args) => Task.FromResult(false);
#endif

    }
}
