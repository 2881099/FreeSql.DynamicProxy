using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static FreeSql.DynamicProxy;

namespace FreeSql
{
    /// <summary>
    /// 实现该特性，标记的方法/属性支持动态代理
    /// </summary>
    public abstract class DynamicProxyAttribute : Attribute
    {

        /// <summary>
        /// 同步方法执行之前
        /// </summary>
        /// <param name="args"></param>
        public virtual void Before(DynamicProxyArguments args) { }

        /// <summary>
        /// 同步方法执行之后
        /// </summary>
        /// <param name="args"></param>
        public virtual void After(DynamicProxyArguments args) { }


#if net40
#else
        /// <summary>
        /// 异步方法执行之前，处理返回值为 Task/Task&lt;T&gt; 的异步方法
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task BeforeAsync(DynamicProxyArguments args) => Task.FromResult(false);
        /// <summary>
        /// 异步方法执行之前，处理返回值为 Task/Task&lt;T&gt; 的异步方法
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task AfterAsync(DynamicProxyArguments args) => Task.FromResult(false);
#endif

    }
}
